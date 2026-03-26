using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchemaTrail.Abstractions;
using SchemaTrail.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SchemaTrail;

/// <summary>
/// Manages migration run records and performs recovery of unfinished migration runs.
/// </summary>
public sealed partial class MigrationRunService : IMigrationRunService
{
    /// <inheritdoc />
    public async Task RecoverDanglingRunsAsync(
        MigrationsDbContext context,
        Dictionary<int, AppliedMigration> appliedMigrations,
        CancellationToken token )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( appliedMigrations );

        var danglingRuns = await context.MigrationRuns
            .AsNoTracking()
            .Where( x =>
                x.Status == MigrationRunStatuses.Running 
                && x.CompletedAt == null )
            .OrderBy( x => x.Version )
            .ThenBy( x => x.StartedAt )
            .ThenBy( x => x.Id )
            .Select( x =>
                new MigrationRunRecord(
                    x.Id,
                    x.Version,
                    x.ScriptName,
                    x.Description,
                    x.Checksum,
                    x.StartedAt ) )
            .ToListAsync( token );

        if (danglingRuns.Count == 0) return;

        var recoveryPlan = danglingRuns
            .GroupBy( x => x.Version )
            .OrderBy( x => x.Key )
            .SelectMany( group => BuildRecoveryActions(
                version: group.Key,
                runs: OrderRuns( group ),
                appliedMigrations: appliedMigrations ) )
            .ToArray();

        foreach (var action in recoveryPlan) {
            await CompleteRecoveredAsync(
                context,
                action.RunId,
                action.Status,
                action.CompletedAt,
                action.Duration,
                action.Message,
                token );
        }

        _logger.LogWarning(
            "Recovered {RecoveredCount} dangling migration run records",
            recoveryPlan.Length );
    }

    private static IReadOnlyList<MigrationRunRecord> OrderRuns(
        IEnumerable<MigrationRunRecord> runs )
    {
        return [.. runs
            .OrderByDescending( x => x.StartedAt )
            .ThenByDescending( x => x.Id )];
    }

    private static IEnumerable<RecoveryAction> BuildRecoveryActions(
        int version,
        IReadOnlyList<MigrationRunRecord> runs,
        IReadOnlyDictionary<int, AppliedMigration> appliedMigrations )
    {
        var now = DateTimeOffset.UtcNow;

        if (TryFindSuccessCandidate( 
                version, 
                runs, 
                appliedMigrations, 
                out var applied, 
                out var successCandidate ))
        {
            foreach (var action 
                in BuildMatchedActions( runs, successCandidate, applied, now ))
                yield return action;

            yield break;
        }

        foreach (var action
            in BuildFailedActions(
                runs,
                now,
                "Recovered on startup after unexpected process termination "
                + "before migration completion." ))
            yield return action;
    }

    private static bool TryFindSuccessCandidate(
        int version,
        IEnumerable<MigrationRunRecord> runs,
        IReadOnlyDictionary<int, AppliedMigration> appliedMigrations,
        [NotNullWhen( true )] out AppliedMigration? applied,
        [NotNullWhen( true )] out MigrationRunRecord? successCandidate )
    {
        successCandidate = null;

        if (!appliedMigrations.TryGetValue( version, out applied ))
            return false;

        var name = applied.ScriptName;
        var description = applied.Description;
        var checksum = applied.Checksum;

        successCandidate = runs.FirstOrDefault( x =>
            string.Equals( x.ScriptName, name, StringComparison.Ordinal )
            && string.Equals( x.Description, description, StringComparison.Ordinal ) 
            && string.Equals( x.Checksum, checksum, StringComparison.Ordinal ) );

        return successCandidate is not null;
    }

    private static IEnumerable<RecoveryAction> BuildMatchedActions(
        IReadOnlyList<MigrationRunRecord> runs,
        MigrationRunRecord successCandidate,
        AppliedMigration applied,
        DateTimeOffset now )
    {
        var successCompletedAt =
            applied.AppliedAt >= successCandidate.StartedAt
                ? applied.AppliedAt
                : now;

        yield return new RecoveryAction(
            successCandidate.Id,
            MigrationRunStatuses.Success,
            successCompletedAt,
            SafeDuration( successCompletedAt, successCandidate.StartedAt ),
            "Recovered on startup after unexpected process termination." );

        foreach (var action
            in BuildFailedActions(
                runs.Where( x => x.Id != successCandidate.Id ),
                now,
                "Marked as failed during recovery because another run "
                + "for the same migration was reconciled as successful." ))
            yield return action;
    }

    private static IEnumerable<RecoveryAction> BuildFailedActions(
        IEnumerable<MigrationRunRecord> runs,
        DateTimeOffset completedAt,
        string message )
    {
        foreach (var run in runs)
            yield return new RecoveryAction(
                run.Id,
                MigrationRunStatuses.Failed,
                completedAt,
                SafeDuration( completedAt, run.StartedAt ),
                message );
    }

    private static TimeSpan SafeDuration(
        DateTimeOffset completedAt,
        DateTimeOffset startedAt )
    {
        var duration = completedAt - startedAt;

        return duration < TimeSpan.Zero
            ? TimeSpan.Zero
            : duration;
    }

    private sealed record RecoveryAction(
        long RunId,
        string Status,
        DateTimeOffset CompletedAt,
        TimeSpan Duration,
        string Message );
}
