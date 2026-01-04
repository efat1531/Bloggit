using Bloggit.API;
using Bloggit.API.Middleware;
using Microsoft.Data.SqlClient;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

// Set minimum log level
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Configure log levels for different categories
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Create a logger for startup
var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information));
var startupLogger = loggerFactory.CreateLogger<Program>();

startupLogger.LogInformation("🚀 Application is starting...");
startupLogger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

// Test database connection
TestDatabaseConnection(connectionString, startupLogger);

// Configure all services using DependencyInjection
builder.Services.AddAllServices(connectionString!, builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    logger.LogInformation("🔧 Running in Development mode");
    logger.LogInformation("📚 API documentation available at /scalar/v1");
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Bloggit API Documentation")
            .WithTheme(ScalarTheme.Default)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.UseRequestLogging();

// Add security headers middleware (skip in Development to allow Scalar API docs)
if (!app.Environment.IsDevelopment())
{
    app.UseSecurityHeaders();
}

app.UseHttpsRedirection();

// Add Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Seed default roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

    string[] roles = ["User", "Admin"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role));
            logger.LogInformation("Created default '{Role}' role", role);
        }
    }
}

logger.LogInformation("✅ Application configured successfully");
logger.LogInformation("🌐 Starting web server...");

app.Run();

logger.LogInformation("🛑 Application is shutting down...");


// Method to test database connection
static void TestDatabaseConnection(string? connectionString, ILogger logger)
{
    if (string.IsNullOrEmpty(connectionString))
    {
        logger.LogError("❌ ERROR: Connection string is not configured!");
        logger.LogWarning("Please set up user secrets or environment variables.");
        return;
    }

    try
    {
        logger.LogInformation("🔌 Testing database connection...");
        
        using var connection = new SqlConnection(connectionString);
        connection.Open();
    
        logger.LogInformation("✅ SUCCESS: Database connection is working!");
        logger.LogInformation("Connected to database: {DatabaseName}", connection.Database);
        logger.LogInformation("Server version: {ServerVersion}", connection.ServerVersion);
        
        connection.Close();
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "❌ ERROR: Database connection failed!");
        logger.LogError("Error Number: {ErrorNumber}", ex.Number);
        logger.LogWarning("Common solutions:");
        logger.LogWarning("1. Check if SQL Server is running");
        logger.LogWarning("2. Verify your connection string");
        logger.LogWarning("3. Ensure the database exists");
        logger.LogWarning("4. Check network connectivity");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "❌ CRITICAL: Unexpected error while testing database connection!");
    }
}