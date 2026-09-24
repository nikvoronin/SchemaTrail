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
        string scriptName = "V001__Init.up.sql";
        string description = "Init";
        string sql = "CREATE TABLE test;";
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        var migration = new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        migration.Version.Should().Be(version);
        migration.ScriptName.Should().Be(scriptName);
        migration.Description.Should().Be(description);
        migration.Sql.Should().Be(sql);
        migration.DownScriptName.Should().Be(downScriptName);
        migration.DownSql.Should().Be(downSql);
    }

    [Fact]
    public void Constructor_WithNullScriptName_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = null!;
        string description = "Init";
        string sql = "CREATE TABLE test;";
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

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
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

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
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("scriptName");
    }

    [Fact]
    public void Constructor_WithNullDescription_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = null!;
        string sql = "CREATE TABLE test;";
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Constructor_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = "";
        string sql = "CREATE TABLE test;";
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Constructor_WithWhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = "   ";
        string sql = "CREATE TABLE test;";
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Constructor_WithNullSql_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = "Init";
        string sql = null!;
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public void Constructor_WithEmptySql_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = "Init";
        string sql = "";
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public void Constructor_WithWhitespaceSql_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = "Init";
        string sql = "   ";
        string downScriptName = "V001__Init.down.sql";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public void Constructor_WithNullDownScriptName_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = "Init";
        string sql = "CREATE TABLE test;";
        string downScriptName = null!;
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("downScriptName");
    }

    [Fact]
    public void Constructor_WithWhitespaceDownScriptName_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = "Init";
        string sql = "CREATE TABLE test;";
        string downScriptName = "   ";
        string downSql = "DROP TABLE test;";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("downScriptName");
    }

    [Fact]
    public void Constructor_WithNullDownSql_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = "Init";
        string sql = "CREATE TABLE test;";
        string downScriptName = "V001__Init.down.sql";
        string downSql = null!;

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("downSql");
    }

    [Fact]
    public void Constructor_WithWhitespaceDownSql_ThrowsArgumentException()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.up.sql";
        string description = "Init";
        string sql = "CREATE TABLE test;";
        string downScriptName = "V001__Init.down.sql";
        string downSql = "   ";

        // Act
        Action act = () => new SqlScriptMigration(version, scriptName, description, sql, downScriptName, downSql);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("downSql");
    }
}
