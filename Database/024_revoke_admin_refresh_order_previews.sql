drop policy "Admins can refresh custom order previews" on public.custom_orders;

revoke update (design_image_path) on public.custom_orders from authenticated;
