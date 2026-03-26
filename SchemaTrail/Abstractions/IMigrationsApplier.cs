using System.Threading;
using System.Threading.Tasks;

namespace SchemaTrail.Abstractions;

/// <summary>
/// Defines a contract for applying database migrations.
/// </summary>
public interface IMigrationsApplier
{
    /// <summary>
    /// Applies pending migrations asynchronously.
    /// </summary>
    /// <param name="token">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous migration operation.
    /// </returns>
    Task ApplyAsync( CancellationToken token );
}
