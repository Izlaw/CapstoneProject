alter table public.orders
  add column share_token uuid not null default gen_random_uuid();

create unique index orders_share_token_key on public.orders (share_token);

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
      select jsonb_build_object('collection_name', co.collection_name)
      from public.collection_orders co
      where co.order_id = o.id
    )
  )
  from public.orders o
  where o.share_token = p_token;
$$;

revoke all on function public.get_shared_order(uuid) from public, anon, authenticated;
grant execute on function public.get_shared_order(uuid) to service_role;

create or replace function private.guard_order_status()
returns trigger
language plpgsql
set search_path = ''
as $$
begin
  if new.status is not distinct from old.status then
    return new;
  end if;

  if (select auth.uid()) is null then
    return new;
  end if;

  if new.status not in ('Pending', 'In Progress', 'Ready for Pickup', 'Completed', 'Cancelled') then
    raise exception 'That order status is not valid.' using errcode = '22023';
  end if;

  if not (select public.is_staff()) then
    raise exception 'Only staff can change an order status.' using errcode = '42501';
  end if;

  if (new.status = 'Cancelled' or old.status = 'Cancelled') and not (select public.is_admin()) then
    raise exception 'Only the owner can cancel or reopen an order.' using errcode = '42501';
  end if;

  return new;
end;
$$;

create trigger orders_guard_status before update of status on public.orders
  for each row execute function private.guard_order_status();
