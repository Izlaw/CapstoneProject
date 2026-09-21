insert into storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
values ('collection-images', 'collection-images', true, 2097152, array['image/png', 'image/jpeg', 'image/webp'])
on conflict (id) do nothing;

create policy "Admins can upload collection images" on storage.objects
  for insert to authenticated
  with check (bucket_id = 'collection-images' and (select public.is_admin()));

create policy "Admins can update collection images" on storage.objects
  for update to authenticated
  using (bucket_id = 'collection-images' and (select public.is_admin()))
  with check (bucket_id = 'collection-images' and (select public.is_admin()));

create policy "Admins can delete collection images" on storage.objects
  for delete to authenticated
  using (bucket_id = 'collection-images' and (select public.is_admin()));
