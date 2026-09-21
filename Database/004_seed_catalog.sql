insert into public.sizes (name, price, width_in, length_in, sort_order) values
  ('4XS', 280.00, 15, 18, 10),
  ('3XS', 285.00, 16, 19, 20),
  ('2XS', 290.00, 17, 25, 30),
  ('XS',  295.00, 19, 26, 40),
  ('S',   300.00, 20, 27, 50),
  ('M',   300.00, 21, 28, 60),
  ('L',   300.00, 22, 29, 70),
  ('XL',  310.00, 23, 30, 80),
  ('XXL', 320.00, 24, 31, 90),
  ('XXXL',330.00, 25, 32, 100),
  ('4XL', 340.00, 26, 33, 110),
  ('5XL', 350.00, 27, 34, 120),
  ('6XL', 360.00, 28, 35, 130)
on conflict do nothing;

insert into public.fabrics (name, price, requires_note, sort_order) values
  ('Cotton',    120.00, false, 10),
  ('Polyester', 300.00, false, 20),
  ('Custom',    500.00, true,  30)
on conflict do nothing;

insert into public.timeframes (label, surcharge_percent, max_quantity, sort_order) values
  ('1 week',  25.00, 150, 10),
  ('2 weeks', 15.00, 300, 20),
  ('3 weeks',  5.00, 450, 30)
on conflict do nothing;
