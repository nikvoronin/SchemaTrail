using FluentAssertions;
using SchemaTrail.Models;
using Xunit;

namespace SchemaTrail.Tests.Models;

public class MigrationRunStatusesTests
{
    [Fact]
    public void Running_IsCorrectValue()
    {
        // Act & Assert
        MigrationRunStatuses.Running.Should().Be("running");
    }

    [Fact]
    public void Success_IsCorrectValue()
    {
        // Act & Assert
        MigrationRunStatuses.Success.Should().Be("success");
    }

    [Fact]
    public void Failed_IsCorrectValue()
    {
        // Act & Assert
        MigrationRunStatuses.Failed.Should().Be("failed");
    }
}