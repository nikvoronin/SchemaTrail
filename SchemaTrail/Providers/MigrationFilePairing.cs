namespace SchemaTrail.Providers;

using SchemaTrail.Models;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Pairs discovered up/down migration script files by version and
/// builds the resulting <see cref="SqlScriptMigration"/> instances.
/// </summary>
internal static class MigrationFilePairing
{
    /// <summary>
    /// Builds migrations from discovered up/down file matches, pairing them by version.
    /// </summary>
    /// <param name="matches">The discovered migration script files.</param>
    /// <returns>The paired migrations, ordered by version.</returns>
    public static IReadOnlyList<SqlScriptMigration> Build(
        IEnumerable<MigrationFileMatch> matches )
    {
        var entries = new Dictionary<int, MigrationFileEntry>();

        foreach (var match in matches) {
            if (!entries.TryGetValue( match.Version, out var entry )) {
                entry = new MigrationFileEntry( match.Version );
                entries[match.Version] = entry;
            }

            if (match.IsUp) {
                if (entry.UpFileName is not null) {
                    throw new InvalidOperationException(
                        $"Duplicate up-migration file for version V{match.Version:D3}: "
                        + $"'{entry.UpFileName}' and '{match.FileName}'." );
                }

                entry.UpFileName = match.FileName;
                entry.UpDescription = match.Description;
                entry.UpSql = match.Sql;
            }
            else {
                if (entry.DownFileName is not null) {
                    throw new InvalidOperationException(
                        $"Duplicate down-migration file for version V{match.Version:D3}: "
                        + $"'{entry.DownFileName}' and '{match.FileName}'." );
                }

                entry.DownFileName = match.FileName;
                entry.DownDescription = match.Description;
                entry.DownSql = match.Sql;
            }
        }

        var scripts = new List<SqlScriptMigration>( entries.Count );

        foreach (var entry in entries.Values.OrderBy( x => x.Version )) {
            if (entry.UpFileName is null) {
                throw new InvalidOperationException(
                    $"Migration V{entry.Version:D3} is missing its up-migration "
                    + $"('.up.sql') file; only a down-migration ('{entry.DownFileName}') was found." );
            }

            if (entry.DownFileName is null) {
                throw new InvalidOperationException(
                    $"Migration V{entry.Version:D3} ('{entry.UpFileName}') is missing "
                    + "its down-migration ('.down.sql') file." );
            }

            if (!string.Equals( entry.UpDescription, entry.DownDescription, StringComparison.Ordinal )) {
                throw new InvalidOperationException(
                    $"Migration V{entry.Version:D3} description mismatch: up-migration "
                    + $"'{entry.UpFileName}' has description '{entry.UpDescription}', "
                    + $"but down-migration '{entry.DownFileName}' has description '{entry.DownDescription}'." );
            }

            scripts.Add(
                new SqlScriptMigration(
                    entry.Version,
                    entry.UpFileName,
                    entry.UpDescription!,
                    entry.UpSql!,
                    entry.DownFileName,
                    entry.DownSql! ) );
        }

        return scripts;
    }

    private sealed class MigrationFileEntry( int version )
    {
        public int Version { get; } = version;
        public string? UpFileName;
        public string? UpDescription;
        public string? UpSql;
        public string? DownFileName;
        public string? DownDescription;
        public string? DownSql;
    }
}

/// <summary>
/// Represents a single discovered migration script file, either the up or down side.
/// </summary>
/// <param name="Version">The migration version.</param>
/// <param name="IsUp">Whether this file is the up-migration side.</param>
/// <param name="FileName">The script file name.</param>
/// <param name="Description">The migration description parsed from the file name.</param>
/// <param name="Sql">The SQL script content.</param>
internal readonly record struct MigrationFileMatch(
    int Version,
    bool IsUp,
    string FileName,
    string Description,
    string Sql );
