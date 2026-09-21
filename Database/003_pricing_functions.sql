create or replace function private.price_order(
  p_order_type text,
  p_items jsonb,
  p_fabric_id uuid,
  p_fabric_note text,
  p_collection_id uuid,
  p_timeframe_id uuid
)
returns jsonb
language plpgsql
stable
set search_path = ''
as $$
declare
  v_fabric public.fabrics;
  v_collection public.collections;
  v_timeframe public.timeframes;
  v_size public.sizes;
  v_item jsonb;
  v_size_id uuid;
  v_quantity integer;
  v_seen_sizes uuid[] := '{}';
  v_lines jsonb := '[]'::jsonb;
  v_base_price numeric(12,2) := 0;
  v_unit_price numeric(12,2);
  v_line_total numeric(12,2);
  v_subtotal numeric(12,2) := 0;
  v_total_quantity integer := 0;
  v_percent numeric(5,2) := 0;
  v_surcharge numeric(12,2);
begin
  if p_order_type is null or p_order_type not in ('custom', 'upload', 'collection') then
    raise exception 'Invalid order type.' using errcode = '22023';
  end if;

  if p_items is null or jsonb_typeof(p_items) <> 'array' or jsonb_array_length(p_items) = 0 then
    raise exception 'Select at least one size and quantity.' using errcode = '22023';
  end if;

  if p_order_type = 'collection' then
    if p_collection_id is null then
      raise exception 'Select a collection design.' using errcode = '22023';
    end if;
    select * into v_collection from public.collections where id = p_collection_id and is_active;
    if not found then
      raise exception 'The selected collection design is no longer available.' using errcode = '22023';
    end if;
    v_base_price := v_collection.price;
  else
    if p_fabric_id is null then
      raise exception 'Select a fabric.' using errcode = '22023';
    end if;
    select * into v_fabric from public.fabrics where id = p_fabric_id and is_active;
    if not found then
      raise exception 'The selected fabric is no longer available.' using errcode = '22023';
    end if;
    if v_fabric.requires_note and coalesce(btrim(p_fabric_note), '') = '' then
      raise exception 'Describe the fabric you want.' using errcode = '22023';
    end if;
    v_base_price := v_fabric.price;
  end if;

  for v_item in select value from jsonb_array_elements(p_items) loop
    if jsonb_typeof(v_item) <> 'object' or v_item ->> 'size_id' is null or v_item ->> 'quantity' is null then
      raise exception 'Each item needs a size and a quantity.' using errcode = '22023';
    end if;

    v_size_id := (v_item ->> 'size_id')::uuid;
    v_quantity := (v_item ->> 'quantity')::integer;

    if v_quantity < 1 or v_quantity > 100000 then
      raise exception 'Quantity must be between 1 and 100000.' using errcode = '22023';
    end if;
    if v_size_id = any (v_seen_sizes) then
      raise exception 'Each size can only be listed once.' using errcode = '22023';
    end if;
    v_seen_sizes := v_seen_sizes || v_size_id;

    select * into v_size from public.sizes where id = v_size_id and is_active;
    if not found then
      raise exception 'A selected size is no longer available.' using errcode = '22023';
    end if;

    v_unit_price := v_size.price + v_base_price;
    v_line_total := v_unit_price * v_quantity;
    v_subtotal := v_subtotal + v_line_total;
    v_total_quantity := v_total_quantity + v_quantity;
    v_lines := v_lines || jsonb_build_array(jsonb_build_object(
      'size_id', v_size.id,
      'size_name', v_size.name,
      'size_price', v_size.price,
      'unit_price', v_unit_price,
      'quantity', v_quantity,
      'line_total', v_line_total
    ));
  end loop;

  if p_timeframe_id is not null then
    select * into v_timeframe from public.timeframes where id = p_timeframe_id and is_active;
    if not found then
      raise exception 'The selected delivery timeframe is no longer available.' using errcode = '22023';
    end if;
    if v_timeframe.max_quantity is not null and v_total_quantity > v_timeframe.max_quantity then
      raise exception 'The "%" delivery option allows at most % pieces per order.',
        v_timeframe.label, v_timeframe.max_quantity using errcode = '22023';
    end if;
    v_percent := v_timeframe.surcharge_percent;
  end if;

  v_surcharge := round(v_subtotal * v_percent / 100, 2);

  return jsonb_build_object(
    'order_type', p_order_type,
    'fabric_id', v_fabric.id,
    'fabric_name', v_fabric.name,
    'custom_fabric_note', case when v_fabric.requires_note then btrim(p_fabric_note) end,
    'collection_id', v_collection.id,
    'collection_name', v_collection.name,
    'base_unit_price', v_base_price,
    'timeframe_id', v_timeframe.id,
    'timeframe_label', v_timeframe.label,
    'surcharge_percent', v_percent,
    'subtotal', v_subtotal,
    'surcharge_amount', v_surcharge,
    'total', v_subtotal + v_surcharge,
    'total_quantity', v_total_quantity,
    'lines', v_lines
  );
