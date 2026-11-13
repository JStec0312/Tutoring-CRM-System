using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Tutoring.Api;
using Tutoring.Api.Configuration;
using Tutoring.Infrastructure;
using Tutoring.Infrastructure.Persistence;
using DotNetEnv;
using Tutoring.Api.Dev;


Env
    .TraversePath()
    .Load(Path.Combine(AppContext.BaseDirectory, ".env"));

var runMigrations = args.Contains("--migrate", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--migrate").ToArray());

// Explicit deployment mode: no HTTP host, authentication, or messaging workers.
if (runMigrations)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is required for migrations.");
    var options = new DbContextOptionsBuilder<TutoringDbContext>()
        .UseSqlServer(connectionString)
        .Options;
    await using var dbContext = new TutoringDbContext(options);
    await dbContext.Database.MigrateAsync();
    return;
}

builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();

const string expoDevelopmentCors = "ExpoDevelopment";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddPolicy(expoDevelopmentCors, policy =>
            policy.WithOrigins("http://localhost:8081", "http://127.0.0.1:8081")
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .WithHeaders("Authorization", "Content-Type")
                .WithExposedHeaders("Content-Disposition")
                .AllowCredentials()));
}

builder.Services.AddInfrastructure(builder.Configuration);

if (!builder.Environment.IsEnvironment("IntegrationTests"))
{
    builder.Services.AddMessagingWorkers();
}

builder.Services.AddAuthenticationServices(builder.Configuration);


builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


// bootstrap 
if (builder.Environment.IsDevelopment())
{
        builder.Services.AddScoped<DevelopmentDataBootstrapper>();
        
}





var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseCors(expoDevelopmentCors);
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

