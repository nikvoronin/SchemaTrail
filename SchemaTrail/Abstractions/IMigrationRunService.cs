using SchemaTrail.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SchemaTrail.Abstractions;

/// <summary>
/// Provides operations for tracking migration run records
/// and recovering unfinished migration attempts.
/// </summary>
public interface IMigrationRunService
{
    /// <summary>
    /// Creates a new migration run record with the <c>running</c> status.
    /// </summary>
    /// <param name="context">The database context used to access migration run storage.</param>
    /// <param name="script">The migration script for which the run record is created.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The identifier of the created migration run record.</returns>
    Task<long> InsertStartedAsync(
        MigrationsDbContext context,
        SqlScriptMigration script,
        CancellationToken token );

    /// <summary>
    /// Tries to complete an existing migration run record with the specified final status.
    /// </summary>
    /// <param name="context">The database context used to access migration run storage.</param>
    /// <param name="runId">The identifier of the migration run record.</param>
    /// <param name="status">The final migration run status.</param>
    /// <param name="completedAt">The UTC timestamp when the run completed.</param>
    /// <param name="duration">The duration of the migration run.</param>
    /// <param name="errorText">The optional error text to persist for failed runs.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task TryCompleteAsync(
        MigrationsDbContext context,
        long runId,
        string status,
        DateTimeOffset completedAt,
        TimeSpan duration,
        string? errorText,
        CancellationToken token );

    /// <summary>
    /// Recovers migration run records that were left in the <c>running</c> state
    /// after an unexpected process termination.
    /// </summary>
    /// <param name="context">The database context used to access migration run storage.</param>
    /// <param name="appliedMigrations">
    /// The migrations that are already applied in the database and can be used
    /// to reconcile unfinished run records.
    /// </param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RecoverDanglingRunsAsync(
        MigrationsDbContext context,
        Dictionary<int, AppliedMigration> appliedMigrations,
        CancellationToken token );
}