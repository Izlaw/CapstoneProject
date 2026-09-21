alter table public.orders
  drop column fabric_id,
  drop column fabric_name,
  drop column custom_fabric_note,
  drop column collection_id,
  drop column collection_name,
  alter column order_type set not null;

create or replace function private.assert_order_type()
returns trigger
language plpgsql
set search_path = ''
as $$
begin
  if not exists (
    select 1 from public.orders
    where id = new.order_id and order_type = tg_argv[0]
  ) then
    raise exception 'This order is not a % order.', tg_argv[0] using errcode = '23514';
  end if;
  return new;
end;
$$;

create table public.custom_orders (
  order_id uuid primary key references public.orders (id) on delete cascade,
  fabric_id uuid not null references public.fabrics (id) on delete restrict,
  fabric_name text not null,
  fabric_note text,
  shirt_color text not null check (shirt_color ~ '^#[0-9A-Fa-f]{6}$'),
  design_data jsonb not null default '{"elements": []}'::jsonb
    check (jsonb_typeof(design_data) = 'object' and jsonb_typeof(design_data -> 'elements') = 'array'),
  design_image_url text check (char_length(design_image_url) <= 2048),
  created_at timestamptz not null default now()
);

create table public.upload_orders (
  order_id uuid primary key references public.orders (id) on delete cascade,
  fabric_id uuid not null references public.fabrics (id) on delete restrict,
  fabric_name text not null,
  fabric_note text,
  uploaded_image_url text not null check (char_length(uploaded_image_url) <= 2048),
  original_file_name text check (char_length(original_file_name) <= 255),
  created_at timestamptz not null default now()
);

create table public.collection_orders (
  order_id uuid primary key references public.orders (id) on delete cascade,
  collection_id uuid not null references public.collections (id) on delete restrict,
  collection_name text not null,
  created_at timestamptz not null default now()
);

create index custom_orders_fabric_id_idx on public.custom_orders (fabric_id);
create index upload_orders_fabric_id_idx on public.upload_orders (fabric_id);
create index collection_orders_collection_id_idx on public.collection_orders (collection_id);

create trigger custom_orders_assert_order_type before insert or update of order_id on public.custom_orders
  for each row execute function private.assert_order_type('custom');
create trigger upload_orders_assert_order_type before insert or update of order_id on public.upload_orders
  for each row execute function private.assert_order_type('upload');
create trigger collection_orders_assert_order_type before insert or update of order_id on public.collection_orders
  for each row execute function private.assert_order_type('collection');

alter table public.custom_orders enable row level security;
alter table public.upload_orders enable row level security;
alter table public.collection_orders enable row level security;

create policy "Customers can view their own custom orders" on public.custom_orders
  for select to authenticated
  using (exists (
    select 1 from public.orders
    where orders.id = custom_orders.order_id and orders.customer_id = (select auth.uid())
  ));
create policy "Staff can view all custom orders" on public.custom_orders
  for select to authenticated using ((select public.is_staff()));

create policy "Customers can view their own upload orders" on public.upload_orders
  for select to authenticated
  using (exists (
    select 1 from public.orders
    where orders.id = upload_orders.order_id and orders.customer_id = (select auth.uid())
  ));
create policy "Staff can view all upload orders" on public.upload_orders
  for select to authenticated using ((select public.is_staff()));

create policy "Customers can view their own collection orders" on public.collection_orders
  for select to authenticated
  using (exists (
    select 1 from public.orders
    where orders.id = collection_orders.order_id and orders.customer_id = (select auth.uid())
  ));
create policy "Staff can view all collection orders" on public.collection_orders
  for select to authenticated using ((select public.is_staff()));

revoke all on public.custom_orders, public.upload_orders, public.collection_orders from anon, authenticated;
grant select on public.custom_orders, public.upload_orders, public.collection_orders to authenticated;

drop function public.place_order(text, jsonb, uuid, text, uuid, uuid, text);

create or replace function public.place_order(
  p_order_type text,
  p_items jsonb,
  p_fabric_id uuid default null,
  p_fabric_note text default null,
  p_collection_id uuid default null,
  p_timeframe_id uuid default null,
  p_shirt_color text default null,
  p_design_data jsonb default null,
  p_image_url text default null,
  p_original_file_name text default null
)
returns uuid
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_customer_id uuid := (select auth.uid());
  v_quote jsonb;
  v_order_id uuid;
  v_design_data jsonb;
