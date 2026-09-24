# SchemaTrail

A tiny, PostgreSQL-only SQL migration library for .NET.

> [!NOTE]
> **SchemaTrail** is intentionally small and opinionated. If you want a more powerful production-grade migration solution, use [DbUp](https://github.com/dbup/dbup) or [FluentMigrator](https://github.com/fluentmigrator/fluentmigrator) instead.

**SchemaTrail** is built for developers that want migrations to stay simple, explicit, and reviewable:

- write plain SQL files;
- apply them in order;
- track what was applied;
- keep an audit trail of migration runs;
- recover safely after crashes or abrupt shutdowns.

**SchemaTrail** does not try to be a database-agnostic framework. It is intentionally focused on PostgreSQL and on a script-first workflow.

## Why SchemaTrail?

Because sometimes you do not need a giant migration platform.

**SchemaTrail** is designed for projects that want:

- small surface area;
- predictable behavior;
- SQL-first migrations instead of generated diffs;
- PostgreSQL-specific safety mechanisms such as advisory locks;
- visibility into migration execution history.

## Features

- Plain SQL migration scripts, paired with down-migration scripts
- Ordered version-based execution
- PostgreSQL advisory lock to prevent concurrent migration runners
- Applied migrations table
- Migration run history table
- Automatic rollback to the pre-run state if a migration fails mid-batch
- Manual revert to an arbitrary target version
- Recovery of unfinished migration runs after unexpected process termination
- Validation of already applied migrations against the current build
- Transactional execution of each individual migration (and its rollback)

## Non-goals

**SchemaTrail** is intentionally **not** trying to provide:

- multi-database support;
- model snapshot generation;
- automatic schema diff generation;
- a GUI;
- a huge abstraction layer over SQL.

If you want full control over schema evolution and prefer reviewing SQL directly, SchemaTrail is a better fit.

## How it works

**SchemaTrail** applies migrations in ascending version order.

For each migration, it:

1. acquires a PostgreSQL advisory lock for the process;
2. ensures migration infrastructure tables exist;
3. loads already applied migrations;
4. recovers dangling migration runs left in the `running` state;
5. validates that previously applied migrations still match the current build;
6. records a new migration run;
7. executes the SQL inside a transaction;
8. records the migration as applied;
9. marks the migration run as `success` or `failed`.

If a migration fails, SchemaTrail automatically reverts every migration that was applied *during
that run*, in reverse order, using each migration's down script — restoring the database to the
state it was in before the run started. See [Rollback](#rollback).

This gives you both:

- a durable record of schema state;
- a durable record of execution attempts.

## Installation

Add the package to your application:

```bash
dotnet add package SchemaTrail
```

Then configure PostgreSQL and register the services in DI.

## Migration naming convention

Every migration ships as a pair of files: an `up` script that applies the change, and a `down`
script that reverts it. Both are **required** — SchemaTrail fails fast at startup if either side
of a version is missing.

A typical naming convention is:

```text
V001__Create_users.up.sql
V001__Create_users.down.sql
V002__Add_email_index.up.sql
V002__Add_email_index.down.sql
```

Recommended format:

```text
V{version}__{description}.up.sql
V{version}__{description}.down.sql
```

Examples:

- V001__Init.up.sql / V001__Init.down.sql
- V002__Create_users_table.up.sql / V002__Create_users_table.down.sql
- V003__Add_order_status_index.up.sql / V003__Add_order_status_index.down.sql

Use monotonically increasing integer versions. Never reuse or rewrite a version that has already been applied.

## Example migration

`V002__Create_users_table.up.sql`:

```sql
create table if not exists users
(
    id uuid primary key,
    email text not null,
    created_at timestamp with time zone not null default (now() at time zone 'utc')
);

create unique index if not exists ix_users_email
    on users (email);
```

`V002__Create_users_table.down.sql`:

```sql
drop index if exists ix_users_email;

drop table if exists users;
```

## Basic registration

The example below uses:

- `MigrationsDbContext` for SchemaTrail storage;
- a custom `ISqlScriptsProvider` implementation;
- the built-in execution and run services;
- `IMigrationsApplier` as the orchestration entry point.

```csharp
namespace WebApi.Example;

using Microsoft.EntityFrameworkCore;
using SchemaTrail;

var builder = WebApplication.CreateBuilder( args );

var connectionString =
    builder.Configuration.GetConnectionString( "DefaultConnection" )
    ?? throw new InvalidOperationException(
        "Connection string is not configured." );

builder.Services
    .AddPooledDbContextFactory<MigrationsDbContext>( options => {
        options.UseNpgsql( connectionString );
    } );

builder.Services
    .AddScoped<ISqlScriptsProvider, EmbeddedSqlScriptsProvider>( sp =>
        new EmbeddedSqlScriptsProvider(
            Assembly.GetExecutingAssembly(),
            "WebApi.Example.Migrations" ) )
    //.AddScoped<ISqlScriptsProvider, FileSystemSqlScriptsProvider>( sp =>
    //    new FileSystemSqlScriptsProvider( ".../Migrations/Scripts" ) )
    .AddScoped<IMigrationRunService, MigrationRunService>()
    .AddScoped<IMigrationExecutionService, MigrationExecutionService>()
    .AddScoped<IMigrationsApplier, MigrationsApplier>();
```

## Applying migrations at startup

```csharp
var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope()) {
    var migrationsApplier = scope.ServiceProvider.GetRequiredService<IMigrationsApplier>();
    await migrationsApplier.ApplyAsync( app.Lifetime.ApplicationStopping );
}

await app.RunAsync();
```

## Rollback

SchemaTrail can undo applied migrations using their down scripts, in two ways:

- **Automatic**, on failure: if a migration fails during `ApplyAsync`, every migration applied
  during that same call is reverted, in reverse order, before the original exception is rethrown.
  This restores the database to the state it was in before the run started. If a revert itself
  fails, the original error and the revert error are both surfaced together (as an
  `AggregateException`), since the database may then be left in a partially reverted state that
  needs manual inspection.
- **Manual**, on demand: call `RevertAsync` to revert applied migrations down to an arbitrary
  target version.

```csharp
await using (var scope = app.Services.CreateAsyncScope()) {
    var migrationsApplier = scope.ServiceProvider.GetRequiredService<IMigrationsApplier>();

    // Reverts every applied migration with a version greater than 2,
    // i.e. leaves the database at the state produced by V002.
    await migrationsApplier.RevertAsync( targetVersion: 2, app.Lifetime.ApplicationStopping );
}
```

Each reverted migration is recorded in the migration run history with a `rolled_back` status.

## Register scripts for embedded provider

Just set file mask in `.csproj`

```xml
<ItemGroup>
  <EmbeddedResource Include="Migrations\*.sql" />
</ItemGroup>
```

## What SchemaTrail stores

**SchemaTrail** typically maintains two kinds of records:

### 1. Applied migrations

A table that represents the schema history of the database.

Typical fields:

- version
- script name
- description
- applied at

### 2. Migration runs

A table that represents execution attempts.

Typical fields:

- id
- version
- script name
- description
- status (`running`, `success`, `failed`, or `rolled_back`)
- direction (`up` or `down`)
- started at
- completed at
- duration
- error text

This separation is useful because it answers two different questions:

- **What schema changes are already part of the database state?**
- **What exactly happened while the process was trying to apply them?**

## Failure recovery

If the process crashes after starting a migration run but before completing it, SchemaTrail can reconcile that state during the next startup.

Typical recovery behavior:

- for a dangling `up` run: if the migration was actually applied and matches the recorded metadata, it can be marked as `success`; if it was not applied, it can be marked as `failed`;
- for a dangling `down` run: if the migration is no longer present in the applied-migrations record, the revert actually went through and it can be marked as `rolled_back`; if it is still present, the revert never took effect and it can be marked as `failed`;
- stale competing runs for the same migration and direction can also be marked as `failed`.

This gives you a much clearer operational history than a single applied-migrations table alone.

## Validation and drift detection

**SchemaTrail** validates previously applied migrations against the current build.

That includes checking:

- version existence;
- script name;
- description.

This protects against dangerous situations such as:

- running an older build against a newer database;
- renaming already applied migration files.

If a mismatch is detected, SchemaTrail fails fast.

## Why PostgreSQL-only?

**SchemaTrail** intentionally embraces PostgreSQL-specific behavior instead of hiding it.

That allows the library to use:

- advisory locks for safe single-runner execution;
- PostgreSQL SQL syntax directly in migrations;
- PostgreSQL-oriented infrastructure without lowest-common-denominator abstractions.

The result is a smaller and simpler library.

## Recommended workflow

1. Create a new pair of SQL files (`.up.sql` / `.down.sql`) with the next version number.
2. Write the schema change in plain PostgreSQL SQL, and its exact reverse in the down script.
3. Review the migration like regular source code.
4. Merge and deploy the application.
5. Let SchemaTrail apply pending migrations at startup or during a deployment step.

## Best practices

- Keep one logical change per migration.
- Never edit an already applied migration.
- Never reuse a version number.
- Prefer additive changes where possible.
- Make destructive changes explicit and reviewed.
- Keep the down script an exact, tested inverse of the up script.
- Test migrations, including their rollback, against a real PostgreSQL instance before production rollout.

## Example project structure

```text
src/
  MyApp/
    Program.cs
    Migrations/
      V001__Init.up.sql
      V001__Init.down.sql
      V002__Create_users_table.up.sql
      V002__Create_users_table.down.sql
      V003__Add_email_index.up.sql
      V003__Add_email_index.down.sql
```

## Logging

**SchemaTrail** is designed to produce useful operational logs such as:

- migration started;
- migration succeeded;
- migration failed;
- migration reverted, automatically or manually;
- dangling migration runs recovered;
- missing run records during reconciliation.

This is especially useful in CI/CD pipelines and containerized deployments.

## When to use SchemaTrail

**SchemaTrail** is a good fit if you want:

- a tiny PostgreSQL migration library;
- SQL to remain the source of truth;
- migration drift detection;
- execution auditability;
- predictable startup behavior.

**SchemaTrail** is probably not the right tool if you want:

- cross-database support;
- code-generated migration graphs;
- ORM-first migration authoring;
- a large plugin ecosystem.

## Status

**SchemaTrail** is intentionally small and opinionated.  
That is part of the design, not a missing feature.

## License

MIT
