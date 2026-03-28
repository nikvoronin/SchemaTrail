using FluentAssertions;
using SchemaTrail.Models;
using System;
using Xunit;

namespace SchemaTrail.Tests.Models;

public class MigrationRunRecordTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesMigrationRunRecord()
    {
        // Arrange
        long id = 1;
        int version = 1;
        string scriptName = "V001__Init.sql";
        string description = "Init";
        string checksum = "checksum123";
        var startedAt = DateTimeOffset.UtcNow;

        // Act
        var runRecord = new MigrationRunRecord(id, version, scriptName, description, checksum, startedAt);

        // Assert
        runRecord.Id.Should().Be(id);
        runRecord.Version.Should().Be(version);
        runRecord.ScriptName.Should().Be(scriptName);
        runRecord.Description.Should().Be(description);
        runRecord.Checksum.Should().Be(checksum);
        runRecord.StartedAt.Should().Be(startedAt);
    }
}