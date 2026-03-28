using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SchemaTrail.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SchemaTrail.Tests;

public class MigrationRunServiceTests
{
    private readonly Mock<ILogger<MigrationRunService>> _loggerMock;
    private readonly MigrationRunService _service;

    public MigrationRunServiceTests()
    {
        _loggerMock = new Mock<ILogger<MigrationRunService>>();
        _service = new MigrationRunService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<MigrationRunService> logger = null!;

        // Act
        Action act = () => new MigrationRunService(logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task InsertStartedAsync_WithValidParameters_InsertsAndReturnsId()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MigrationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new MigrationsDbContext(options);

        var script = new SqlScriptMigration(1, "V001__Init.sql", "Init", "CREATE TABLE test;");

        // Act
        var id = await _service.InsertStartedAsync(context, script, CancellationToken.None);

        // Assert
        id.Should().BeGreaterThan(0);

        var entity = await context.MigrationRuns.FindAsync(id);
        entity.Should().NotBeNull();
        entity!.Version.Should().Be(1);
        entity.ScriptName.Should().Be("V001__Init.sql");
        entity.Description.Should().Be("Init");
        entity.Checksum.Should().Be(script.Checksum);
        entity.Status.Should().Be(MigrationRunStatuses.Running);
        entity.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task InsertStartedAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        MigrationsDbContext context = null!;
        var script = new SqlScriptMigration(1, "V001__Init.sql", "Init", "CREATE TABLE test;");

        // Act
        Func<Task> act = () => _service.InsertStartedAsync(context, script, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public async Task InsertStartedAsync_WithNullScript_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MigrationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new MigrationsDbContext(options);
        SqlScriptMigration script = null!;

        // Act
        Func<Task> act = () => _service.InsertStartedAsync(context, script, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("script");
    }

    [Fact]
    public async Task TryCompleteAsync_WithValidParameters_UpdatesRun()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MigrationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new MigrationsDbContext(options);

        var script = new SqlScriptMigration(1, "V001__Init.sql", "Init", "CREATE TABLE test;");
        var id = await _service.InsertStartedAsync(context, script, CancellationToken.None);

        var completedAt = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromSeconds(5);

        // Act
        await _service.TryCompleteAsync(context, id, MigrationRunStatuses.Success, completedAt, duration, null, CancellationToken.None);

        // Assert
        var entity = await context.MigrationRuns.FindAsync(id);
        entity.Should().NotBeNull();
        entity!.Status.Should().Be(MigrationRunStatuses.Success);
        entity.CompletedAt.Should().Be(completedAt);
        entity.DurationMs.Should().Be(5000);
        entity.ErrorText.Should().BeNull();
    }

    [Fact]
    public async Task TryCompleteAsync_WithNonExistentId_LogsWarning()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MigrationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new MigrationsDbContext(options);

        var completedAt = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromSeconds(1);

        // Act
        await _service.TryCompleteAsync(context, 999, MigrationRunStatuses.Success, completedAt, duration, null, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Migration run record 999 not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}