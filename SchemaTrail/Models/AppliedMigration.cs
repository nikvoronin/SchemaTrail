using System;

namespace SchemaTrail.Models;

public sealed record AppliedMigration(
    int Version,
    string ScriptName,
    string Description,
    string Checksum,
    DateTimeOffset AppliedAt );
