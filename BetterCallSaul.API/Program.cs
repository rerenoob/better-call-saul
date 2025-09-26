using BetterCallSaul.API.Middleware;
using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Infrastructure.Data;
using BetterCallSaul.Infrastructure.Data.NoSQL;
using BetterCallSaul.Infrastructure.Repositories.NoSQL;
using BetterCallSaul.Infrastructure.Http;
using BetterCallSaul.Infrastructure.ML;
using BetterCallSaul.Infrastructure.Services;
using BetterCallSaul.Infrastructure.Services.Authentication;
using BetterCallSaul.Infrastructure.Services.AI;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using BetterCallSaul.Infrastructure.Services.LegalResearch;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Validate AWS configuration for production environment
if (!builder.Environment.IsDevelopment())
{
    // In ECS, AWS credentials are provided via IAM roles, not environment variables
    // Only validate that AWS_REGION is configured
    var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
    if (string.IsNullOrEmpty(awsRegion))
    {
        Log.Warning("AWS_REGION environment variable not set. Defaulting to us-east-1");
    }

    Log.Information("AWS configuration validated for production environment (using ECS IAM role)");
}

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serializer to handle circular references
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = false;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BetterCallSaul API", Version = "v1" });
});

// Add Entity Framework
if (builder.Environment.IsDevelopment())
{
    // Use SQLite for local development on Linux
    builder.Services.AddDbContext<BetterCallSaulContext>(options =>
    {
        options.UseSqlite("Data Source=BetterCallSaul.db");
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    });
}
else
{
    // Use PostgreSQL for production - read environment variables directly
    builder.Services.AddDbContext<BetterCallSaulContext>(options =>
    {
        var rdsHost = Environment.GetEnvironmentVariable("RDS_HOST");
        var rdsUsername = Environment.GetEnvironmentVariable("RDS_USERNAME");
        var rdsPassword = Environment.GetEnvironmentVariable("RDS_PASSWORD");
        
        if (string.IsNullOrEmpty(rdsHost) || string.IsNullOrEmpty(rdsUsername) || string.IsNullOrEmpty(rdsPassword))
        {
            throw new InvalidOperationException("RDS environment variables (RDS_HOST, RDS_USERNAME, RDS_PASSWORD) are not configured for production.");
        }
        
        var connectionString = $"Host={rdsHost};Port=5432;Database=BetterCallSaul;Username={rdsUsername};Password={rdsPassword}";
        options.UseNpgsql(connectionString);
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    });
}

// Add MongoDB configuration
if (builder.Environment.IsDevelopment())
{
    // Use local MongoDB for development
    builder.Services.AddSingleton<IMongoClient>(_ =>
        new MongoClient("mongodb://localhost:27017"));
}
else
{
    // Use AWS DocumentDB for production
    var documentDbConnectionString = builder.Configuration.GetConnectionString("DocumentDb") ??
        Environment.GetEnvironmentVariable("DOCUMENTDB_CONNECTION_STRING");

    if (string.IsNullOrEmpty(documentDbConnectionString))
    {
        throw new InvalidOperationException("DocumentDB connection string is not configured for production.");
    }

    builder.Services.AddSingleton<IMongoClient>(_ =>
    {
        var settings = MongoClientSettings.FromConnectionString(documentDbConnectionString);

        // Configure SSL settings for AWS DocumentDB
        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
            CheckCertificateRevocation = false,
            ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                // For AWS DocumentDB, we need to allow certificate chain issues
                // This is a known requirement for DocumentDB SSL connections
                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None ||
                       sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors;
            }
        };

        // Set connection timeout and retry settings
        settings.ConnectTimeout = TimeSpan.FromSeconds(30);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
        settings.SocketTimeout = TimeSpan.FromSeconds(60);

        return new MongoClient(settings);
    });
}

// Configure NoSQL settings
builder.Services.Configure<NoSqlOptions>(builder.Configuration.GetSection(NoSqlOptions.SectionName));
builder.Services.Configure<NoSqlSettings>(options =>
{
    options.DatabaseName = builder.Environment.IsDevelopment() ? "BetterCallSaulDev" : "BetterCallSaul";
});

// Register NoSQL context
builder.Services.AddScoped<NoSqlContext>();

// Database initialization will be handled after app.Build()

