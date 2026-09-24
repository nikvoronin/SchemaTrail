using System;

namespace SchemaTrail.Models;

public sealed class MigrationEntity
{
    public int Version { get; set; }

    public required string ScriptName { get; set; }

    public required string Description { get; set; }

    public DateTimeOffset AppliedAt { get; set; }
}
