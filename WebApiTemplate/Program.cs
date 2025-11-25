#region References
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using WebApiTemplate.Data;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.DatabaseOperation.Implementation;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
using WebApiTemplate.Service;
using WebApiTemplate.Service.Interface;
using WebApiTemplate.Validators;
#endregion

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Explicit Kestrel configuration
// -----------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(6000); // HTTP
    options.ListenLocalhost(6001, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

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

// Controllers with improved validation behavior
builder.Services.AddControllers()
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

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductOperation, ProductOperation>();

// Authentication services
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// -----------------------------
// JWT Authentication Configuration
// -----------------------------
var jwtService = new JwtService(builder.Configuration);
var secretKey = jwtService.SecretKey;

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };
});

builder.Services.AddAuthorization();

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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// -----------------------------
// Auto Apply Migrations
// -----------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<WenApiTemplateDbContext>();

    context.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
            ""MigrationId"" varchar(150) PRIMARY KEY,
            ""ProductVersion"" varchar(32) NOT NULL
        )
    ");

    context.Database.Migrate();
}

// Development-only automatic migrations
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WenApiTemplateDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied (Development).");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply migrations in Development.");
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