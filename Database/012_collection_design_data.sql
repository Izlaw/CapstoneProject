alter table public.collections
  add column design_data jsonb;

alter table public.collections
  add constraint collections_design_data_check check (
    design_data is null
    or (
      jsonb_typeof(design_data) = 'object'
      and jsonb_typeof(design_data -> 'elements') = 'array'
      and jsonb_array_length(design_data -> 'elements') <= 10
      and char_length(design_data::text) <= 10000
      and jsonb_typeof(design_data -> 'parts') = 'object'
      and coalesce(design_data -> 'parts' ->> 'body' ~ '^#[0-9A-Fa-f]{6}$', false)
      and coalesce(design_data -> 'parts' ->> 'sleeves' ~ '^#[0-9A-Fa-f]{6}$', false)
      and coalesce(design_data -> 'parts' ->> 'collar' ~ '^#[0-9A-Fa-f]{6}$', false)
      and not jsonb_path_exists(design_data, '$.elements[*] ? (@.type != "text")')
      and not jsonb_path_exists(design_data, '$.elements[*] ? (!(@.color like_regex "^#[0-9A-Fa-f]{6}$"))')
    )
  );
