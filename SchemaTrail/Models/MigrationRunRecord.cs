using System;

namespace SchemaTrail.Models;

public sealed record MigrationRunRecord(
    long Id,
    int Version,
    string ScriptName,
    string Description,
    string Direction,
    DateTimeOffset StartedAt );