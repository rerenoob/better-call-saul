using BetterCallSaul.API.Controllers;
using BetterCallSaul.API.Middleware;
using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Data;
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
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
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
        options.UseSqlite("Data Source=BetterCallSaul.db"));
}
else
{
    // Use SQL Server for production
    builder.Services.AddDbContext<BetterCallSaulContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

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
// Configure file upload service based on Azure storage settings
builder.Services.AddScoped<IFileUploadService>(serviceProvider =>
{
    var azureOptions = serviceProvider.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
    var logger = serviceProvider.GetRequiredService<ILogger<IFileUploadService>>();
    
    if (azureOptions.UseAzureStorage && !string.IsNullOrEmpty(azureOptions.ConnectionString))
    {
        var azureLogger = serviceProvider.GetRequiredService<ILogger<AzureBlobStorageService>>();
        return new AzureBlobStorageService(
            serviceProvider.GetRequiredService<IOptions<AzureBlobStorageOptions>>(),
            azureLogger);
    }
    else
    {
        logger.LogWarning("Azure Blob Storage not configured or disabled, falling back to local file storage");
        var context = serviceProvider.GetRequiredService<BetterCallSaulContext>();
        var fileValidationService = serviceProvider.GetRequiredService<IFileValidationService>();
        var textExtractionService = serviceProvider.GetRequiredService<ITextExtractionService>();
        var fileUploadLogger = serviceProvider.GetRequiredService<ILogger<FileUploadService>>();
        return new FileUploadService(context, fileValidationService, textExtractionService, fileUploadLogger);
    }
});
builder.Services.AddScoped<IVirusScanningService, ClamAvService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();

// Configure text extraction service - use Azure Form Recognizer in production, mock in development
builder.Services.AddScoped<ITextExtractionService>(serviceProvider =>
{
    var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
    var formRecognizerOptions = serviceProvider.GetRequiredService<IOptions<FormRecognizerOptions>>().Value;
    
    // Use Azure Form Recognizer if configured and not in development, otherwise use mock
    if (!env.IsDevelopment() && 
        !string.IsNullOrEmpty(formRecognizerOptions.Endpoint) && 
        !string.IsNullOrEmpty(formRecognizerOptions.ApiKey))
    {
        var logger = serviceProvider.GetRequiredService<ILogger<AzureFormRecognizerService>>();
        return new AzureFormRecognizerService(
            serviceProvider.GetRequiredService<IOptions<FormRecognizerOptions>>(),
            logger);
    }
    else
    {
        var logger = serviceProvider.GetRequiredService<ILogger<MockTextExtractionService>>();
        return new MockTextExtractionService(logger);
    }
});
builder.Services.AddScoped<DatabaseSeedingService>();

builder.Services.AddHttpContextAccessor();

// Add legal research services
builder.Services.AddHttpClient<CourtListenerClient>();
builder.Services.AddHttpClient<JustiaClient>();
builder.Services.AddScoped<ICourtListenerService, CourtListenerService>();
builder.Services.AddScoped<IJustiaService, JustiaService>();
builder.Services.AddScoped<UnifiedLegalSearchService>();
builder.Services.AddScoped<ICaseMatchingService, IntelligentCaseMatchingService>();
builder.Services.AddScoped<LegalTextSimilarity>();
builder.Services.AddMemoryCache();

// Configure Azure OpenAI
builder.Services.Configure<OpenAIOptions>(options =>
{
    var section = builder.Configuration.GetSection(OpenAIOptions.SectionName);
    section.Bind(options);
    
    // Map configuration to new property names
    options.EndpointFromConfig = section["Endpoint"];
    options.ApiKeyFromConfig = section["ApiKey"];
});
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<ICaseAnalysisService, CaseAnalysisService>();

// Configure Azure Blob Storage
builder.Services.Configure<AzureBlobStorageOptions>(options =>
{
    var section = builder.Configuration.GetSection(AzureBlobStorageOptions.SectionName);
    section.Bind(options);
    
    // Override connection string from environment variable if provided
    var connectionString = Environment.GetEnvironmentVariable("AZURE_BLOB_STORAGE_CONNECTION_STRING");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.ConnectionString = connectionString;
    }
    
    // Override UseAzureStorage from environment variable if provided
    var useAzureStorage = Environment.GetEnvironmentVariable("USE_AZURE_STORAGE");
    if (!string.IsNullOrEmpty(useAzureStorage) && bool.TryParse(useAzureStorage, out var useAzure))
    {
        options.UseAzureStorage = useAzure;
    }
});

// Configure Azure Form Recognizer
builder.Services.Configure<FormRecognizerOptions>(options =>
{
    var section = builder.Configuration.GetSection(FormRecognizerOptions.SectionName);
    section.Bind(options);
    
    // Override from environment variables if provided
    var endpoint = Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_ENDPOINT");
    var apiKey = Environment.GetEnvironmentVariable("AZURE_FORM_RECOGNIZER_API_KEY");
    
    if (!string.IsNullOrEmpty(endpoint))
        options.EndpointFromConfig = endpoint;
    if (!string.IsNullOrEmpty(apiKey))
        options.ApiKeyFromConfig = apiKey;
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
                "https://orange-island-0a659d210.1.azurestaticapps.net")
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
