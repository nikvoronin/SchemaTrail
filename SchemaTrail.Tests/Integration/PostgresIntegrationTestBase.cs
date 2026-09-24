namespace SchemaTrail.Tests.Integration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using System;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Base class for tests that exercise SchemaTrail against a real, local PostgreSQL
/// instance. Each test gets its own throwaway database, created before and dropped
/// after the test runs.
/// </summary>
[Trait( "Category", "Integration" )]
public abstract class PostgresIntegrationTestBase : IAsyncLifetime, IAsyncDisposable
{
    private const string MaintenanceConnectionString =
        "Host=localhost;Port=5432;Username=root;Database=root";

    private string _databaseName = null!;

    protected string ConnectionString { get; private set; } = null!;

    protected IDbContextFactory<MigrationsDbContext> DbContextFactory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _databaseName = $"schematrail_test_{Guid.NewGuid():N}";

        await using (var connection = new NpgsqlConnection( MaintenanceConnectionString )) {
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"create database \"{_databaseName}\";";
            await command.ExecuteNonQueryAsync();
        }

        ConnectionString =
            $"Host=localhost;Port=5432;Username=root;Database={_databaseName}";

        var options = new DbContextOptionsBuilder<MigrationsDbContext>()
            .UseNpgsql( ConnectionString )
            .Options;

        DbContextFactory = new PooledDbContextFactory<MigrationsDbContext>( options );
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection( MaintenanceConnectionString );
        await connection.OpenAsync();

        await using (var terminate = connection.CreateCommand()) {
            terminate.CommandText =
                "select pg_terminate_backend(pid) from pg_stat_activity "
                + "where datname = @db and pid <> pg_backend_pid();";
            terminate.Parameters.AddWithValue( "db", _databaseName );
            await terminate.ExecuteNonQueryAsync();
        }

        await using (var drop = connection.CreateCommand()) {
            drop.CommandText = $"drop database if exists \"{_databaseName}\";";
            await drop.ExecuteNonQueryAsync();
        }
    }
}
