using FluentAssertions;
using SchemaTrail.Providers;
using System;
using System.IO;
using Xunit;

namespace SchemaTrail.Tests.Providers;

public class FileSystemSqlScriptsProviderTests : IDisposable
{
    private readonly string _tempDirectory;

    public FileSystemSqlScriptsProviderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WithNullDirectoryPath_ThrowsArgumentNullException()
    {
        // Arrange
        string directoryPath = null!;

        // Act
        Action act = () => new FileSystemSqlScriptsProvider(directoryPath);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("directoryPath");
    }

    [Fact]
    public void GetMigrationScripts_WithNonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent");
        var provider = new FileSystemSqlScriptsProvider(nonExistentPath);

        // Act
        var scripts = provider.GetMigrationScripts();

        // Assert
        scripts.Should().BeEmpty();
    }

    [Fact]
    public void GetMigrationScripts_WithValidFiles_ReturnsOrderedScripts()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDirectory, "V001__Init.up.sql"), "CREATE TABLE test;");
        File.WriteAllText(Path.Combine(_tempDirectory, "V001__Init.down.sql"), "DROP TABLE test;");
        File.WriteAllText(Path.Combine(_tempDirectory, "V002__Create_users_table.up.sql"), "CREATE TABLE users;");
        File.WriteAllText(Path.Combine(_tempDirectory, "V002__Create_users_table.down.sql"), "DROP TABLE users;");

        var provider = new FileSystemSqlScriptsProvider(_tempDirectory);

        // Act
        var scripts = provider.GetMigrationScripts();

        // Assert
        scripts.Should().HaveCount(2);
        scripts[0].Version.Should().Be(1);
        scripts[0].ScriptName.Should().Be("V001__Init.up.sql");
        scripts[0].Description.Should().Be("Init");
        scripts[0].Sql.Should().Be("CREATE TABLE test;");
        scripts[0].DownScriptName.Should().Be("V001__Init.down.sql");
        scripts[0].DownSql.Should().Be("DROP TABLE test;");
        scripts[1].Version.Should().Be(2);
        scripts[1].ScriptName.Should().Be("V002__Create_users_table.up.sql");
        scripts[1].Description.Should().Be("Create users table");
        scripts[1].DownScriptName.Should().Be("V002__Create_users_table.down.sql");
        scripts[1].DownSql.Should().Be("DROP TABLE users;");
    }

    [Fact]
    public void GetMigrationScripts_WithInvalidFileName_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidFile = Path.Combine(_tempDirectory, "V.sql");
        File.WriteAllText(invalidFile, "SELECT 1;");

        var provider = new FileSystemSqlScriptsProvider(_tempDirectory);

        // Act
        Action act = () => provider.GetMigrationScripts();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid migration file name 'V.sql'.");
    }

    [Fact]
    public void GetMigrationScripts_WithMissingDownFile_ThrowsInvalidOperationException()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDirectory, "V001__Init.up.sql"), "CREATE TABLE test;");

        var provider = new FileSystemSqlScriptsProvider(_tempDirectory);

        // Act
        Action act = () => provider.GetMigrationScripts();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*V001*down-migration*");
    }

    [Fact]
    public void GetMigrationScripts_WithMissingUpFile_ThrowsInvalidOperationException()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDirectory, "V001__Init.down.sql"), "DROP TABLE test;");

        var provider = new FileSystemSqlScriptsProvider(_tempDirectory);

        // Act
        Action act = () => provider.GetMigrationScripts();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*V001*up-migration*");
    }

    [Fact]
    public void GetMigrationScripts_WithMismatchedDescriptions_ThrowsInvalidOperationException()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDirectory, "V001__Init.up.sql"), "CREATE TABLE test;");
        File.WriteAllText(Path.Combine(_tempDirectory, "V001__Different.down.sql"), "DROP TABLE test;");

        var provider = new FileSystemSqlScriptsProvider(_tempDirectory);

        // Act
        Action act = () => provider.GetMigrationScripts();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*V001*description mismatch*");
    }

    [Fact]
    public void ReadToEndRequiredScript_WithValidFile_ReturnsContent()
    {
        // Arrange
        var file = Path.Combine(_tempDirectory, "test.sql");
        var content = "SELECT * FROM test;";
        File.WriteAllText(file, content);

        var provider = new FileSystemSqlScriptsProvider(_tempDirectory);

        // Act
        var result = provider.ReadToEndRequiredScript(file);

        // Assert
        result.Should().Be(content);
    }
}