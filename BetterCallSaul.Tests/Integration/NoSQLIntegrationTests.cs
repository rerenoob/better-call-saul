using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Core.Models.NoSQL;
using BetterCallSaul.Infrastructure.Data;
using BetterCallSaul.Infrastructure.Data.NoSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BetterCallSaul.Tests.Integration;

public class NoSQLIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private readonly BetterCallSaulContext _sqlContext;
    private readonly NoSqlContext _nosqlContext;
    private readonly ICaseDocumentRepository _caseDocumentRepository;

    public NoSQLIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _sqlContext = fixture.CreateContext();
        _nosqlContext = fixture.CreateNoSqlContext();
        _caseDocumentRepository = fixture.Services.GetRequiredService<ICaseDocumentRepository>();
    }

    [Fact]
    public async Task HybridOperation_CreateCaseInBothDatabases_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var caseId = Guid.NewGuid();

        // Create SQL user (if needed for foreign key constraints)
        var user = new Core.Models.Entities.User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        };
        
        _sqlContext.Users.Add(user);
        await _sqlContext.SaveChangesAsync();

        // Create NoSQL case document
        var caseDocument = new CaseDocument
        {
            CaseId = caseId,
            UserId = userId,
            Title = "Integration Test Case",
            Description = "Test case for hybrid operations",
            CaseNumber = "TEST-001",
            Status = Core.Enums.CaseStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var createdDocument = await _caseDocumentRepository.CreateAsync(caseDocument);

        // Assert
        Assert.NotNull(createdDocument);
        Assert.Equal(caseId, createdDocument.CaseId);
        Assert.Equal(userId, createdDocument.UserId);

        // Verify document can be retrieved
        var retrievedDocument = await _caseDocumentRepository.GetByIdAsync(caseId);
        Assert.NotNull(retrievedDocument);
        Assert.Equal("Integration Test Case", retrievedDocument.Title);

        // Cleanup
        await _caseDocumentRepository.DeleteAsync(caseId);
        _sqlContext.Users.Remove(user);
        await _sqlContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CrossDatabaseQuery_GetUserCases_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Create SQL user
        var user = new Core.Models.Entities.User
        {
            Id = userId,
            Email = "test2@example.com",
            FirstName = "Test",
            LastName = "User2",
            CreatedAt = DateTime.UtcNow
        };
        
        _sqlContext.Users.Add(user);
        await _sqlContext.SaveChangesAsync();

        // Create multiple NoSQL case documents for the user
        var caseDocuments = new List<CaseDocument>
        {
            new()
            {
                CaseId = Guid.NewGuid(),
                UserId = userId,
                Title = "Case 1",
                CaseNumber = "CASE-001",
                Status = Core.Enums.CaseStatus.New,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                CaseId = Guid.NewGuid(),
                UserId = userId,
                Title = "Case 2", 
                CaseNumber = "CASE-002",
                Status = Core.Enums.CaseStatus.InProgress,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        foreach (var doc in caseDocuments)
        {
            await _caseDocumentRepository.CreateAsync(doc);
        }

        // Act
        var userCases = await _caseDocumentRepository.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(userCases);
        Assert.Equal(2, userCases.Count);
        Assert.All(userCases, c => Assert.Equal(userId, c.UserId));

        // Cleanup
        foreach (var doc in caseDocuments)
        {
            await _caseDocumentRepository.DeleteAsync(doc.CaseId);
        }
        _sqlContext.Users.Remove(user);
        await _sqlContext.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchOperation_WithMultipleCriteria_ReturnsFilteredResults()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Create SQL user
        var user = new Core.Models.Entities.User
        {
            Id = userId,
            Email = "test3@example.com",
            FirstName = "Test",
            LastName = "User3",
            CreatedAt = DateTime.UtcNow
        };
        
        _sqlContext.Users.Add(user);
        await _sqlContext.SaveChangesAsync();

        // Create test cases with different characteristics
        var testCases = new List<CaseDocument>
        {
            new()
            {
                CaseId = Guid.NewGuid(),
                UserId = userId,
                Title = "Criminal Case - Theft",
                Description = "Case involving theft charges",
                CaseNumber = "CRIM-001",
                Status = Core.Enums.CaseStatus.New,
                Tags = new List<string> { "criminal", "theft" },
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                CaseId = Guid.NewGuid(),
                UserId = userId,
                Title = "Civil Case - Contract",
                Description = "Contract dispute case",
                CaseNumber = "CIVIL-001",
                Status = Core.Enums.CaseStatus.InProgress,
                Tags = new List<string> { "civil", "contract" },
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        foreach (var doc in testCases)
        {
            await _caseDocumentRepository.CreateAsync(doc);
        }

        // Act - Search for criminal cases
        var searchCriteria = new CaseSearchCriteria
        {
            UserId = userId,
            SearchText = "criminal",
            Tags = new List<string> { "criminal" }
        };

        var results = await _caseDocumentRepository.SearchAsync(searchCriteria);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Criminal Case - Theft", results[0].Title);

        // Cleanup
        foreach (var doc in testCases)
        {
            await _caseDocumentRepository.DeleteAsync(doc.CaseId);
        }
        _sqlContext.Users.Remove(user);
        await _sqlContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CaseAnalysis_Integration_StoresAnalysisInNoSQL()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var caseId = Guid.NewGuid();

        // Create SQL user
        var user = new Core.Models.Entities.User
        {
            Id = userId,
            Email = "test4@example.com",
            FirstName = "Test",
            LastName = "User4",
            CreatedAt = DateTime.UtcNow
        };
        
        _sqlContext.Users.Add(user);
        await _sqlContext.SaveChangesAsync();

        // Create case document with analysis
        var caseDocument = new CaseDocument
        {
            CaseId = caseId,
            UserId = userId,
            Title = "Analysis Test Case",
            CaseNumber = "ANALYSIS-001",
            Status = Core.Enums.CaseStatus.New,
            CreatedAt = DateTime.UtcNow,
            Analyses = new List<Core.Models.NoSQL.CaseAnalysis>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Status = Core.Enums.AnalysisStatus.Completed,
                    ViabilityScore = 75.5,
                    ConfidenceScore = 85.0,
                    AnalysisText = "This case shows strong potential for success.",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var createdDocument = await _caseDocumentRepository.CreateAsync(caseDocument);

        // Assert
        Assert.NotNull(createdDocument);
        Assert.NotNull(createdDocument.Analyses);
        Assert.Single(createdDocument.Analyses);
        Assert.Equal(75.5, createdDocument.Analyses[0].ViabilityScore);

        // Verify analysis stats
        var stats = await _caseDocumentRepository.GetAnalysisStatsAsync(caseId);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.TotalAnalyses);
        Assert.Equal(1, stats.CompletedAnalyses);

        // Cleanup
        await _caseDocumentRepository.DeleteAsync(caseId);
        _sqlContext.Users.Remove(user);
        await _sqlContext.SaveChangesAsync();
    }
}

public class TestDatabaseFixture : IDisposable
{
    public ServiceProvider Services { get; private set; }
    private readonly string _databaseName = $"test_db_{Guid.NewGuid()}";

    public TestDatabaseFixture()
    {
        var services = new ServiceCollection();
        
        // Configure SQL Server for testing
        services.AddDbContext<BetterCallSaulContext>(options =>
            options.UseInMemoryDatabase(_databaseName));

        // Configure NoSQL for testing
        services.Configure<NoSqlOptions>(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DatabaseName = "test_integration";
        });

        services.AddScoped<NoSqlContext>();
        services.AddScoped<ICaseDocumentRepository, BetterCallSaul.Infrastructure.Repositories.NoSQL.CaseDocumentRepository>();
        services.AddScoped<ILegalResearchRepository, BetterCallSaul.Infrastructure.Repositories.NoSQL.LegalResearchRepository>();

        Services = services.BuildServiceProvider();
    }

    public BetterCallSaulContext CreateContext()
    {
        return Services.GetRequiredService<BetterCallSaulContext>();
    }

    public NoSqlContext CreateNoSqlContext()
    {
        return Services.GetRequiredService<NoSqlContext>();
    }

    public void Dispose()
    {
        Services?.Dispose();
    }
}