begin
  if v_customer_id is null then
    raise exception 'Sign in to place an order.' using errcode = '28000';
  end if;
  if not exists (select 1 from public.profiles where id = v_customer_id) then
    raise exception 'Your profile could not be found.' using errcode = '23503';
  end if;

  v_quote := private.price_order(p_order_type, p_items, p_fabric_id, p_fabric_note, p_collection_id, p_timeframe_id);

  if p_image_url is not null and char_length(p_image_url) > 2048 then
    raise exception 'The image link is too long. Upload the image first and use its link.' using errcode = '22023';
  end if;

  if p_order_type = 'custom' then
    if p_shirt_color is null or p_shirt_color !~ '^#[0-9A-Fa-f]{6}$' then
      raise exception 'Choose a shirt color.' using errcode = '22023';
    end if;
    v_design_data := coalesce(p_design_data, '{"elements": []}'::jsonb);
    if jsonb_typeof(v_design_data) <> 'object' or jsonb_typeof(v_design_data -> 'elements') is distinct from 'array' then
      raise exception 'The design data is not valid.' using errcode = '22023';
    end if;
    if jsonb_array_length(v_design_data -> 'elements') > 50 or char_length(v_design_data::text) > 100000 then
      raise exception 'The design is too large.' using errcode = '22023';
    end if;
  elsif p_order_type = 'upload' then
    if coalesce(btrim(p_image_url), '') = '' then
      raise exception 'Upload your design image.' using errcode = '22023';
    end if;
  end if;

  insert into public.orders (
    customer_id, order_type, status, total_price, quantity, base_unit_price,
    timeframe_id, timeframe_label, surcharge_percent, subtotal, surcharge_amount
  )
  values (
    v_customer_id,
    p_order_type,
    'Pending',
    (v_quote ->> 'total')::numeric,
    (v_quote ->> 'total_quantity')::integer,
    (v_quote ->> 'base_unit_price')::numeric,
    (v_quote ->> 'timeframe_id')::uuid,
    v_quote ->> 'timeframe_label',
    (v_quote ->> 'surcharge_percent')::numeric,
    (v_quote ->> 'subtotal')::numeric,
    (v_quote ->> 'surcharge_amount')::numeric
  )
  returning id into v_order_id;

  insert into public.order_items (order_id, size_id, size_name, size_price, unit_price, quantity, line_total)
  select v_order_id, l.size_id, l.size_name, l.size_price, l.unit_price, l.quantity, l.line_total
  from jsonb_to_recordset(v_quote -> 'lines') as l(
    size_id uuid, size_name text, size_price numeric, unit_price numeric, quantity integer, line_total numeric
  );

  if p_order_type = 'custom' then
    insert into public.custom_orders (order_id, fabric_id, fabric_name, fabric_note, shirt_color, design_data, design_image_url)
    values (
      v_order_id,
      (v_quote ->> 'fabric_id')::uuid,
      v_quote ->> 'fabric_name',
      v_quote ->> 'custom_fabric_note',
      p_shirt_color,
      v_design_data,
      p_image_url
    );
  elsif p_order_type = 'upload' then
    insert into public.upload_orders (order_id, fabric_id, fabric_name, fabric_note, uploaded_image_url, original_file_name)
    values (
      v_order_id,
      (v_quote ->> 'fabric_id')::uuid,
      v_quote ->> 'fabric_name',
      v_quote ->> 'custom_fabric_note',
      btrim(p_image_url),
      p_original_file_name
    );
  else
    insert into public.collection_orders (order_id, collection_id, collection_name)
    values (
      v_order_id,
      (v_quote ->> 'collection_id')::uuid,
      v_quote ->> 'collection_name'
    );
  end if;

  return v_order_id;
end;
$$;

revoke all on function public.place_order(text, jsonb, uuid, text, uuid, uuid, text, jsonb, text, text) from public, anon;
grant execute on function public.place_order(text, jsonb, uuid, text, uuid, uuid, text, jsonb, text, text) to authenticated;
