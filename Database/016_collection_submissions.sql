alter table public.collections
  add column status text not null default 'approved',
  add column submitted_by uuid references auth.users (id) on delete set null,
  add column rejection_reason text,
  add constraint collections_status_check check (status in ('pending', 'approved', 'rejected')),
  add constraint collections_active_requires_approval_check check (status = 'approved' or not is_active),
  add constraint collections_rejection_reason_check check (
    (status = 'rejected') = (rejection_reason is not null)
    and (rejection_reason is null or (btrim(rejection_reason) <> '' and char_length(rejection_reason) <= 500))
  );

drop policy "Active collections are viewable by everyone" on public.collections;
create policy "Active collections are viewable by everyone, submitters can view their own" on public.collections
  for select using (
    is_active
    or (select public.is_admin())
    or submitted_by = (select auth.uid())
  );

create policy "Staff can submit designs for approval" on public.collections
  for insert to authenticated
  with check (
    (select public.is_staff())
    and submitted_by = (select auth.uid())
    and status = 'pending'
    and not is_active
    and price = 0
    and design_data is not null
  );

create policy "Staff can edit their pending or rejected submissions" on public.collections
  for update to authenticated
  using (
    (select public.is_staff())
    and submitted_by = (select auth.uid())
    and status in ('pending', 'rejected')
  )
  with check (
    (select public.is_staff())
    and submitted_by = (select auth.uid())
    and status = 'pending'
    and not is_active
    and price = 0
    and design_data is not null
  );

drop policy "Admins can upload collection images" on storage.objects;
create policy "Staff can upload collection images" on storage.objects
  for insert to authenticated
  with check (bucket_id = 'collection-images' and (select public.is_staff()));
