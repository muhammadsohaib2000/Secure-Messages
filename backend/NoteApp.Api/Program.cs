using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NoteApp.Application.Services;
using NoteApp.Domain.Interfaces;
using NoteApp.Infrastructure.Data;
using NoteApp.Infrastructure.Repositories;
using System.Text.Json;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Set development environment
builder.Environment.EnvironmentName = "Development";

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure logging - set to Debug for maximum detail
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add Entity Framework logging (optional but helpful for debugging)
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
});

// Configure PostgreSQL database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=notesdb;Username=postgres;Password=12345";

Console.WriteLine($"Using connection string: {connectionString}");

// First, ensure the PostgreSQL database exists
EnsurePostgresDbExists(connectionString);

// Add DbContext with detailed logging
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        
        npgsqlOptions.CommandTimeout(30); // Increase timeout
    });
    
    // Add detailed command logging
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    options.UseLoggerFactory(loggerFactory);
    options.EnableSensitiveDataLogging(); // Shows parameter values in logs
    options.EnableDetailedErrors();
});

// Use in-memory caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Register repositories and services
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<INoteService, NoteService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Important: Place CORS middleware before routing
app.UseCors("AllowAll");

// Add detailed error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception occurred");

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var errorDetail = ex.ToString();
        var innerMessage = ex.InnerException?.Message;
        var innerStack = ex.InnerException?.StackTrace;
        
        // Log additional detailed error information
        if (ex.InnerException != null)
        {
            logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
            logger.LogError("Inner exception stack: {StackTrace}", ex.InnerException.StackTrace);
            
            if (ex.InnerException.InnerException != null)
            {
                logger.LogError("Third-level exception: {Message}", ex.InnerException.InnerException.Message);
            }
        }
        
        var errorResponse = new
        {
            Error = "An error occurred while processing your request",
            Details = ex.Message,
            InnerError = innerMessage,
            StackTrace = app.Environment.IsDevelopment() ? ex.StackTrace : null
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Force PostgreSQL to create schema
        logger.LogInformation("Creating database schema if it doesn't exist");
        string createSchema = @"
            CREATE TABLE IF NOT EXISTS ""Notes"" (
                ""Id"" UUID PRIMARY KEY,
                ""Content"" TEXT NOT NULL,
                ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                ""ExpiresAt"" TIMESTAMP WITH TIME ZONE NULL,
                ""IsViewed"" BOOLEAN NOT NULL DEFAULT FALSE
            );";
        
        await dbContext.Database.ExecuteSqlRawAsync(createSchema);
        logger.LogInformation("Database schema created successfully");
        
        // Verify connection
        bool canConnect = await dbContext.Database.CanConnectAsync();
        logger.LogInformation("Database connection test: {Result}", canConnect ? "SUCCESS" : "FAILED");
        
        if (!canConnect)
        {
            throw new Exception("Cannot connect to database after schema creation");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        
        // Log detailed error
        if (ex.InnerException != null)
        {
            logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
        }
        
        throw; // Re-throw to prevent application from starting with bad database
    }
}

app.Run();

// Utility function to ensure the PostgreSQL database exists
void EnsurePostgresDbExists(string connectionString)
{
    try
    {
        // Parse the connection string
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database;
        
        // Remove the database name to connect to the default 'postgres' database
        builder.Database = "postgres";
        
        Console.WriteLine($"Checking if database '{database}' exists...");
        
        using (var connection = new NpgsqlConnection(builder.ConnectionString))
        {
            connection.Open();
            
            // Check if the database exists
            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{database}'";
                var exists = cmd.ExecuteScalar() != null;
                
                if (!exists)
                {
                    Console.WriteLine($"Database '{database}' does not exist. Creating it now...");
                    
                    // Create the database
                    using (var createCmd = new NpgsqlCommand())
                    {
                        createCmd.Connection = connection;
                        createCmd.CommandText = $"CREATE DATABASE \"{database}\"";
                        createCmd.ExecuteNonQuery();
                        Console.WriteLine($"Database '{database}' created successfully");
                    }
                }
                else
                {
                    Console.WriteLine($"Database '{database}' already exists");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error ensuring database exists: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner error: {ex.InnerException.Message}");
        }
        throw;
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