// Add ASP.NET Core Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<BetterCallSaulContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured. Set JWT_SECRET_KEY environment variable.");
}

var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Configure services based on environment
if (builder.Environment.IsDevelopment())
{
    // Development environment - use mock/local services
    builder.Services.AddScoped<IFileUploadService, FileUploadService>();
    builder.Services.AddScoped<IStorageService, LocalFileStorageService>();
    builder.Services.AddScoped<ITextExtractionService, MockTextExtractionService>();
    builder.Services.AddScoped<IAIService, MockAIService>();
    
    Log.Information("Development environment: Registered mock services");
    Log.Information("  IFileUploadService -> FileUploadService");
    Log.Information("  IStorageService -> LocalFileStorageService");
    Log.Information("  ITextExtractionService -> MockTextExtractionService");
    Log.Information("  IAIService -> MockAIService");
}
else
{
    // Production environment - use AWS services
    builder.Services.AddScoped<IFileUploadService, FileUploadService>(); // TODO: Replace with AWS implementation when available
    builder.Services.AddScoped<IStorageService, AWSS3StorageService>();

    // Register AWS Textract service as dependency for composite service
    builder.Services.AddScoped<AWSTextractService>();
    builder.Services.AddScoped<ITextExtractionService, CompositeTextExtractionService>();

    builder.Services.AddScoped<IAIService, AWSBedrockService>();

    Log.Information("Production environment: Registered AWS services");
    Log.Information("  IFileUploadService -> FileUploadService");
    Log.Information("  IStorageService -> AWSS3StorageService");
    Log.Information("  ITextExtractionService -> CompositeTextExtractionService");
    Log.Information("  IAIService -> AWSBedrockService");
}

builder.Services.AddScoped<IVirusScanningService, ClamAvService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<DatabaseSeedingService>();

builder.Services.AddHttpContextAccessor();

// Add legal research services
builder.Services.AddHttpClient<CourtListenerClient>();
builder.Services.AddHttpClient<JustiaClient>();
builder.Services.AddScoped<ICourtListenerService, CourtListenerService>();
builder.Services.AddScoped<IJustiaService, JustiaService>();
builder.Services.AddScoped<ICaseMatchingService, IntelligentCaseMatchingService>();
builder.Services.AddScoped<LegalTextSimilarity>();
builder.Services.AddMemoryCache();

// Register NoSQL repositories
builder.Services.AddScoped<ICaseDocumentRepository, CaseDocumentRepository>();
builder.Services.AddScoped<ILegalResearchRepository, LegalResearchRepository>();

// Register case management services
builder.Services.AddScoped<ICaseManagementService, CaseManagementService>();
builder.Services.AddScoped<AWSBedrockService>();

// Background services can be added here as needed
builder.Services.AddScoped<ICaseAnalysisService, CaseAnalysisService>();



// Configure AWS Options
builder.Services.Configure<AWSOptions>(options =>
{
    var section = builder.Configuration.GetSection(AWSOptions.SectionName);
    section.Bind(options);
});

// Configure Local Storage Options
builder.Services.Configure<LocalStorageOptions>(options =>
{
    var section = builder.Configuration.GetSection("LocalStorage");
    section.Bind(options);
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:5174",
                "https://localhost:5174",
                "https://d1c0215ar7cs56.cloudfront.net")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BetterCallSaulContext>();
    context.Database.EnsureCreated();
    
    // Seed roles first
    var seedingService = scope.ServiceProvider.GetRequiredService<DatabaseSeedingService>();
    await seedingService.SeedRolesAsync();
    
    // Seed registration codes if needed
    await seedingService.SeedRegistrationCodesAsync(100, 365, "System", "Initial seeding - 100 registration codes");

    // Seed admin user with configured email
    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "dphamsw@gmail.com";
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "TempAdmin123!";
    var adminName = Environment.GetEnvironmentVariable("ADMIN_NAME") ?? "Duong Pham";

    await seedingService.SeedAdminUserAsync(adminEmail, adminPassword, adminName);

    var stats = await seedingService.GetRegistrationCodeStatsAsync();
    Log.Information("Registration Code Stats - Total: {Total}, Active: {Active}, Used: {Used}, Expired: {Expired}",
        stats.Total, stats.Active, stats.Used, stats.Expired);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BetterCallSaul API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("ReactFrontend");

// Add health check endpoint for ALB
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
