create table public.profiles (
  id uuid primary key references auth.users (id) on delete cascade,
  full_name text,
  role text default 'customer' check (role in ('customer', 'employee', 'admin')),
  address text,
  phone text,
  created_at timestamptz not null default timezone('utc', now())
);

create table public.collections (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  price numeric not null,
  image_url text,
  is_active boolean default true,
  created_at timestamptz not null default timezone('utc', now())
);

create table public.orders (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid references public.profiles (id) on delete cascade,
  order_type text check (order_type in ('custom', 'upload', 'collection')),
  status text default 'Pending' check (status in ('Pending', 'In Progress', 'Ready for Pickup', 'Completed', 'Cancelled')),
  total_price numeric not null,
  fabric_type text,
  size text,
  quantity integer default 1,
  timeframe text,
  design_reference text,
  created_at timestamptz not null default timezone('utc', now())
);

create table public.conversations (
  id uuid primary key default gen_random_uuid(),
  customer_id uuid references public.profiles (id) on delete cascade,
  status text default 'open' check (status in ('open', 'closed')),
  updated_at timestamptz not null default timezone('utc', now()),
  created_at timestamptz not null default timezone('utc', now())
);

create table public.messages (
  id uuid primary key default gen_random_uuid(),
  conversation_id uuid references public.conversations (id) on delete cascade,
  sender_id uuid references public.profiles (id) on delete cascade,
  content text not null,
  created_at timestamptz not null default timezone('utc', now())
);

create table public.dtr_entries (
  id uuid primary key default gen_random_uuid()
);

create table public.notepad_tabs (
  id uuid primary key default gen_random_uuid()
);

create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer
as $$
begin
  insert into public.profiles (id, full_name, role)
  values (new.id, new.raw_user_meta_data->>'full_name', 'customer');
  return new;
end;
$$;

create trigger on_auth_user_created after insert on auth.users
  for each row execute function public.handle_new_user();

alter table public.profiles enable row level security;
alter table public.collections enable row level security;
alter table public.orders enable row level security;
alter table public.conversations enable row level security;
alter table public.messages enable row level security;

create policy "Profiles are viewable by everyone." on public.profiles
  for select using (true);
create policy "Users can update own profile." on public.profiles
  for update using (auth.uid() = id);

create policy "Collections are viewable by everyone." on public.collections
  for select using (true);
create policy "Only Admins can modify collections." on public.collections
  for all using (exists (
    select 1 from public.profiles
    where profiles.id = auth.uid() and profiles.role = 'admin'
  ));

create policy "Customers can view their own orders." on public.orders
  for select using (auth.uid() = customer_id);
create policy "Customers can insert their own orders." on public.orders
  for insert with check (auth.uid() = customer_id);
create policy "Employees/Admins can view and update orders." on public.orders
  for all using (exists (
    select 1 from public.profiles
    where profiles.id = auth.uid() and profiles.role in ('employee', 'admin')
  ));

create policy "Customers can view their conversations." on public.conversations
  for select using (auth.uid() = customer_id);
create policy "Employees/Admins can view all conversations." on public.conversations
  for select using (exists (
    select 1 from public.profiles
    where profiles.id = auth.uid() and profiles.role in ('employee', 'admin')
  ));

create policy "Users can view messages in their conversations." on public.messages
  for select using (
    exists (
      select 1 from public.conversations
      where conversations.id = messages.conversation_id and conversations.customer_id = auth.uid()
    )
    or exists (
      select 1 from public.profiles
      where profiles.id = auth.uid() and profiles.role in ('employee', 'admin')
    )
  );
create policy "Users can insert messages." on public.messages
  for insert with check (auth.uid() = sender_id);
