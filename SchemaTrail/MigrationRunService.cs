using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchemaTrail.Abstractions;
using SchemaTrail.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SchemaTrail;

/// <summary>
/// Manages migration run records and performs recovery of unfinished migration runs.
/// </summary>
public sealed partial class MigrationRunService : IMigrationRunService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationRunService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public MigrationRunService( ILogger<MigrationRunService> logger )
    {
        ArgumentNullException.ThrowIfNull( logger );
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<long> InsertStartedAsync(
        MigrationsDbContext context,
        SqlScriptMigration script,
        CancellationToken token )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( script );

        var entity = new MigrationRunEntity {
            Version = script.Version,
            ScriptName = script.ScriptName,
            Description = script.Description,
            Checksum = script.Checksum,
            Status = MigrationRunStatuses.Running,
            StartedAt = DateTimeOffset.UtcNow,
        };

        context.MigrationRuns.Add( entity );
        await context.SaveChangesAsync( token );
        context.ChangeTracker.Clear();

        return entity.Id;
    }

    /// <inheritdoc />
    public async Task TryCompleteAsync(
        MigrationsDbContext context,
        long runId,
        string status,
        DateTimeOffset completedAt,
        TimeSpan duration,
        string? errorText,
        CancellationToken token )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( status );

        try {
            var run = await context.MigrationRuns
                .SingleOrDefaultAsync( x => x.Id == runId, token );

            if (run is null) {
                _logger.LogWarning(
                    "Migration run record {RunId} not found while setting status {Status}",
                    runId,
                    status );
                return;
            }

            run.Status = status;
            run.CompletedAt = completedAt;
            run.DurationMs = ToDurationMilliseconds( duration );
            run.ErrorText = errorText;

            await context.SaveChangesAsync( token );
            context.ChangeTracker.Clear();
        }
        catch (Exception ex) {
            _logger.LogError(
                ex,
                "Failed to update migration run record {RunId} with status {Status}",
                runId,
                status );
        }
    }

    /// <summary>
    /// Completes a migration run record that is still marked as running during recovery.
    /// </summary>
    /// <param name="context">The database context used to access migration run storage.</param>
    /// <param name="runId">The identifier of the migration run record.</param>
    /// <param name="status">The final migration run status.</param>
    /// <param name="completedAt">The UTC timestamp when the run completed.</param>
    /// <param name="duration">The duration of the migration run.</param>
    /// <param name="errorText">The recovery note to append to the error text field.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task CompleteRecoveredAsync(
        MigrationsDbContext context,
        long runId,
        string status,
        DateTimeOffset completedAt,
        TimeSpan duration,
        string errorText,
        CancellationToken token )
    {
        var run = await context.MigrationRuns
            .SingleOrDefaultAsync(
                x => x.Id == runId
                    && x.Status == MigrationRunStatuses.Running
                    && x.CompletedAt == null,
                token );

        if (run is null) {
            return;
        }

        run.Status = status;
        run.CompletedAt = completedAt;
        run.DurationMs = ToDurationMilliseconds( duration );
        run.ErrorText = string.IsNullOrWhiteSpace( run.ErrorText )
            ? errorText
            : run.ErrorText + Environment.NewLine + errorText;

        await context.SaveChangesAsync( token );
        context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Converts a duration to a non-negative <see cref="long"/> value in milliseconds.
    /// </summary>
    /// <param name="duration">The duration to convert.</param>
    /// <returns>The duration in milliseconds, clamped to the <see cref="long"/> range.</returns>
    private static long ToDurationMilliseconds( TimeSpan duration )
    {
        return duration.TotalMilliseconds switch {
            < 0 => 0L,
            > long.MaxValue => long.MaxValue,
            _ => Convert.ToInt64( duration.TotalMilliseconds )
        };
    }

    private readonly ILogger<MigrationRunService> _logger;
}