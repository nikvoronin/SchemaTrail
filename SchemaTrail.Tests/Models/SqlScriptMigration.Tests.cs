using FluentAssertions;
using SchemaTrail.Models;
using System;
using Xunit;

namespace SchemaTrail.Tests.Models;

public class SqlScriptMigrationTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesMigration()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.sql";
        string description = "Init";
        string sql = "CREATE TABLE test;";

        // Act
        var migration = new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        migration.Version.Should().Be(version);
        migration.ScriptName.Should().Be(scriptName);
        migration.Description.Should().Be(description);
        migration.Sql.Should().Be(sql);
        migration.Checksum.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithNullScriptName_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = null!;
        string description = "Init";
        string sql = "CREATE TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("scriptName");
    }

    [Fact]
    public void Constructor_WithEmptyScriptName_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "";
        string description = "Init";
        string sql = "CREATE TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("scriptName");
    }

    [Fact]
    public void Constructor_WithWhitespaceScriptName_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "   ";
        string description = "Init";
        string sql = "CREATE TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("scriptName");
    }

    [Fact]
    public void Constructor_WithNullDescription_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.sql";
        string description = null!;
        string sql = "CREATE TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Constructor_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.sql";
        string description = "";
        string sql = "CREATE TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Constructor_WithWhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.sql";
        string description = "   ";
        string sql = "CREATE TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Constructor_WithNullSql_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.sql";
        string description = "Init";
        string sql = null!;

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public void Constructor_WithEmptySql_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.sql";
        string description = "Init";
        string sql = "";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public void Constructor_WithWhitespaceSql_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.sql";
        string description = "Init";
        string sql = "   ";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public void Checksum_IsCalculatedCorrectly()
    {
        // Arrange
        string sql = "SELECT 1;";
        var migration = new SqlScriptMigration(1, "V001__Init.sql", "Init", sql);

        // Act
        var checksum = migration.Checksum;

        // Assert
        checksum.Should().NotBeNullOrEmpty();
        checksum.Length.Should().Be(64); // SHA256 hex length
    }
}