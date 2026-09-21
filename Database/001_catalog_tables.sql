create schema if not exists private;
revoke all on schema private from public;

create or replace function private.set_updated_at()
returns trigger
language plpgsql
set search_path = ''
as $$
begin
  new.updated_at := now();
  return new;
end;
$$;

create or replace function public.is_admin()
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
  select exists (
    select 1 from public.profiles
    where id = (select auth.uid()) and role = 'admin'
  );
$$;

create or replace function public.is_staff()
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
  select exists (
    select 1 from public.profiles
    where id = (select auth.uid()) and role in ('employee', 'admin')
  );
$$;

revoke all on function public.is_admin() from public;
revoke all on function public.is_staff() from public;
grant execute on function public.is_admin() to anon, authenticated;
grant execute on function public.is_staff() to anon, authenticated;

create table public.sizes (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  price numeric(12,2) not null check (price >= 0),
  width_in numeric(6,2) check (width_in > 0),
  length_in numeric(6,2) check (length_in > 0),
  sort_order integer not null default 0,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
create unique index sizes_name_key on public.sizes (lower(name));

create table public.fabrics (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  price numeric(12,2) not null check (price >= 0),
  requires_note boolean not null default false,
  sort_order integer not null default 0,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
create unique index fabrics_name_key on public.fabrics (lower(name));

create table public.timeframes (
  id uuid primary key default gen_random_uuid(),
  label text not null,
  surcharge_percent numeric(5,2) not null default 0 check (surcharge_percent between 0 and 100),
  max_quantity integer check (max_quantity > 0),
  sort_order integer not null default 0,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
create unique index timeframes_label_key on public.timeframes (lower(label));

alter table public.collections
  add column sort_order integer not null default 0,
  add column updated_at timestamptz not null default now(),
  alter column is_active set not null,
  alter column price type numeric(12,2),
  add constraint collections_price_check check (price >= 0);

create table public.price_audit_log (
  id bigint generated always as identity primary key,
  table_name text not null,
  row_id uuid not null,
  row_label text,
  field text not null,
  old_value text,
  new_value text,
  changed_by uuid,
  changed_at timestamptz not null default now()
);
create index price_audit_log_changed_at_idx on public.price_audit_log (changed_at desc);

create or replace function private.log_price_change()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_column text;
  v_old text;
  v_new text;
begin
  foreach v_column in array tg_argv loop
    v_old := to_jsonb(old) ->> v_column;
    v_new := to_jsonb(new) ->> v_column;
    if v_old is distinct from v_new then
      insert into public.price_audit_log (table_name, row_id, row_label, field, old_value, new_value, changed_by)
      values (
        tg_table_name,
        new.id,
        coalesce(to_jsonb(new) ->> 'name', to_jsonb(new) ->> 'label'),
        v_column,
        v_old,
        v_new,
        (select auth.uid())
      );
    end if;
  end loop;
  return null;
end;
$$;

create trigger sizes_set_updated_at before update on public.sizes
  for each row execute function private.set_updated_at();
create trigger fabrics_set_updated_at before update on public.fabrics
  for each row execute function private.set_updated_at();
create trigger timeframes_set_updated_at before update on public.timeframes
  for each row execute function private.set_updated_at();
create trigger collections_set_updated_at before update on public.collections
  for each row execute function private.set_updated_at();

create trigger sizes_log_price_change after update on public.sizes
  for each row execute function private.log_price_change('price');
create trigger fabrics_log_price_change after update on public.fabrics
  for each row execute function private.log_price_change('price');
create trigger timeframes_log_price_change after update on public.timeframes
  for each row execute function private.log_price_change('surcharge_percent', 'max_quantity');
create trigger collections_log_price_change after update on public.collections
  for each row execute function private.log_price_change('price');

alter table public.sizes enable row level security;
alter table public.fabrics enable row level security;
alter table public.timeframes enable row level security;
alter table public.price_audit_log enable row level security;

create policy "Active sizes are viewable by everyone" on public.sizes
  for select using (is_active or (select public.is_admin()));
create policy "Admins can manage sizes" on public.sizes
  for all to authenticated
  using ((select public.is_admin())) with check ((select public.is_admin()));

create policy "Active fabrics are viewable by everyone" on public.fabrics
  for select using (is_active or (select public.is_admin()));
create policy "Admins can manage fabrics" on public.fabrics
  for all to authenticated
  using ((select public.is_admin())) with check ((select public.is_admin()));

create policy "Active timeframes are viewable by everyone" on public.timeframes
  for select using (is_active or (select public.is_admin()));
create policy "Admins can manage timeframes" on public.timeframes
  for all to authenticated
  using ((select public.is_admin())) with check ((select public.is_admin()));

drop policy "Collections are viewable by everyone." on public.collections;
drop policy "Only Admins can modify collections." on public.collections;
create policy "Active collections are viewable by everyone" on public.collections
  for select using (is_active or (select public.is_admin()));
create policy "Admins can manage collections" on public.collections
  for all to authenticated
  using ((select public.is_admin())) with check ((select public.is_admin()));

create policy "Admins can view the price audit log" on public.price_audit_log
  for select to authenticated using ((select public.is_admin()));

revoke all on public.sizes, public.fabrics, public.timeframes, public.price_audit_log from anon, authenticated;
grant select on public.sizes, public.fabrics, public.timeframes to anon, authenticated;
grant insert, update, delete on public.sizes, public.fabrics, public.timeframes to authenticated;
grant select on public.price_audit_log to authenticated;
