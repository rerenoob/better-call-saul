using BetterCallSaul.Infrastructure.Data;
using BetterCallSaul.Infrastructure.Data.NoSQL;
using BetterCallSaul.Infrastructure.Services;
using BetterCallSaul.Infrastructure.Repositories.NoSQL;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace BetterCallSaul.Tests.Integration
{
    public class DatabaseWiringTests
    {
        [Fact]
        public void PostgreSQL_Context_ShouldBeProperlyConfigured()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=test-host;Port=5432;Database=BetterCallSaul;Username=testuser;Password=testpass"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();

            // Act - Configure like in production environment
            services.AddDbContext<BetterCallSaulContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<BetterCallSaulContext>();

            Assert.NotNull(context);
            Assert.NotNull(context.Cases);
            Assert.NotNull(context.Documents);
            Assert.NotNull(context.Users);
            Assert.NotNull(context.RegistrationCodes);
        }

        [Fact]
        public void MongoDB_Context_ShouldBeProperlyConfigured()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Mock MongoDB client for testing
            var mockClient = new MongoClient("mongodb://localhost:27017");
            services.AddSingleton<IMongoClient>(mockClient);

            // Configure NoSQL settings
            services.Configure<NoSqlSettings>(options =>
            {
                options.DatabaseName = "BetterCallSaulTest";
            });

            services.AddScoped<NoSqlContext>();

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var noSqlContext = serviceProvider.GetRequiredService<NoSqlContext>();

            // Assert
            Assert.NotNull(noSqlContext);
            Assert.NotNull(noSqlContext.CaseDocuments);
            Assert.NotNull(noSqlContext.LegalResearchDocuments);
            Assert.NotNull(noSqlContext.CaseMatches);
        }

        [Fact]
        public void DatabaseRepositories_ShouldBeProperlyWired()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Configure SQL Database
            services.AddDbContext<BetterCallSaulContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            // Configure MongoDB
            var mockClient = new MongoClient("mongodb://localhost:27017");
            services.AddSingleton<IMongoClient>(mockClient);
            services.Configure<NoSqlSettings>(options =>
            {
                options.DatabaseName = "BetterCallSaulTest";
            });
            services.AddScoped<NoSqlContext>();

            // Register repositories
            services.AddScoped<ICaseDocumentRepository, CaseDocumentRepository>();
            services.AddScoped<ILegalResearchRepository, LegalResearchRepository>();

            // Register services that use both databases
            services.AddScoped<ICaseManagementService, CaseManagementService>();

            // Act
            var serviceProvider = services.BuildServiceProvider();

            // Assert - All services should resolve without errors
            var caseManagementService = serviceProvider.GetRequiredService<ICaseManagementService>();
            var caseDocumentRepo = serviceProvider.GetRequiredService<ICaseDocumentRepository>();
            var legalResearchRepo = serviceProvider.GetRequiredService<ILegalResearchRepository>();

            Assert.NotNull(caseManagementService);
            Assert.NotNull(caseDocumentRepo);
            Assert.NotNull(legalResearchRepo);
        }

        [Fact]
        public void ProductionConfiguration_ShouldMapCorrectly()
        {
            // Arrange - Simulate production environment variables
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // PostgreSQL configuration (from CloudFormation)
                    ["ConnectionStrings:DefaultConnection"] = "Host=${RDS_HOST};Port=5432;Database=BetterCallSaul;Username=${RDS_USERNAME};Password=${RDS_PASSWORD}",

                    // DocumentDB configuration (from CloudFormation)
                    ["NoSql:ConnectionString"] = "${DOCUMENTDB_CONNECTION_STRING}",
                    ["NoSql:DatabaseName"] = "BetterCallSaul",

                    // JWT configuration
                    ["JwtSettings:SecretKey"] = "${JWT_SECRET_KEY}",
                })
                .Build();

            // Act & Assert - Configuration should be readable
            var defaultConnection = configuration.GetConnectionString("DefaultConnection");
            var noSqlConnection = configuration.GetSection("NoSql:ConnectionString").Value;
            var jwtSecret = configuration.GetSection("JwtSettings:SecretKey").Value;

            Assert.Contains("${RDS_HOST}", defaultConnection);
            Assert.Contains("Database=BetterCallSaul", defaultConnection);
            Assert.Equal("${DOCUMENTDB_CONNECTION_STRING}", noSqlConnection);
            Assert.Equal("${JWT_SECRET_KEY}", jwtSecret);
        }

        [Fact]
        public void Database_TablesAreConfigured()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDbContext<BetterCallSaulContext>(options =>
                options.UseInMemoryDatabase("TablesTestDb"));

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<BetterCallSaulContext>();

            // Assert - Verify all main tables are accessible
            Assert.NotNull(context.Cases);
            Assert.NotNull(context.Documents);
            Assert.NotNull(context.DocumentAnnotations);
            Assert.NotNull(context.AuditLogs);
            Assert.NotNull(context.CaseAnalyses);
            Assert.NotNull(context.LegalCases);
            Assert.NotNull(context.CourtOpinions);
            Assert.NotNull(context.JustiaSearchResults);
            Assert.NotNull(context.LegalStatutes);
            Assert.NotNull(context.CaseMatches);
            Assert.NotNull(context.MatchingCriteria);
            Assert.NotNull(context.RegistrationCodes);
        }
    }
}