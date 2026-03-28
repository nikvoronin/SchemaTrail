using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SchemaTrail.Abstractions;
using System;
using Xunit;

namespace SchemaTrail.Tests;

public class MigrationsApplierTests
{
    private readonly Mock<IDbContextFactory<MigrationsDbContext>> _dbContextFactoryMock;
    private readonly Mock<ISqlScriptsProvider> _scriptsProviderMock;
    private readonly Mock<IMigrationRunService> _migrationRunServiceMock;
    private readonly Mock<IMigrationExecutionService> _migrationExecutionServiceMock;
    private readonly Mock<ILogger<MigrationsApplier>> _loggerMock;
    private readonly MigrationsApplier _applier;

    public MigrationsApplierTests()
    {
        _dbContextFactoryMock = new Mock<IDbContextFactory<MigrationsDbContext>>();
        _scriptsProviderMock = new Mock<ISqlScriptsProvider>();
        _migrationRunServiceMock = new Mock<IMigrationRunService>();
        _migrationExecutionServiceMock = new Mock<IMigrationExecutionService>();
        _loggerMock = new Mock<ILogger<MigrationsApplier>>();

        _applier = new MigrationsApplier(
            _dbContextFactoryMock.Object,
            _scriptsProviderMock.Object,
            _migrationRunServiceMock.Object,
            _migrationExecutionServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullDbContextFactory_ThrowsArgumentNullException()
    {
        // Arrange
        IDbContextFactory<MigrationsDbContext> dbContextFactory = null!;

        // Act
        Action act = () => new MigrationsApplier(
            dbContextFactory,
            _scriptsProviderMock.Object,
            _migrationRunServiceMock.Object,
            _migrationExecutionServiceMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContextFactory");
    }

    [Fact]
    public void Constructor_WithNullScriptsProvider_ThrowsArgumentNullException()
    {
        // Arrange
        ISqlScriptsProvider scriptsProvider = null!;

        // Act
        Action act = () => new MigrationsApplier(
            _dbContextFactoryMock.Object,
            scriptsProvider,
            _migrationRunServiceMock.Object,
            _migrationExecutionServiceMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("scriptsProvider");
    }

    [Fact]
    public void Constructor_WithNullMigrationRunService_ThrowsArgumentNullException()
    {
        // Arrange
        IMigrationRunService migrationRunService = null!;

        // Act
        Action act = () => new MigrationsApplier(
            _dbContextFactoryMock.Object,
            _scriptsProviderMock.Object,
            migrationRunService,
            _migrationExecutionServiceMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("migrationRunService");
    }

    [Fact]
    public void Constructor_WithNullMigrationExecutionService_ThrowsArgumentNullException()
    {
        // Arrange
        IMigrationExecutionService migrationExecutionService = null!;

        // Act
        Action act = () => new MigrationsApplier(
            _dbContextFactoryMock.Object,
            _scriptsProviderMock.Object,
            _migrationRunServiceMock.Object,
            migrationExecutionService,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("migrationExecutionService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<MigrationsApplier> logger = null!;

        // Act
        Action act = () => new MigrationsApplier(
            _dbContextFactoryMock.Object,
            _scriptsProviderMock.Object,
            _migrationRunServiceMock.Object,
            _migrationExecutionServiceMock.Object,
            logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // [Fact]
    // public async Task ApplyAsync_WithNoScripts_DoesNothing()
    // {
    //     // Arrange
    //     var options = new DbContextOptionsBuilder<MigrationsDbContext>()
    //         .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    //         .Options;
    //     var contextMock = new MigrationsDbContext(options);
    //     _dbContextFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
    //         .ReturnsAsync(contextMock);
    //     _scriptsProviderMock.Setup(x => x.GetMigrationScripts()).Returns(Array.Empty<SqlScriptMigration>());
    //     _migrationExecutionServiceMock.Setup(x => x.LoadAppliedMigrationsAsync(It.IsAny<MigrationsDbContext>(), It.IsAny<CancellationToken>()))
    //         .ReturnsAsync(new Dictionary<int, AppliedMigration>());

    //     // Act
    //     await _applier.ApplyAsync(CancellationToken.None);

    //     // Assert
    //     _migrationExecutionServiceMock.Verify(x => x.EnsureInfrastructureTablesExistAsync(It.IsAny<MigrationsDbContext>(), It.IsAny<CancellationToken>()), Times.Once);
    //     _migrationRunServiceMock.Verify(x => x.RecoverDanglingRunsAsync(It.IsAny<MigrationsDbContext>(), It.IsAny<Dictionary<int, AppliedMigration>>(), It.IsAny<CancellationToken>()), Times.Once);
    //     _migrationExecutionServiceMock.Verify(x => x.ValidateAlreadyAppliedMigrations(It.IsAny<IReadOnlyList<SqlScriptMigration>>(), It.IsAny<IReadOnlyDictionary<int, AppliedMigration>>()), Times.Once);
    // }
}