using Microsoft.EntityFrameworkCore;
using SchemaTrail.Models;

namespace SchemaTrail;

/// <summary>
/// Represents the database context used to store applied migrations
/// and migration run records.
/// </summary>
/// <param name="options">
/// The options used to configure the database context.
/// </param>
public sealed class MigrationsDbContext(
    DbContextOptions<MigrationsDbContext> options ) 
    : DbContext( options )
{
    /// <summary>
    /// Gets the set of applied migrations.
    /// </summary>
    public DbSet<MigrationEntity> AppliedMigrations =>
        Set<MigrationEntity>();

    /// <summary>
    /// Gets the set of migration run records.
    /// </summary>
    public DbSet<MigrationRunEntity> MigrationRuns => 
        Set<MigrationRunEntity>();

    /// <inheritdoc/>
    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        base.OnModelCreating( modelBuilder );

        ConfigureAppliedMigrations( modelBuilder );
        ConfigureMigrationRuns( modelBuilder );
    }

    private static void ConfigureAppliedMigrations( ModelBuilder modelBuilder )
    {
        var entity = modelBuilder.Entity<MigrationEntity>();

        entity.ToTable( STORAGE_MIGRATION_TABLENAME );

        entity.HasKey( x => x.Version )
            .HasName( $"pk_{STORAGE_MIGRATION_TABLENAME}" );

        entity.Property( x => x.Version )
            .HasColumnName( "version" )
            .ValueGeneratedNever();

        entity.Property( x => x.ScriptName )
            .HasColumnName( "script_name" )
            .HasColumnType( "text" )
            .IsRequired();

        entity.Property( x => x.Description )
            .HasColumnName( "description" )
            .HasColumnType( "text" )
            .IsRequired();

        entity.Property( x => x.Checksum )
            .HasColumnName( "checksum" )
            .HasColumnType( "text" )
            .IsRequired();

        entity.Property( x => x.AppliedAt )
            .HasColumnName( "applied_at" )
            .HasColumnType( "timestamp with time zone" )
            .IsRequired();

        entity.HasIndex( x => x.ScriptName )
            .HasDatabaseName( $"ix_{STORAGE_MIGRATION_TABLENAME}_script_name" )
            .IsUnique();
    }

    private static void ConfigureMigrationRuns( ModelBuilder modelBuilder )
    {
        var entity = modelBuilder.Entity<MigrationRunEntity>();

        entity.ToTable(
            $"{STORAGE_MIGRATION_TABLENAME}_runs",
            tableBuilder => {
                tableBuilder.HasCheckConstraint(
                    $"ck_{STORAGE_MIGRATION_TABLENAME}_runs_status",
                    "status in ('running', 'success', 'failed')" );
            } );

        entity.HasKey( x => x.Id )
            .HasName( $"pk_{STORAGE_MIGRATION_TABLENAME}_runs" );

        entity.Property( x => x.Id )
            .HasColumnName( "id" );

        entity.Property( x => x.Version )
            .HasColumnName( "version" )
            .IsRequired();

        entity.Property( x => x.ScriptName )
            .HasColumnName( "script_name" )
            .HasColumnType( "text" )
            .IsRequired();

        entity.Property( x => x.Description )
            .HasColumnName( "description" )
            .HasColumnType( "text" )
            .IsRequired();

        entity.Property( x => x.Checksum )
            .HasColumnName( "checksum" )
            .HasColumnType( "text" )
            .IsRequired();

        entity.Property( x => x.Status )
            .HasColumnName( "status" )
            .HasColumnType( "text" )
            .IsRequired();

        entity.Property( x => x.StartedAt )
            .HasColumnName( "started_at" )
            .HasColumnType( "timestamp with time zone" )
            .IsRequired();

        entity.Property( x => x.CompletedAt )
            .HasColumnName( "completed_at" )
            .HasColumnType( "timestamp with time zone" );

        entity.Property( x => x.DurationMs )
            .HasColumnName( "duration_ms" )
            .HasColumnType( "bigint" );

        entity.Property( x => x.ErrorText )
            .HasColumnName( "error_text" )
            .HasColumnType( "text" );

        entity.HasIndex( x => new { x.Version, x.StartedAt } )
            .HasDatabaseName( $"ix_{STORAGE_MIGRATION_TABLENAME}_runs_version_started_at" );
    }

    /// <summary>
    /// The name of the table used to store applied migrations.
    /// </summary>
    public const string STORAGE_MIGRATION_TABLENAME = "storage_migrations";
}
