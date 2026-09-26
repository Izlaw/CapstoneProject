alter table public.collection_orders
  add column design_data jsonb;

update public.collection_orders co
set design_data = c.design_data
from public.collections c
where c.id = co.collection_id
  and co.design_data is null;

create or replace function public.place_order(
  p_order_type text,
  p_items jsonb,
  p_fabric_id uuid default null,
  p_fabric_note text default null,
  p_collection_id uuid default null,
  p_timeframe_id uuid default null,
  p_shirt_color text default null,
  p_design_data jsonb default null,
  p_image_path text default null,
  p_original_file_name text default null
)
returns uuid
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_customer_id uuid := (select auth.uid());
  v_folder_prefix text;
  v_quote jsonb;
  v_order_id uuid;
  v_design_data jsonb;
  v_element jsonb;
begin
  if v_customer_id is null then
    raise exception 'Sign in to place an order.' using errcode = '28000';
  end if;
  if not exists (select 1 from public.profiles where id = v_customer_id) then
    raise exception 'Your profile could not be found.' using errcode = '23503';
  end if;

  v_folder_prefix := v_customer_id::text || '/';
  v_quote := private.price_order(p_order_type, p_items, p_fabric_id, p_fabric_note, p_collection_id, p_timeframe_id);

  if p_image_path is not null and (char_length(p_image_path) > 2048 or left(p_image_path, char_length(v_folder_prefix)) <> v_folder_prefix) then
    raise exception 'The image is not valid. Please upload it again.' using errcode = '22023';
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
    for v_element in select value from jsonb_array_elements(v_design_data -> 'elements') loop
      if jsonb_typeof(v_element) <> 'object' or v_element ->> 'type' not in ('text', 'image') then
        raise exception 'The design data is not valid.' using errcode = '22023';
      end if;
      if v_element ->> 'type' = 'image' and (
        v_element ->> 'path' is null
        or left(v_element ->> 'path', char_length(v_folder_prefix)) <> v_folder_prefix
      ) then
        raise exception 'A design image is not valid. Please upload it again.' using errcode = '22023';
      end if;
    end loop;
  elsif p_order_type = 'upload' then
    if coalesce(btrim(p_image_path), '') = '' then
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
    insert into public.custom_orders (order_id, fabric_id, fabric_name, fabric_note, shirt_color, design_data, design_image_path)
    values (
      v_order_id,
      (v_quote ->> 'fabric_id')::uuid,
      v_quote ->> 'fabric_name',
      v_quote ->> 'custom_fabric_note',
      p_shirt_color,
      v_design_data,
      p_image_path
    );
  elsif p_order_type = 'upload' then
    insert into public.upload_orders (order_id, fabric_id, fabric_name, fabric_note, uploaded_image_path, original_file_name)
    values (
      v_order_id,
      (v_quote ->> 'fabric_id')::uuid,
      v_quote ->> 'fabric_name',
      v_quote ->> 'custom_fabric_note',
      btrim(p_image_path),
      left(p_original_file_name, 255)
    );
  else
    insert into public.collection_orders (order_id, collection_id, collection_name, design_data)
    select
      v_order_id,
      c.id,
      c.name,
      c.design_data
    from public.collections c
    where c.id = (v_quote ->> 'collection_id')::uuid;
  end if;

  return v_order_id;
end;
$$;

create or replace function public.get_shared_order(p_token uuid)
returns jsonb
language sql
stable
security definer
set search_path = ''
as $$
  select jsonb_build_object(
    'short_id', left(o.id::text, 8),
    'order_type', o.order_type,
    'status', o.status,
    'created_at', o.created_at,
    'timeframe_label', o.timeframe_label,
    'quantity', o.quantity,
    'subtotal', o.subtotal,
    'surcharge_percent', o.surcharge_percent,
    'surcharge_amount', o.surcharge_amount,
    'total_price', o.total_price,
    'items', coalesce((
      select jsonb_agg(
        jsonb_build_object(
          'size_name', i.size_name,
          'quantity', i.quantity,
          'unit_price', i.unit_price,
          'line_total', i.line_total
        )
        order by i.size_name
      )
      from public.order_items i
      where i.order_id = o.id
    ), '[]'::jsonb),
    'custom', (
      select jsonb_build_object(
        'fabric_name', c.fabric_name,
        'fabric_note', c.fabric_note,
        'shirt_color', c.shirt_color,
        'design_data', c.design_data
      )
      from public.custom_orders c
      where c.order_id = o.id
    ),
    'upload', (
      select jsonb_build_object(
        'fabric_name', u.fabric_name,
        'fabric_note', u.fabric_note,
        'original_file_name', u.original_file_name,
        'image_path', u.uploaded_image_path
      )
      from public.upload_orders u
      where u.order_id = o.id
    ),
    'collection', (
      select jsonb_build_object(
        'collection_name', co.collection_name,
        'design_data', co.design_data
      )
      from public.collection_orders co
      where co.order_id = o.id
    )
  )
  from public.orders o
  where o.share_token = p_token;
$$;
