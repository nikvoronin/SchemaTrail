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

    /// <summary>
    /// Reverts applied migrations down to (but not including) the specified target version,
    /// executing their down-migration scripts in descending version order.
    /// </summary>
    /// <param name="targetVersion">
    /// The version to revert to. Migrations with a version greater than this value are reverted.
    /// Pass <c>0</c> to revert all applied migrations.
    /// </param>
    /// <param name="token">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous revert operation.
    /// </returns>
    Task RevertAsync( int targetVersion, CancellationToken token );
}
