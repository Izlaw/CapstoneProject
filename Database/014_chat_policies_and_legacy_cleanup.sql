drop table public.dtr_entries;
drop table public.notepad_tabs;

create policy "Customers can start their own conversation, staff can start any" on public.conversations
  for insert to authenticated
  with check (customer_id = (select auth.uid()) or (select public.is_staff()));

drop policy "Users can insert messages." on public.messages;
create policy "Users can send messages in their own conversations" on public.messages
  for insert to authenticated
  with check (
    sender_id = (select auth.uid())
    and (
      exists (
        select 1
        from public.conversations c
        where c.id = messages.conversation_id
          and c.customer_id = (select auth.uid())
      )
      or (select public.is_staff())
    )
  );
