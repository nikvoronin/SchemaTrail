using SchemaTrail.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SchemaTrail.Abstractions;

/// <summary>
/// Provides operations for preparing migration storage,
/// loading applied migrations, validating migration metadata,
/// and executing individual SQL migrations.
/// </summary>
public interface IMigrationExecutionService
{
    /// <summary>
    /// Ensures that the migration infrastructure tables required by the library exist.
    /// </summary>
    /// <param name="context">The database context used to access migration storage.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task EnsureInfrastructureTablesExistAsync(
        MigrationsDbContext context,
        CancellationToken token );

    /// <summary>
    /// Loads all migrations that have already been applied to the database.
    /// </summary>
    /// <param name="context">The database context used to access migration storage.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>
    /// A dictionary keyed by migration version and containing metadata of applied migrations.
    /// </returns>
    Task<Dictionary<int, AppliedMigration>> LoadAppliedMigrationsAsync(
        MigrationsDbContext context,
        CancellationToken token );

    /// <summary>
    /// Validates that the migrations already applied to the database
    /// match the migrations available in the current application build.
    /// </summary>
    /// <param name="scripts">The migrations available in the current build.</param>
    /// <param name="appliedMigrations">The migrations already applied to the database.</param>
    void ValidateAlreadyAppliedMigrations(
        IReadOnlyList<SqlScriptMigration> scripts,
        IReadOnlyDictionary<int, AppliedMigration> appliedMigrations );

    /// <summary>
    /// Executes a single SQL migration inside a transaction and records it as applied.
    /// </summary>
    /// <param name="context">The database context used to execute the migration.</param>
    /// <param name="script">The migration script to execute.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The UTC timestamp when the migration was recorded as applied.</returns>
    Task<DateTimeOffset> ApplySingleMigrationAsync(
        MigrationsDbContext context,
        SqlScriptMigration script,
        CancellationToken token );
}