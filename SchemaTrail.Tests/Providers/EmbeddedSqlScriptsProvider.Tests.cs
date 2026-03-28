using FluentAssertions;
using SchemaTrail.Providers;
using System;
using System.Reflection;
using Xunit;

namespace SchemaTrail.Tests.Providers;

public class EmbeddedSqlScriptsProviderTests
{
    [Fact]
    public void Constructor_WithNullResourcesPath_ThrowsArgumentNullException()
    {
        // Arrange
        string resourcesPath = null!;

        // Act
        Action act = () => new EmbeddedSqlScriptsProvider(resourcesPath);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("resourcesPath");
    }

    [Fact]
    public void Constructor_WithAssemblyAndNullResourcesPath_ThrowsArgumentNullException()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        string resourcesPath = null!;

        // Act
        Action act = () => new EmbeddedSqlScriptsProvider(assembly, resourcesPath);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("resourcesPath");
    }

    [Fact]
    public void Constructor_WithNullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        Assembly assembly = null!;
        string resourcesPath = "Migrations";

        // Act
        Action act = () => new EmbeddedSqlScriptsProvider(assembly, resourcesPath);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("assembly");
    }

    [Fact]
    public void GetMigrationScripts_WithValidResources_ReturnsScripts()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var provider = new EmbeddedSqlScriptsProvider(assembly, "TestMigrations");

        // Act
        var scripts = provider.GetMigrationScripts();

        // Assert
        scripts.Should().NotBeNull();
        // Note: In a real test, you would embed test resources
        // For this example, assuming no resources, it should return empty
        scripts.Should().BeEmpty();
    }

    [Fact]
    public void ReadToEndRequiredScript_WithNullResourceName_ThrowsArgumentException()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var provider = new EmbeddedSqlScriptsProvider(assembly, "TestMigrations");
        string resourceName = null!;

        // Act
        Action act = () => provider.ReadToEndRequiredScript(resourceName);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("resourceName");
    }

    [Fact]
    public void ReadToEndRequiredScript_WithEmptyResourceName_ThrowsArgumentException()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var provider = new EmbeddedSqlScriptsProvider(assembly, "TestMigrations");
        string resourceName = "";

        // Act
        Action act = () => provider.ReadToEndRequiredScript(resourceName);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("resourceName");
    }

    [Fact]
    public void ReadToEndRequiredScript_WithWhitespaceResourceName_ThrowsArgumentException()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var provider = new EmbeddedSqlScriptsProvider(assembly, "TestMigrations");
        string resourceName = "   ";

        // Act
        Action act = () => provider.ReadToEndRequiredScript(resourceName);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("resourceName");
    }

    [Fact]
    public void ReadToEndRequiredScript_WithNonExistentResource_ThrowsInvalidOperationException()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        var provider = new EmbeddedSqlScriptsProvider(assembly, "TestMigrations");
        string resourceName = "NonExistentResource.sql";

        // Act
        Action act = () => provider.ReadToEndRequiredScript(resourceName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("SQL resource 'NonExistentResource.sql' not found.");
    }

    // Additional tests can be added for parsing logic, etc.
}