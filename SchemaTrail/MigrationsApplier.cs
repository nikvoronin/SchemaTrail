using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchemaTrail.Abstractions;
using SchemaTrail.Models;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SchemaTrail;

/// <summary>
/// Coordinates the SQL migration application process for the storage.
/// </summary>
public sealed class MigrationsApplier : IMigrationsApplier
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationsApplier"/> class.
    /// </summary>
    /// <param name="dbContextFactory">The factory used to create migration database contexts.</param>
    /// <param name="scriptsProvider">The provider that returns migration SQL scripts.</param>
    /// <param name="migrationRunService">The service that manages migration run records.</param>
    /// <param name="migrationExecutionService">The service that validates and executes migrations.</param>
    /// <param name="logger">The logger instance.</param>
    public MigrationsApplier(
        IDbContextFactory<MigrationsDbContext> dbContextFactory,
        ISqlScriptsProvider scriptsProvider,
        IMigrationRunService migrationRunService,
        IMigrationExecutionService migrationExecutionService,
        ILogger<MigrationsApplier> logger )
    {
        ArgumentNullException.ThrowIfNull( dbContextFactory );
        ArgumentNullException.ThrowIfNull( scriptsProvider );
        ArgumentNullException.ThrowIfNull( migrationRunService );
        ArgumentNullException.ThrowIfNull( migrationExecutionService );
        ArgumentNullException.ThrowIfNull( logger );

        _dbContextFactory = dbContextFactory;
        _scriptsProvider = scriptsProvider;
        _migrationRunService = migrationRunService;
        _migrationExecutionService = migrationExecutionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ApplyAsync( CancellationToken token )
    {
        token.ThrowIfCancellationRequested();

        using var logScope = _logger.BeginScope( "Processing migrations" );

        var scripts = _scriptsProvider.GetMigrationScripts();

        await using var migrationContext =
            await _dbContextFactory.CreateDbContextAsync( token );
        await using var auditContext =
            await _dbContextFactory.CreateDbContextAsync( token );

        await migrationContext.Database.OpenConnectionAsync( token );

        try {
            await AcquireMigrationLockAsync( migrationContext, token );

            await _migrationExecutionService.EnsureInfrastructureTablesExistAsync(
                migrationContext,
                token );

            var appliedMigrations =
                await _migrationExecutionService.LoadAppliedMigrationsAsync(
                    migrationContext,
                    token );

            await _migrationRunService.RecoverDanglingRunsAsync(
                auditContext,
                appliedMigrations,
                token );

            _migrationExecutionService.ValidateAlreadyAppliedMigrations(
                scripts,
                appliedMigrations );

            foreach (var script in scripts) {
                if (appliedMigrations.ContainsKey( script.Version )) {
                    continue;
                }

                _logger.LogInformation(
                    "Applying migration V{Version:D3}: {ScriptName}",
                    script.Version,
                    script.ScriptName );

                var runId = await _migrationRunService.InsertStartedAsync(
                    auditContext,
                    script,
                    token );

                var stopwatch = Stopwatch.StartNew();

                try {
                    var appliedAt = await _migrationExecutionService.ApplySingleMigrationAsync(
                        migrationContext,
                        script,
                        token );

                    stopwatch.Stop();

                    await _migrationRunService.TryCompleteAsync(
                        auditContext,
                        runId,
                        MigrationRunStatuses.Success,
                        appliedAt,
                        stopwatch.Elapsed,
                        errorText: null,
                        token );

                    _logger.LogInformation(
                        "Migration V{Version:D3}: {ScriptName} applied successfully in {DurationMs} ms",
                        script.Version,
                        script.ScriptName,
                        stopwatch.ElapsedMilliseconds );

                    appliedMigrations[script.Version] =
                        new AppliedMigration(
                            script.Version,
                            script.ScriptName,
                            script.Description,
                            script.Checksum,
                            appliedAt );
                }
                catch (Exception ex) {
                    stopwatch.Stop();

                    await _migrationRunService.TryCompleteAsync(
                        auditContext,
                        runId,
                        MigrationRunStatuses.Failed,
                        DateTimeOffset.UtcNow,
                        stopwatch.Elapsed,
                        ex.ToString(),
                        token );

                    _logger.LogError(
                        ex,
                        "Migration V{Version:D3}: {ScriptName} failed after {DurationMs} ms",
                        script.Version,
                        script.ScriptName,
                        stopwatch.ElapsedMilliseconds );

                    throw;
                }
            }

            _logger.LogInformation( "Migration process completed successfully" );
        }
        finally {
            await ReleaseMigrationLockSafeAsync( migrationContext );
            await migrationContext.Database.CloseConnectionAsync();
        }
    }

    private static async Task AcquireMigrationLockAsync(
        MigrationsDbContext context,
        CancellationToken token )
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"select pg_advisory_lock({ADVISORY_KEY_A}, {ADVISORY_KEY_B});",
            token );
    }

    private static async Task ReleaseMigrationLockSafeAsync(
        MigrationsDbContext context )
    {
        try {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"select pg_advisory_unlock({ADVISORY_KEY_A}, {ADVISORY_KEY_B});",
                CancellationToken.None );
        }
        catch {
            // Ignore unlock errors during shutdown/dispose.
        }
    }

    private readonly IDbContextFactory<MigrationsDbContext> _dbContextFactory;
    private readonly ISqlScriptsProvider _scriptsProvider;
    private readonly IMigrationRunService _migrationRunService;
    private readonly IMigrationExecutionService _migrationExecutionService;
    private readonly ILogger<MigrationsApplier> _logger;

    private const int ADVISORY_KEY_A = 184312;
    private const int ADVISORY_KEY_B = 908741;

}