end;
$$;

create or replace function public.quote_order(
  p_order_type text,
  p_items jsonb,
  p_fabric_id uuid default null,
  p_fabric_note text default null,
  p_collection_id uuid default null,
  p_timeframe_id uuid default null
)
returns jsonb
language sql
stable
security definer
set search_path = ''
as $$
  select private.price_order(p_order_type, p_items, p_fabric_id, p_fabric_note, p_collection_id, p_timeframe_id);
$$;

create or replace function public.place_order(
  p_order_type text,
  p_items jsonb,
  p_fabric_id uuid default null,
  p_fabric_note text default null,
  p_collection_id uuid default null,
  p_timeframe_id uuid default null,
  p_design_reference text default null
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
begin
  if v_customer_id is null then
    raise exception 'Sign in to place an order.' using errcode = '28000';
  end if;
  if not exists (select 1 from public.profiles where id = v_customer_id) then
    raise exception 'Your profile could not be found.' using errcode = '23503';
  end if;

  v_quote := private.price_order(p_order_type, p_items, p_fabric_id, p_fabric_note, p_collection_id, p_timeframe_id);

  insert into public.orders (
    customer_id, order_type, status, total_price, quantity,
    fabric_id, fabric_name, fabric_type, custom_fabric_note,
    collection_id, collection_name, base_unit_price,
    timeframe_id, timeframe_label, surcharge_percent,
    subtotal, surcharge_amount, design_reference
  )
  values (
    v_customer_id,
    p_order_type,
    'Pending',
    (v_quote ->> 'total')::numeric,
    (v_quote ->> 'total_quantity')::integer,
    (v_quote ->> 'fabric_id')::uuid,
    v_quote ->> 'fabric_name',
    v_quote ->> 'fabric_name',
    v_quote ->> 'custom_fabric_note',
    (v_quote ->> 'collection_id')::uuid,
    v_quote ->> 'collection_name',
    (v_quote ->> 'base_unit_price')::numeric,
    (v_quote ->> 'timeframe_id')::uuid,
    v_quote ->> 'timeframe_label',
    (v_quote ->> 'surcharge_percent')::numeric,
    (v_quote ->> 'subtotal')::numeric,
    (v_quote ->> 'surcharge_amount')::numeric,
    p_design_reference
  )
  returning id into v_order_id;

  insert into public.order_items (order_id, size_id, size_name, size_price, unit_price, quantity, line_total)
  select v_order_id, l.size_id, l.size_name, l.size_price, l.unit_price, l.quantity, l.line_total
  from jsonb_to_recordset(v_quote -> 'lines') as l(
    size_id uuid, size_name text, size_price numeric, unit_price numeric, quantity integer, line_total numeric
  );

  return v_order_id;
end;
$$;

revoke all on function public.quote_order(text, jsonb, uuid, text, uuid, uuid) from public;
grant execute on function public.quote_order(text, jsonb, uuid, text, uuid, uuid) to anon, authenticated;

revoke all on function public.place_order(text, jsonb, uuid, text, uuid, uuid, text) from public, anon;
grant execute on function public.place_order(text, jsonb, uuid, text, uuid, uuid, text) to authenticated;
