grant update (design_image_path) on public.custom_orders to authenticated;

create policy "Admins can refresh custom order previews" on public.custom_orders
  for update to authenticated
  using ((select public.is_admin()))
  with check ((select public.is_admin()));
