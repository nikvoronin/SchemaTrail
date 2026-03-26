create table if not exists app.users
(
    id uuid primary key,
    email text not null,
    created_at timestamp with time zone not null default (now() at time zone 'utc')
);

create unique index if not exists ix_users_email
    on app.users (email);