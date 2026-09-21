alter table public.orders
  add column fabric_id uuid references public.fabrics (id) on delete restrict,
  add column fabric_name text,
  add column custom_fabric_note text,
  add column collection_id uuid references public.collections (id) on delete restrict,
  add column collection_name text,
  add column base_unit_price numeric(12,2) check (base_unit_price >= 0),
  add column timeframe_id uuid references public.timeframes (id) on delete restrict,
  add column timeframe_label text,
  add column surcharge_percent numeric(5,2) check (surcharge_percent between 0 and 100),
  add column subtotal numeric(12,2) check (subtotal >= 0),
  add column surcharge_amount numeric(12,2) check (surcharge_amount >= 0),
  add column updated_at timestamptz not null default now(),
  alter column total_price type numeric(12,2),
  add constraint orders_total_price_check check (total_price >= 0);

create table public.order_items (
  id uuid primary key default gen_random_uuid(),
  order_id uuid not null references public.orders (id) on delete cascade,
  size_id uuid not null references public.sizes (id) on delete restrict,
  size_name text not null,
  size_price numeric(12,2) not null check (size_price >= 0),
  unit_price numeric(12,2) not null check (unit_price >= 0),
  quantity integer not null check (quantity > 0),
  line_total numeric(12,2) not null check (line_total >= 0),
  created_at timestamptz not null default now(),
  unique (order_id, size_id)
);

create index orders_customer_id_idx on public.orders (customer_id);
create index orders_created_at_idx on public.orders (created_at desc);
create index orders_fabric_id_idx on public.orders (fabric_id);
create index orders_collection_id_idx on public.orders (collection_id);
create index orders_timeframe_id_idx on public.orders (timeframe_id);
create index order_items_order_id_idx on public.order_items (order_id);
create index order_items_size_id_idx on public.order_items (size_id);

create trigger orders_set_updated_at before update on public.orders
  for each row execute function private.set_updated_at();

alter table public.order_items enable row level security;

create policy "Customers can view their own order items" on public.order_items
  for select to authenticated
  using (exists (
    select 1 from public.orders
    where orders.id = order_items.order_id and orders.customer_id = (select auth.uid())
  ));
create policy "Staff can view all order items" on public.order_items
  for select to authenticated using ((select public.is_staff()));

revoke all on public.order_items from anon, authenticated;
grant select on public.order_items to authenticated;

revoke all on public.orders from anon, authenticated;
grant select, insert on public.orders to authenticated;
grant update (status) on public.orders to authenticated;
