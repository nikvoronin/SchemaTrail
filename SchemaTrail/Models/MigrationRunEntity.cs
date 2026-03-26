using System;

namespace SchemaTrail.Models;

public sealed class MigrationRunEntity
{
    public long Id { get; set; }

    public required int Version { get; set; }

    public required string ScriptName { get; set; }

    public required string Description { get; set; }

    public required string Checksum { get; set; }

    public required string Status { get; set; }

    public required DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public long? DurationMs { get; set; }

    public string? ErrorText { get; set; }
}
