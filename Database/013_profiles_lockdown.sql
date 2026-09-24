revoke insert, update on public.profiles from anon, authenticated;
grant update (full_name, phone, address) on public.profiles to authenticated;

drop policy "Profiles are viewable by everyone." on public.profiles;
create policy "Users can view own profile, staff can view all" on public.profiles
  for select to authenticated
  using (id = (select auth.uid()) or (select public.is_staff()));

drop policy "Users can update own profile." on public.profiles;
create policy "Users can update own profile" on public.profiles
  for update to authenticated
  using (id = (select auth.uid()))
  with check (id = (select auth.uid()));
