using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchemaTrail;
using SchemaTrail.Abstractions;
using System;
using WebApi.Example.Providers;

var builder = WebApplication.CreateBuilder( args );

var connectionString =
    builder.Configuration.GetConnectionString( "DefaultConnection" )
    ?? throw new InvalidOperationException(
        "Connection string 'StateCache' is not configured." );

builder.Services
    .AddPooledDbContextFactory<MigrationsDbContext>( options => {
        options.UseNpgsql( connectionString );
    } );

builder.Services
    .AddScoped<IMigrationRunService, MigrationRunService>()
    .AddScoped<IMigrationExecutionService, MigrationExecutionService>()
    .AddScoped<IMigrationsApplier, MigrationsApplier>()
#region Embedded Sql Scripts Provider
    //.AddScoped<ISqlScriptsProvider, EmbeddedSqlScriptsProvider>( sp =>
    //    new EmbeddedSqlScriptsProvider( 
    //        Assembly.GetExecutingAssembly(),
    //        "WebApi.Example.Migrations" ) )
#endregion
#region File System Sql Scripts Provider
    //.AddScoped<ISqlScriptsProvider, FileSystemSqlScriptsProvider>( sp =>
    //    new FileSystemSqlScriptsProvider( ".../Migrations/Scripts" ) )
#endregion
#region Custom code-defined script provider
    //.AddScoped<ISqlScriptsProvider, MyCustomStringScriptProvider>()
#endregion
    .AddScoped<ISqlScriptsProvider, EmbeddedSqlScriptsProviderWrapper>();

builder.Services.AddControllers();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope()) {
    var migrationsApplier = scope.ServiceProvider.GetRequiredService<IMigrationsApplier>();
    await migrationsApplier.ApplyAsync( app.Lifetime.ApplicationStopping );
}

app.Run();
