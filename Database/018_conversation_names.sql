create or replace function public.get_conversation_names(p_conversation_id uuid)
returns jsonb
language sql
stable
security definer
set search_path = ''
as $$
  select coalesce(
    jsonb_object_agg(p.id::text, coalesce(nullif(btrim(p.full_name), ''), '-')),
    '{}'::jsonb
  )
  from public.profiles p
  join public.conversations c on c.id = p_conversation_id
  where (c.customer_id = (select auth.uid()) or (select public.is_staff()))
    and (
      p.id = c.customer_id
      or p.id in (
        select m.sender_id
        from public.messages m
        where m.conversation_id = p_conversation_id
      )
    );
$$;

revoke execute on function public.get_conversation_names(uuid) from public, anon;
grant execute on function public.get_conversation_names(uuid) to authenticated;
