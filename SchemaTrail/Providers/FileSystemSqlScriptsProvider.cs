namespace SchemaTrail.Providers;

using SchemaTrail.Abstractions;
using SchemaTrail.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Provides access to SQL migration scripts stored in a file system directory.
/// </summary>
public partial class FileSystemSqlScriptsProvider : ISqlScriptsProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemSqlScriptsProvider"/> class.
    /// </summary>
    /// <param name="directoryPath">
    /// The path to the directory that contains SQL migration scripts.
    /// </param>
    public FileSystemSqlScriptsProvider( string directoryPath )
    {
        ArgumentNullException.ThrowIfNull( directoryPath );
        _directoryPath = directoryPath;
    }

    /// <inheritdoc/>
    public IReadOnlyList<SqlScriptMigration> GetMigrationScripts()
    {
        if (!Directory.Exists( _directoryPath )) {
            return [];
        }

        var files = Directory.EnumerateFiles(
            _directoryPath,
            "V*.sql",
            SearchOption.TopDirectoryOnly );

        var scripts = new List<SqlScriptMigration>();

        foreach (var fileName in files) {
            var fileNameOnly = Path.GetFileName(fileName);
            var match = _fileNameRegex.Match( fileNameOnly );

            if (!match.Success) {
                throw new InvalidOperationException(
                    $"Invalid migration file name '{fileNameOnly}'." );
            }

            var version = int.Parse( match.Groups["version"].Value );
            var description =
                match.Groups["description"].Value
                .Replace( '_', ' ' );
            var sql = ReadToEndRequiredScript( fileName );

            scripts.Add(
                new SqlScriptMigration(
                    version, 
                    fileNameOnly, 
                    description,
                    sql) );
        }

        return [.. scripts.OrderBy( x => x.Version )];
    }

    /// <inheritdoc/>
    public string ReadToEndRequiredScript( string filePath ) =>
        File.ReadAllText( filePath, Encoding.UTF8 );

    private readonly string _directoryPath;

    [GeneratedRegex( 
        @"^V(?<version>\d+)__(?<description>.+)\.sql$", 
        RegexOptions.Compiled )]
    private static partial Regex CreateFileNameRegex();
    private static readonly Regex _fileNameRegex = CreateFileNameRegex();
}