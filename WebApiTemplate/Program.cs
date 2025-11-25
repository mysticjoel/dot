#region References
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.DatabaseOperation.Implementation;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
using WebApiTemplate.Service;
using WebApiTemplate.Service.Interface;
#endregion

var builder = WebApplication.CreateBuilder(args);

// database connection
builder.Services.AddDbContext<WenApiTemplateDbContext>(options =>
    options.UseNpgsql(GetPostgresConnectionString()));

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

// DI and controllers
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddControllers();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductOperation, ProductOperation>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Web API", Version = "v1" });
});

var app = builder.Build();

// Auto Apply Migrations 
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

// Swagger (unchanged behavior from your request snippet)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

//getting values from evnironment variables
static string GetPostgresConnectionString()
{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var database = Environment.GetEnvironmentVariable("DB_NAME");
    var username = Environment.GetEnvironmentVariable("DB_USER");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
    var port = Environment.GetEnvironmentVariable("DB_PORT");
    return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

// Helper to read Postgres connection from env vars (cloud). Returns null if incomplete.
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