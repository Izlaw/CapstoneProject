alter table public.custom_orders
  drop constraint custom_orders_design_parts_check;

alter table public.custom_orders
  add constraint custom_orders_design_parts_check check (
    not (design_data ? 'parts')
    or (
      jsonb_typeof(design_data -> 'parts') = 'object'
      and coalesce(design_data -> 'parts' ->> 'body' ~ '^#[0-9A-Fa-f]{6}$', false)
      and coalesce(design_data -> 'parts' ->> 'sleeves' ~ '^#[0-9A-Fa-f]{6}$', false)
      and coalesce(design_data -> 'parts' ->> 'collar' ~ '^#[0-9A-Fa-f]{6}$', false)
      and (not (design_data -> 'parts' ? 'front') or coalesce(design_data -> 'parts' ->> 'front' ~ '^#[0-9A-Fa-f]{6}$', false))
      and (not (design_data -> 'parts' ? 'back') or coalesce(design_data -> 'parts' ->> 'back' ~ '^#[0-9A-Fa-f]{6}$', false))
      and (not (design_data -> 'parts' ? 'leftSleeve') or coalesce(design_data -> 'parts' ->> 'leftSleeve' ~ '^#[0-9A-Fa-f]{6}$', false))
      and (not (design_data -> 'parts' ? 'rightSleeve') or coalesce(design_data -> 'parts' ->> 'rightSleeve' ~ '^#[0-9A-Fa-f]{6}$', false))
    )
  );
