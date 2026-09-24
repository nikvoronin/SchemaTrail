namespace SchemaTrail.Models;

using System;

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
    /// Gets the SQL script content that applies the migration.
    /// </summary>
    public string Sql { get; }

    /// <summary>
    /// Gets the down-migration script file name.
    /// </summary>
    public string DownScriptName { get; }

    /// <summary>
    /// Gets the SQL script content that reverts the migration.
    /// </summary>
    public string DownSql { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlScriptMigration"/> class.
    /// </summary>
    /// <param name="version">
    /// The migration version.
    /// </param>
    /// <param name="scriptName">
    /// The up-migration script file name.
    /// </param>
    /// <param name="description">
    /// The migration description.
    /// </param>
    /// <param name="sql">
    /// The SQL script content that applies the migration.
    /// </param>
    /// <param name="downScriptName">
    /// The down-migration script file name.
    /// </param>
    /// <param name="downSql">
    /// The SQL script content that reverts the migration.
    /// </param>
    public SqlScriptMigration(
        int version,
        string scriptName,
        string description,
        string sql,
        string downScriptName,
        string downSql )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( scriptName );
        ArgumentException.ThrowIfNullOrWhiteSpace( description );
        ArgumentException.ThrowIfNullOrWhiteSpace( sql );
        ArgumentException.ThrowIfNullOrWhiteSpace( downScriptName );
        ArgumentException.ThrowIfNullOrWhiteSpace( downSql );

        Version = version;
        ScriptName = scriptName;
        Description = description;
        Sql = sql;
        DownScriptName = downScriptName;
        DownSql = downSql;
    }
}
