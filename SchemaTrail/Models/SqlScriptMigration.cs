namespace SchemaTrail.Models;

using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Represents a SQL script-based migration.
/// </summary>
public sealed partial class SqlScriptMigration
{
    /// <summary>
    /// Gets the migration version.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the script file name.
    /// </summary>
    public string ScriptName { get; }

    /// <summary>
    /// Gets the migration description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the checksum calculated from the SQL script content.
    /// </summary>
    public string Checksum { get; }

    /// <summary>
    /// Gets the SQL script content.
    /// </summary>
    public string Sql { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlScriptMigration"/> class.
    /// </summary>
    /// <param name="version">
    /// The migration version.
    /// </param>
    /// <param name="scriptName">
    /// The script file name.
    /// </param>
    /// <param name="description">
    /// The migration description.
    /// </param>
    /// <param name="sql">
    /// The SQL script content.
    /// </param>
    public SqlScriptMigration(
        int version,
        string scriptName,
        string description,
        string sql )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( scriptName );
        ArgumentException.ThrowIfNullOrWhiteSpace( description );
        ArgumentException.ThrowIfNullOrWhiteSpace( sql );

        Version = version;
        ScriptName = scriptName;
        Description = description;
        Checksum = CalculateChecksum( sql );
        Sql = sql;
    }

    private static string CalculateChecksum( string sql )
    {
        var bytes = Encoding.UTF8.GetBytes( sql );
        var hash = SHA256.HashData( bytes );

        return Convert.ToHexString( hash );
    }
}