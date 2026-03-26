namespace SchemaTrail.Providers;

using SchemaTrail.Abstractions;
using SchemaTrail.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Provides access to embedded SQL migration scripts 
/// stored as assembly resources.
/// </summary>
public partial class EmbeddedSqlScriptsProvider : ISqlScriptsProvider
{
    private readonly Assembly _assembly;
    private readonly string _scriptsDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedSqlScriptsProvider"/> class
    /// using the current assembly as the resource source.
    /// </summary>
    /// <param name="resourcesPath">
    /// The part of resource path that contains embedded SQL scripts.
    /// </param>
    public EmbeddedSqlScriptsProvider(string resourcesPath)
    {
        ArgumentNullException.ThrowIfNull( resourcesPath );

        _assembly = GetType().Assembly;
        _scriptsDirectory = resourcesPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedSqlScriptsProvider"/> class
    /// using the specified assembly as the resource source.
    /// </summary>
    /// <param name="assembly">
    /// The assembly that contains the embedded SQL script resources.
    /// </param>
    /// <param name="resourcesPath">
    /// The part of resource path that contains embedded SQL scripts.
    /// </param>
    public EmbeddedSqlScriptsProvider( 
        Assembly assembly,
        string resourcesPath )
    {
        ArgumentNullException.ThrowIfNull( assembly );
        ArgumentNullException.ThrowIfNull( resourcesPath );

        _assembly = assembly;
        _scriptsDirectory = resourcesPath;
    }

    /// <inheritdoc/>
    public IReadOnlyList<SqlScriptMigration> GetMigrationScripts()
    {
        var resourceNames = _assembly
            .GetManifestResourceNames()
            .Where( resourceName =>
                resourceName.StartsWith( _scriptsDirectory )
                && resourceName.EndsWith( ".sql", StringComparison.OrdinalIgnoreCase ) )
            .OrderBy( x => x, StringComparer.Ordinal )
            .ToArray();

        var scripts = new List<SqlScriptMigration>( resourceNames.Length );

        foreach (var resourceName in resourceNames) {
            var sql = ReadToEndRequiredScript( resourceName );

            scripts.Add(
                BuildMigrationScript(resourceName, sql) );
        }

        ValidateScripts( scripts );

        return [.. scripts.OrderBy( x => x.Version )];
    }

    /// <inheritdoc/>
    public string ReadToEndRequiredScript( string resourceName )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( resourceName );

        using var stream = _assembly.GetManifestResourceStream( resourceName )
            ?? throw new InvalidOperationException(
                $"SQL resource '{resourceName}' not found." );

        using var reader = new StreamReader( stream, Encoding.UTF8 );
        return reader.ReadToEnd();
    }

    private SqlScriptMigration BuildMigrationScript(
        string resourceName,
        string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( resourceName );
        ArgumentException.ThrowIfNullOrWhiteSpace( sql );

        var prefix = _scriptsDirectory + ".";

        if (!resourceName.StartsWith( prefix, StringComparison.Ordinal )) {
            throw new InvalidOperationException(
                $"Resource '{resourceName}' does not belong "
                + $"to scripts namespace '{_scriptsDirectory}'." );
        }

        var fileName = resourceName[prefix.Length..];

        var match = _embeddedFileNameRegex.Match( fileName );
        if (!match.Success) {
            throw new InvalidOperationException(
                $"Invalid migration file name '{fileName}'." );
        }

        var version = int.Parse( match.Groups["version"].Value );
        var description =
            match.Groups["description"].Value
            .Replace( '_', ' ' );

        return new SqlScriptMigration(
            version,
            fileName,
            description,
            sql );
    }

    private static void ValidateScripts( 
        IReadOnlyCollection<SqlScriptMigration> scripts )
    {
        var duplicateVersions = scripts
            .GroupBy( x => x.Version )
            .Where( x => x.Count() > 1 )
            .ToArray();

        if (duplicateVersions.Length > 0) {
            var details = string.Join(
                Environment.NewLine,
                duplicateVersions.Select( x =>
                    $"Version {x.Key}: {string.Join( ", ", x.Select( y => y.ScriptName ) )}" ) );

            throw new InvalidOperationException(
                $"Duplicate migration versions detected:{Environment.NewLine}{details}" );
        }

        var ordered = scripts
            .OrderBy( x => x.Version )
            .ToArray();

        for (var i = 0; i < ordered.Length; i++) {
            var expectedVersion = i + 1;
            var actualVersion = ordered[i].Version;

            if (actualVersion != expectedVersion) {
                throw new InvalidOperationException(
                    $"Migration version gap detected. Expected V{expectedVersion:D3}, " +
                    $"but found V{actualVersion:D3} ({ordered[i].ScriptName})." );
            }
        }
    }

    [GeneratedRegex(
        @"^V(?<version>\d+)__(?<description>[A-Za-z0-9_\-]+)\.sql$",
        RegexOptions.Compiled
        | RegexOptions.CultureInvariant )]
    private static partial Regex EmbeddedFileNameRegex();

    private static readonly Regex _embeddedFileNameRegex = EmbeddedFileNameRegex();
}