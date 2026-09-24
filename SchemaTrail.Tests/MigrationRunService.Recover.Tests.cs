using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SchemaTrail.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SchemaTrail.Tests;

public class MigrationRunServiceRecoverTests
{
    private readonly MigrationRunService _service;
    private readonly MigrationsDbContext _context;

    public MigrationRunServiceRecoverTests()
    {
        var loggerMock = new Mock<ILogger<MigrationRunService>>();
        _service = new MigrationRunService(loggerMock.Object);

        var options = new DbContextOptionsBuilder<MigrationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MigrationsDbContext(options);
    }

    private async Task<long> AddDanglingRunAsync(
        int version,
        string direction,
        DateTimeOffset startedAt,
        string scriptName = "V001__Init.up.sql",
        string description = "Init")
    {
        var entity = new MigrationRunEntity
        {
            Version = version,
            ScriptName = scriptName,
            Description = description,
            Status = MigrationRunStatuses.Running,
            Direction = direction,
            StartedAt = startedAt,
        };

        _context.MigrationRuns.Add(entity);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return entity.Id;
    }

    [Fact]
    public async Task RecoverDanglingRunsAsync_UpRunMatchingAppliedMigration_MarksSuccess()
    {
        // Arrange
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var runId = await AddDanglingRunAsync(1, MigrationRunDirections.Up, startedAt);

        var appliedMigrations = new Dictionary<int, AppliedMigration>
        {
            { 1, new AppliedMigration(1, "V001__Init.up.sql", "Init", startedAt.AddSeconds(1)) }
        };

        // Act
        await _service.RecoverDanglingRunsAsync(_context, appliedMigrations, CancellationToken.None);

        // Assert
        var entity = await _context.MigrationRuns.FindAsync(runId);
        entity!.Status.Should().Be(MigrationRunStatuses.Success);
    }

    [Fact]
    public async Task RecoverDanglingRunsAsync_UpRunNotApplied_MarksFailed()
    {
        // Arrange
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var runId = await AddDanglingRunAsync(1, MigrationRunDirections.Up, startedAt);

        var appliedMigrations = new Dictionary<int, AppliedMigration>();

        // Act
        await _service.RecoverDanglingRunsAsync(_context, appliedMigrations, CancellationToken.None);

        // Assert
        var entity = await _context.MigrationRuns.FindAsync(runId);
        entity!.Status.Should().Be(MigrationRunStatuses.Failed);
    }

    [Fact]
    public async Task RecoverDanglingRunsAsync_DownRunNoLongerApplied_MarksRolledBack()
    {
        // Arrange - the revert transaction committed (row removed) before the crash
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var runId = await AddDanglingRunAsync(1, MigrationRunDirections.Down, startedAt);

        var appliedMigrations = new Dictionary<int, AppliedMigration>();

        // Act
        await _service.RecoverDanglingRunsAsync(_context, appliedMigrations, CancellationToken.None);

        // Assert
        var entity = await _context.MigrationRuns.FindAsync(runId);
        entity!.Status.Should().Be(MigrationRunStatuses.RolledBack);
    }

    [Fact]
    public async Task RecoverDanglingRunsAsync_DownRunStillApplied_MarksFailed()
    {
        // Arrange - the revert transaction never committed before the crash
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var runId = await AddDanglingRunAsync(1, MigrationRunDirections.Down, startedAt);

        var appliedMigrations = new Dictionary<int, AppliedMigration>
        {
            { 1, new AppliedMigration(1, "V001__Init.up.sql", "Init", startedAt.AddMinutes(-10)) }
        };

        // Act
        await _service.RecoverDanglingRunsAsync(_context, appliedMigrations, CancellationToken.None);

        // Assert
        var entity = await _context.MigrationRuns.FindAsync(runId);
        entity!.Status.Should().Be(MigrationRunStatuses.Failed);
    }

    [Fact]
    public async Task RecoverDanglingRunsAsync_DuplicateDownRuns_OnlyMostRecentMarkedRolledBack()
    {
        // Arrange
        var olderStartedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var newerStartedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var olderRunId = await AddDanglingRunAsync(1, MigrationRunDirections.Down, olderStartedAt);
        var newerRunId = await AddDanglingRunAsync(1, MigrationRunDirections.Down, newerStartedAt);

        var appliedMigrations = new Dictionary<int, AppliedMigration>();

        // Act
        await _service.RecoverDanglingRunsAsync(_context, appliedMigrations, CancellationToken.None);

        // Assert
        var newerEntity = await _context.MigrationRuns.FindAsync(newerRunId);
        newerEntity!.Status.Should().Be(MigrationRunStatuses.RolledBack);

        var olderEntity = await _context.MigrationRuns.FindAsync(olderRunId);
        olderEntity!.Status.Should().Be(MigrationRunStatuses.Failed);
    }

    [Fact]
    public async Task RecoverDanglingRunsAsync_MixedUpAndDownDanglingRunsForSameVersion_ResolvedIndependently()
    {
        // Arrange - an up run that succeeded, and (pathologically) an unrelated dangling down
        // run for the same version that never took effect.
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var upRunId = await AddDanglingRunAsync(1, MigrationRunDirections.Up, startedAt);
        var downRunId = await AddDanglingRunAsync(1, MigrationRunDirections.Down, startedAt);

        var appliedMigrations = new Dictionary<int, AppliedMigration>
        {
            { 1, new AppliedMigration(1, "V001__Init.up.sql", "Init", startedAt.AddSeconds(1)) }
        };

        // Act
        await _service.RecoverDanglingRunsAsync(_context, appliedMigrations, CancellationToken.None);

        // Assert
        var upEntity = await _context.MigrationRuns.FindAsync(upRunId);
        upEntity!.Status.Should().Be(MigrationRunStatuses.Success);

        var downEntity = await _context.MigrationRuns.FindAsync(downRunId);
        downEntity!.Status.Should().Be(MigrationRunStatuses.Failed);
    }
}
