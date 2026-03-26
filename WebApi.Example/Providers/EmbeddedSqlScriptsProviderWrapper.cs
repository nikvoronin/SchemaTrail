using SchemaTrail.Providers;
using System.Reflection;

namespace WebApi.Example.Providers;

/// <summary>
/// Wraps <see cref="EmbeddedSqlScriptsProvider"/> and configures it to load
/// embedded SQL migration scripts from the current assembly.
/// </summary>
internal class EmbeddedSqlScriptsProviderWrapper() :
    EmbeddedSqlScriptsProvider(
        Assembly.GetExecutingAssembly(),
        "WebApi.Example.Migrations" )
{ }
