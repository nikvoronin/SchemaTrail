namespace SchemaTrail.Tests.Integration;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchemaTrail.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class MigrationExecutionServiceIntegrationTests : PostgresIntegrationTestBase
{
    [Fact]
    public async Task ApplyThenRevertSingleMigrationAsync_UndoesSchemaChangeAndAppliedRecord()
    {
        // Arrange
        var service = new MigrationExecutionService();

        var script = new SqlScriptMigration(
            1,
            "V001__Create_widgets.up.sql",
            "Create widgets",
            "create table widgets (id int primary key);",
            "V001__Create_widgets.down.sql",
            "drop table widgets;" );

        await using var context = await DbContextFactory.CreateDbContextAsync();

        await service.EnsureInfrastructureTablesExistAsync( context, CancellationToken.None );

        // Act - apply
        await service.ApplySingleMigrationAsync( context, script, CancellationToken.None );

        // Assert - applied
        (await TableExistsAsync( context, "widgets" )).Should().BeTrue();
        var appliedAfterUp = await service.LoadAppliedMigrationsAsync( context, CancellationToken.None );
        appliedAfterUp.Should().ContainKey( 1 );

        // Act - revert
        await service.RevertSingleMigrationAsync( context, script, CancellationToken.None );

        // Assert - reverted
        (await TableExistsAsync( context, "widgets" )).Should().BeFalse();
        var appliedAfterDown = await service.LoadAppliedMigrationsAsync( context, CancellationToken.None );
        appliedAfterDown.Should().NotContainKey( 1 );
    }

    [Fact]
    public async Task RevertSingleMigrationAsync_WhenDownScriptFails_RollsBackTransaction()
    {
        // Arrange
        var service = new MigrationExecutionService();

        var script = new SqlScriptMigration(
            1,
            "V001__Create_widgets.up.sql",
            "Create widgets",
            "create table widgets (id int primary key);",
            "V001__Create_widgets.down.sql",
            "drop table this_table_does_not_exist;" );

        await using var context = await DbContextFactory.CreateDbContextAsync();

        await service.EnsureInfrastructureTablesExistAsync( context, CancellationToken.None );
        await service.ApplySingleMigrationAsync( context, script, CancellationToken.None );

        // Act
        Func<Task> act = () => service.RevertSingleMigrationAsync( context, script, CancellationToken.None );

        // Assert - down script failed, nothing should have been undone
        await act.Should().ThrowAsync<Exception>();

        (await TableExistsAsync( context, "widgets" )).Should().BeTrue();
        var applied = await service.LoadAppliedMigrationsAsync( context, CancellationToken.None );
        applied.Should().ContainKey( 1 );
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
