drop policy "Staff can edit their pending or rejected submissions" on public.collections;

create policy "Staff can edit and resubmit their own designs" on public.collections
  for update to authenticated
  using (
    (select public.is_staff())
    and submitted_by = (select auth.uid())
  )
  with check (
    (select public.is_staff())
    and submitted_by = (select auth.uid())
    and status = 'pending'
    and not is_active
    and price = 0
    and design_data is not null
  );
