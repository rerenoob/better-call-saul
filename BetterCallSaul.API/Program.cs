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

// Configure services based on environment
if (builder.Environment.IsDevelopment())
{
    // Development environment - use mock/local services
    builder.Services.AddScoped<IFileUploadService, FileUploadService>();
    builder.Services.AddScoped<IStorageService, FileUploadService>();
    builder.Services.AddScoped<ITextExtractionService, MockTextExtractionService>();
    builder.Services.AddScoped<IAIService, MockAIService>();
    
    Log.Information("Development environment: Registered mock services");
    Log.Information("  IFileUploadService -> FileUploadService");
    Log.Information("  IStorageService -> FileUploadService");
    Log.Information("  ITextExtractionService -> MockTextExtractionService");
    Log.Information("  IAIService -> MockAIService");
}
else
{
    // Production environment - use AWS services
    builder.Services.AddScoped<IFileUploadService, FileUploadService>(); // TODO: Replace with AWS implementation when available
    builder.Services.AddScoped<IStorageService, AWSS3StorageService>();
    builder.Services.AddScoped<ITextExtractionService, AWSTextractService>();
    builder.Services.AddScoped<IAIService, AWSBedrockService>();
    
    Log.Information("Production environment: Registered AWS services");
    Log.Information("  IFileUploadService -> FileUploadService");
    Log.Information("  IStorageService -> AWSS3StorageService");
    Log.Information("  ITextExtractionService -> AWSTextractService");
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
builder.Services.AddScoped<UnifiedLegalSearchService>();
builder.Services.AddScoped<ICaseMatchingService, IntelligentCaseMatchingService>();
builder.Services.AddScoped<LegalTextSimilarity>();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<AWSBedrockService>();
builder.Services.AddScoped<ICaseAnalysisService, CaseAnalysisService>();

// Configure Cloud Provider Options
builder.Services.Configure<CloudProviderOptions>(options =>
{
    var section = builder.Configuration.GetSection(CloudProviderOptions.SectionName);
    section.Bind(options);
    
    // Override Active provider from environment variable
    var cloudProvider = Environment.GetEnvironmentVariable("CLOUD_PROVIDER");
    if (!string.IsNullOrEmpty(cloudProvider) && (cloudProvider == "Azure" || cloudProvider == "AWS"))
    {
        options.Active = cloudProvider;
    }
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
