#region References

using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using WebApiTemplate.BackgroundServices;
using WebApiTemplate.Configuration;
using WebApiTemplate.Data;
using WebApiTemplate.Filters;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.DatabaseOperation.Implementation;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
using WebApiTemplate.Service;
using WebApiTemplate.Service.Interface;
using WebApiTemplate.Validators;

#endregion


var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Database connection
// -----------------------------
// Prefer cloud env vars for Postgres; otherwise use local dev connection string
string? envConn = GetPostgresConnectionStringFromEnv();
string? devConfigConn = builder.Configuration.GetConnectionString("DefaultConnection");

string connectionString = !string.IsNullOrWhiteSpace(envConn)
    ? envConn
    : devConfigConn ?? throw new InvalidOperationException(
        "No connection string found. Set ConnectionStrings:DefaultConnection in appsettings.Development.json or provide DB_* environment variables."
    );

// PostgreSQL DbContext
builder.Services.AddDbContext<WenApiTemplateDbContext>(options =>
    options.UseNpgsql(connectionString));

// -----------------------------
// Dependency Injection and Controllers
// -----------------------------
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// FluentValidation setup (core library only)
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

// Controllers with improved validation behavior and global filters
builder.Services.AddControllers(options =>
{
    // Add global filters for all controllers
    options.Filters.Add<ActivityLoggingFilter>();
    options.Filters.Add<CacheControlFilter>(); // No caching by default for auction data
})
.ConfigureApiBehaviorOptions(options =>
{
    // Customize validation error response
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        var result = new
        {
            message = "Validation failed",
            errors = errors
        };

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(result);
    };
});

// Register filters for dependency injection
builder.Services.AddScoped<ActivityLoggingFilter>();
builder.Services.AddScoped<ValidateModelStateFilter>();
builder.Services.AddScoped<CacheControlFilter>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductOperation, ProductOperation>();

builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IBidOperation, BidOperation>();

// Auction settings configuration
builder.Services.Configure<AuctionSettings>(
    builder.Configuration.GetSection("AuctionSettings"));

// Payment settings configuration
builder.Services.Configure<PaymentSettings>(
    builder.Configuration.GetSection("PaymentSettings"));

// SMTP settings configuration
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));

// Auction extension service
builder.Services.AddScoped<IAuctionExtensionService, AuctionExtensionService>();

// Payment services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentOperation, PaymentOperation>();

// Email service
builder.Services.AddScoped<IEmailService, EmailService>();

// Dashboard service
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Background service for auction monitoring
builder.Services.AddHostedService<AuctionMonitoringService>();

// Background service for payment retry queue
builder.Services.AddHostedService<RetryQueueService>();


builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<WebApiTemplate.Service.QueryParser.IAsqlParser, WebApiTemplate.Service.QueryParser.AsqlParser>();


// -----------------------------
// JWT Key Configuration
// -----------------------------
byte[] jwtKeyBytes;
var configuredKeyBase64 = builder.Configuration["Jwt:SecretKeyBase64"];
var configuredKey = builder.Configuration["Jwt:SecretKey"];
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

if (!string.IsNullOrWhiteSpace(configuredKeyBase64))
{
    jwtKeyBytes = Convert.FromBase64String(configuredKeyBase64);
}
else if (!string.IsNullOrWhiteSpace(configuredKey))
{
    jwtKeyBytes = Encoding.UTF8.GetBytes(configuredKey);
}
else if (!string.IsNullOrWhiteSpace(dbPassword))
{
    var fixedSalt = SHA256.HashData(Encoding.UTF8.GetBytes("WebApiTemplate:JwtService:DerivationSalt:v1"));
    using var pbkdf2 = new Rfc2898DeriveBytes(dbPassword, fixedSalt, 200_000, HashAlgorithmName.SHA256);
    jwtKeyBytes = pbkdf2.GetBytes(32);
}
else
{
    throw new InvalidOperationException("JWT SecretKey not configured. Add Jwt:SecretKeyBase64, Jwt:SecretKey, or set DB_PASSWORD environment variable.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };
});

builder.Services.AddAuthorization();

// -----------------------------
// CORS Configuration
// -----------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4201") // Angular dev servers
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// -----------------------------
// Swagger
// -----------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BidSphere API",
        Version = "v1",
        Description = "API for BidSphere auction management system"
    });

    // Add JWT Authentication support in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array. Empty<string>()
        }
    });
});

var app = builder.Build();

// -----------------------------
// Auto Apply Migrations
// -----------------------------
// Auto Apply Migrations with try-catch
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<WenApiTemplateDbContext>();

    try
    {
        // Ensure migrations history table exists (idempotent)
        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId"" varchar(150) PRIMARY KEY,
                ""ProductVersion"" varchar(32) NOT NULL
            );
        ");

        // Apply pending migrations
        context.Database.Migrate();

        logger.LogInformation("Database initialization completed: '__EFMigrationsHistory' ensured and migrations applied.");
    }
    catch (PostgresException pgEx)
    {
        // Handles cases like relation already exists, role/user exists, etc.
        logger.LogWarning(pgEx, "Postgres exception during database initialization. Continuing startup. SqlState={SqlState}", pgEx.SqlState);
    }
    catch (DbUpdateException dbEx)
    {
        logger.LogError(dbEx, "EF Core DbUpdateException during database initialization.");
        // Optionally rethrow if you want startup to fail:
        // throw;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during database initialization.");
        // Decide whether to continue or fail fast
        // throw;
    }
}

// Development-only automatic migrations (optional; guard to avoid double-run)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WenApiTemplateDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // If you already migrated above, consider skipping to avoid duplicate work
        // db. Database.Migrate();
        logger.LogInformation("Development environment detected. Database already initialized at startup.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Development migration step encountered an issue, but continuing.");
    }
}

// -----------------------------
// Seed Default Admin User
// -----------------------------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WenApiTemplateDbContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await AdminSeeder.SeedAdminAsync(context, configuration, logger);
}

// -----------------------------
// Middleware
// -----------------------------
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Enable CORS - must come before Authentication
app.UseCors("AllowAngularApp");

app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

// -----------------------------
// Run the app
// -----------------------------
app.Run();

// -----------------------------
// Helper method for DB connection from environment variables
// -----------------------------
static string? GetPostgresConnectionStringFromEnv()
{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var database = Environment.GetEnvironmentVariable("DB_NAME");
    var username = Environment.GetEnvironmentVariable("DB_USER");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(database) ||
        string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password))
    {
        return null;
    }

    return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}