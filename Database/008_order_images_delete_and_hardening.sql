create policy "Customers can delete their own order images"
on storage.objects for delete
to authenticated
using (
  bucket_id = 'order-images'
  and (storage.foldername(name))[1] = (select auth.uid())::text
);

alter function public.handle_new_user() set search_path = '';

revoke execute on function public.handle_new_user() from public, anon, authenticated;
