using System;

namespace SchemaTrail.Models;

public sealed record AppliedMigration(
    int Version,
    string ScriptName,
    string Description,
    DateTimeOffset AppliedAt );
