using Microsoft.Data.SqlClient;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Test database connection
TestDatabaseConnection(connectionString);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

// Method to test database connection
static void TestDatabaseConnection(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("❌ ERROR: Connection string is not configured!");
        Console.WriteLine("Please set up user secrets or environment variables.");
        return;
    }

    try
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
    
        Console.WriteLine("✅ SUCCESS: Database connection is working!");
        Console.WriteLine($"Connected to: {connection.Database}");
        
        connection.Close();
    }
    catch (SqlException ex)
    {
        Console.WriteLine("❌ ERROR: Database connection failed!");
        Console.WriteLine($"Error Message: {ex.Message}");
        Console.WriteLine($"Error Number: {ex.Number}");
        Console.WriteLine("\nCommon solutions:");
        Console.WriteLine("1. Check if SQL Server is running");
        Console.WriteLine("2. Verify your connection string");
        Console.WriteLine("3. Ensure the database exists");
        Console.WriteLine("4. Check network connectivity");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ ERROR: Unexpected error while testing database connection!");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
