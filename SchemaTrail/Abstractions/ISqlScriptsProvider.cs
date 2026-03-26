namespace SchemaTrail.Abstractions;

using SchemaTrail.Models;
using System.Collections.Generic;

/// <summary>
/// Provides access to SQL migration scripts and required SQL resources.
/// </summary>
public interface ISqlScriptsProvider
{
    /// <summary>
    /// Gets the collection of SQL migration scripts.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="SqlScriptMigration"/> instances.
    /// </returns>
    IReadOnlyList<SqlScriptMigration> GetMigrationScripts();

    /// <summary>
    /// Reads the entire contents of the required script resource.
    /// </summary>
    /// <param name="resourceName">
    /// The name of the embedded resource to read.
    /// </param>
    /// <returns>
    /// The full text content of the specified script resource.
    /// </returns>
    string ReadToEndRequiredScript( string resourceName );
}
