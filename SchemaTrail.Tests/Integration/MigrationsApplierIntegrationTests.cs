namespace SchemaTrail.Tests.Integration;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SchemaTrail.Abstractions;
using SchemaTrail.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class MigrationsApplierIntegrationTests : PostgresIntegrationTestBase
{
    [Fact]
    public async Task ApplyAsync_WhenLaterMigrationFails_RevertsPreviouslyAppliedMigrationsInThisRun()
    {
        // Arrange
        var scripts = new[]
        {
            new SqlScriptMigration(
                1, "V001__Create_widgets.up.sql", "Create widgets",
                "create table widgets (id int primary key);",
                "V001__Create_widgets.down.sql", "drop table widgets;" ),
            new SqlScriptMigration(
                2, "V002__Create_gadgets.up.sql", "Create gadgets",
                "create table gadgets (id int primary key);",
                "V002__Create_gadgets.down.sql", "drop table gadgets;" ),
            new SqlScriptMigration(
                3, "V003__Broken.up.sql", "Broken",
                "this is not valid sql;",
                "V003__Broken.down.sql", "select 1;" ),
        };

        var scriptsProviderMock = new Mock<ISqlScriptsProvider>();
        scriptsProviderMock.Setup( x => x.GetMigrationScripts() ).Returns( scripts );

        var applier = new MigrationsApplier(
            DbContextFactory,
            scriptsProviderMock.Object,
            new MigrationRunService( NullLogger<MigrationRunService>.Instance ),
            new MigrationExecutionService(),
            NullLogger<MigrationsApplier>.Instance );

        // Act
        Func<Task> act = () => applier.ApplyAsync( CancellationToken.None );

        // Assert - original failure surfaces, not wrapped, since rollback itself succeeded
        var thrown = await act.Should().ThrowAsync<Exception>();
        thrown.Which.Should().NotBeOfType<AggregateException>();

        await using var context = await DbContextFactory.CreateDbContextAsync();

        (await TableExistsAsync( context, "widgets" )).Should().BeFalse();
        (await TableExistsAsync( context, "gadgets" )).Should().BeFalse();

        var applied = await context.AppliedMigrations.ToListAsync();
        applied.Should().BeEmpty();

        var runs = await context.MigrationRuns.OrderBy( x => x.Id ).ToListAsync();
        runs.Should().Contain( x => x.Version == 1 && x.Status == MigrationRunStatuses.Success );
        runs.Should().Contain( x => x.Version == 2 && x.Status == MigrationRunStatuses.Success );
        runs.Should().Contain( x => x.Version == 3 && x.Status == MigrationRunStatuses.Failed );
        runs.Should().Contain( x => x.Version == 1 && x.Status == MigrationRunStatuses.RolledBack );
        runs.Should().Contain( x => x.Version == 2 && x.Status == MigrationRunStatuses.RolledBack );
    }

    [Fact]
    public async Task RevertAsync_RevertsAppliedMigrationsAboveTargetVersion()
    {
        // Arrange
        var scripts = new[]
        {
            new SqlScriptMigration(
                1, "V001__Create_widgets.up.sql", "Create widgets",
                "create table widgets (id int primary key);",
                "V001__Create_widgets.down.sql", "drop table widgets;" ),
            new SqlScriptMigration(
                2, "V002__Create_gadgets.up.sql", "Create gadgets",
                "create table gadgets (id int primary key);",
                "V002__Create_gadgets.down.sql", "drop table gadgets;" ),
        };

        var scriptsProviderMock = new Mock<ISqlScriptsProvider>();
        scriptsProviderMock.Setup( x => x.GetMigrationScripts() ).Returns( scripts );

        var applier = new MigrationsApplier(
            DbContextFactory,
            scriptsProviderMock.Object,
            new MigrationRunService( NullLogger<MigrationRunService>.Instance ),
            new MigrationExecutionService(),
            NullLogger<MigrationsApplier>.Instance );

        await applier.ApplyAsync( CancellationToken.None );

        // Act
        await applier.RevertAsync( targetVersion: 1, CancellationToken.None );

        // Assert
        await using var context = await DbContextFactory.CreateDbContextAsync();

        (await TableExistsAsync( context, "widgets" )).Should().BeTrue();
        (await TableExistsAsync( context, "gadgets" )).Should().BeFalse();

        var applied = await context.AppliedMigrations.Select( x => x.Version ).ToListAsync();
        applied.Should().BeEquivalentTo( new[] { 1 } );
    }

    private static async Task<bool> TableExistsAsync( MigrationsDbContext context, string tableName )
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            "select exists (select 1 from information_schema.tables "
            + "where table_schema = 'public' and table_name = @tableName);";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tableName";
        parameter.Value = tableName;
        command.Parameters.Add( parameter );

        if (command.Connection!.State != System.Data.ConnectionState.Open) {
            await context.Database.OpenConnectionAsync();
        }

        var result = await command.ExecuteScalarAsync();
        return result is true;
    }
}
