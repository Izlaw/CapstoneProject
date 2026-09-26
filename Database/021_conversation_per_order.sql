alter table public.conversations
  add column order_id uuid references public.orders (id) on delete cascade;

create unique index conversations_order_id_key on public.conversations (order_id);
create index conversations_customer_id_idx on public.conversations (customer_id);

drop policy "Customers can start their own conversation, staff can start any" on public.conversations;
create policy "Customers can start a chat for their own order, staff for any order" on public.conversations
  for insert to authenticated
  with check (
    order_id is not null
    and exists (
      select 1
      from public.orders o
      where o.id = conversations.order_id
        and o.customer_id = conversations.customer_id
    )
    and (customer_id = (select auth.uid()) or (select public.is_staff()))
  );
