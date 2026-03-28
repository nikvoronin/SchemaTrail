using FluentAssertions;
using SchemaTrail.Models;
using System;
using Xunit;

namespace SchemaTrail.Tests.Models;

public class AppliedMigrationTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesAppliedMigration()
    {
        // Arrange
        int version = 1;
        string scriptName = "V001__Init.sql";
        string description = "Init";
        string checksum = "checksum123";
        var appliedAt = DateTimeOffset.UtcNow;

        // Act
        var appliedMigration = new AppliedMigration(version, scriptName, description, checksum, appliedAt);

        // Assert
        appliedMigration.Version.Should().Be(version);
        appliedMigration.ScriptName.Should().Be(scriptName);
        appliedMigration.Description.Should().Be(description);
        appliedMigration.Checksum.Should().Be(checksum);
        appliedMigration.AppliedAt.Should().Be(appliedAt);
    }
}