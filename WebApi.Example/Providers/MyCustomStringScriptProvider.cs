using SchemaTrail.Abstractions;
using SchemaTrail.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApi.Example.Providers;

/// <summary>
/// Provides a collection of SQL migration scripts defined directly in code.
/// </summary>
internal class MyCustomStringScriptProvider : ISqlScriptsProvider
{
    /// <inheritdoc/>
    public IReadOnlyList<SqlScriptMigration> GetMigrationScripts() => _scripts;

    /// <inheritdoc/>
    public string ReadToEndRequiredScript( string resourceName ) =>
        _scripts.FirstOrDefault( x => x.ScriptName == resourceName )
        ?.Sql
        ?? throw new InvalidOperationException(
            $"Migration script '{resourceName}' was not found." );

    private SqlScriptMigration[] _scripts = [
        new SqlScriptMigration(
            1, "Initialize",
            "Initialize app schema",
            """
            create schema if not exists app;
            """),
        new SqlScriptMigration(
            2, "Create users table",
            "Create users table",
            """
            create table if not exists app.users
            (
                id uuid primary key,
                email text not null,
                created_at timestamp with time zone not null default (now() at time zone 'utc')
            );

            create unique index if not exists ix_users_email
                on app.users (email);
            """),
    ];
}
