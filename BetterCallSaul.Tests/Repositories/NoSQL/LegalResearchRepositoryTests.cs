using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Core.Models.NoSQL;
using BetterCallSaul.Infrastructure.Repositories.NoSQL;
using BetterCallSaul.Infrastructure.Data.NoSQL;
using BetterCallSaul.Core.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Repositories.NoSQL;

public class LegalResearchRepositoryTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<LegalResearchDocument>> _mockCollection;
    private readonly Mock<IOptions<NoSqlOptions>> _mockOptions;
    private readonly LegalResearchRepository _repository;

    public LegalResearchRepositoryTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<LegalResearchDocument>>();
        _mockOptions = new Mock<IOptions<NoSqlOptions>>();

        var mockClient = new Mock<IMongoClient>();
        mockClient.Setup(c => c.GetDatabase(It.IsAny<string>(), null))
            .Returns(_mockDatabase.Object);

        _mockDatabase.Setup(d => d.GetCollection<LegalResearchDocument>(It.IsAny<string>(), null))
            .Returns(_mockCollection.Object);

        _mockOptions.Setup(o => o.Value)
            .Returns(new NoSqlOptions
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "test_db"
            });

        var context = new NoSqlContext(_mockOptions.Object);
        _repository = new LegalResearchRepository(context);
    }

    [Fact]
    public async Task SearchTextAsync_WithQuery_ReturnsMatchingDocuments()
    {
        // Arrange
        var query = "search query";
        var expectedDocuments = new List<LegalResearchDocument>
        {
            new() { Id = "1", Title = "Document 1", Content = "Contains search query" },
            new() { Id = "2", Title = "Document 2", Content = "Also contains search query" }
        };

        var mockCursor = new Mock<IAsyncCursor<LegalResearchDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(expectedDocuments);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LegalResearchDocument>>(),
            It.IsAny<FindOptions<LegalResearchDocument, LegalResearchDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.SearchTextAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FindSimilarCasesAsync_WithCaseText_ReturnsSimilarDocuments()
    {
        // Arrange
        var caseText = "This is a criminal case involving theft";
        var expectedDocuments = new List<LegalResearchDocument>
        {
            new() { Id = "1", Title = "Similar Case 1", SimilarityScore = 0.85 },
            new() { Id = "2", Title = "Similar Case 2", SimilarityScore = 0.78 }
        };

        var mockCursor = new Mock<IAsyncCursor<LegalResearchDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(expectedDocuments);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LegalResearchDocument>>(),
            It.IsAny<FindOptions<LegalResearchDocument, LegalResearchDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.FindSimilarCasesAsync(caseText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByCitationAsync_ExistingCitation_ReturnsDocument()
    {
        // Arrange
        var citation = "123 U.S. 456";
        var expectedDocument = new LegalResearchDocument
        {
            Id = "1",
            Citation = citation,
            Title = "Test Case"
        };

        var mockCursor = new Mock<IAsyncCursor<LegalResearchDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(new List<LegalResearchDocument> { expectedDocument });

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LegalResearchDocument>>(),
            It.IsAny<FindOptions<LegalResearchDocument, LegalResearchDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.GetByCitationAsync(citation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(citation, result.Citation);
    }

    [Fact]
    public async Task CreateAsync_ValidDocument_ReturnsCreatedDocument()
    {
        // Arrange
        var document = new LegalResearchDocument
        {
            Id = "1",
            Title = "New Research Document",
            Citation = "123 U.S. 456"
        };

        _mockCollection.Setup(c => c.InsertOneAsync(
            document,
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _repository.CreateAsync(document);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(document.Id, result.Id);
        _mockCollection.Verify(c => c.InsertOneAsync(
            document,
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkIndexAsync_MultipleDocuments_InsertsAll()
    {
        // Arrange
        var documents = new List<LegalResearchDocument>
        {
            new() { Id = "1", Title = "Doc 1" },
            new() { Id = "2", Title = "Doc 2" },
            new() { Id = "3", Title = "Doc 3" }
        };

        _mockCollection.Setup(c => c.InsertManyAsync(
            documents,
            It.IsAny<InsertManyOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.BulkIndexAsync(documents);

        // Assert
        _mockCollection.Verify(c => c.InsertManyAsync(
            documents,
            It.IsAny<InsertManyOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByJurisdictionAsync_ValidJurisdiction_ReturnsDocuments()
    {
        // Arrange
        var jurisdiction = "federal";
        var expectedDocuments = new List<LegalResearchDocument>
        {
            new() { Id = "1", Jurisdiction = jurisdiction },
            new() { Id = "2", Jurisdiction = jurisdiction }
        };

        var mockCursor = new Mock<IAsyncCursor<LegalResearchDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(expectedDocuments);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LegalResearchDocument>>(),
            It.IsAny<FindOptions<LegalResearchDocument, LegalResearchDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.GetByJurisdictionAsync(jurisdiction);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(jurisdiction, d.Jurisdiction));
    }

    [Fact]
    public async Task SearchAdvancedAsync_ComplexQuery_ReturnsMatchingDocuments()
    {
        // Arrange
        var query = new LegalSearchQuery
        {
            FullTextQuery = "search term",
            Jurisdiction = "federal",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2023, 12, 31)
        };

        var expectedDocuments = new List<LegalResearchDocument>
        {
            new() { Id = "1", Title = "Matching Document 1" },
            new() { Id = "2", Title = "Matching Document 2" }
        };

        var mockCursor = new Mock<IAsyncCursor<LegalResearchDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(expectedDocuments);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<LegalResearchDocument>>(),
            It.IsAny<FindOptions<LegalResearchDocument, LegalResearchDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.SearchAdvancedAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CountAsync_ReturnsTotalCount()
    {
        // Arrange
        var expectedCount = 10L;

        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<LegalResearchDocument>>(),
            It.IsAny<CountOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        Assert.Equal(expectedCount, result);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsStatistics()
    {
        // Arrange
        var expectedStats = new Dictionary<string, long>
        {
            ["total"] = 100,
            ["federal"] = 60,
            ["state"] = 40
        };

        // Mock aggregation pipeline results
        var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        
        var bsonDocs = new List<BsonDocument>
        {
            BsonDocument.Parse("{ '_id': 'total', 'count': 100 }"),
            BsonDocument.Parse("{ '_id': 'federal', 'count': 60 }"),
            BsonDocument.Parse("{ '_id': 'state', 'count': 40 }")
        };
        mockCursor.Setup(c => c.Current).Returns(bsonDocs);

        _mockCollection.Setup(c => c.AggregateAsync(
            It.IsAny<PipelineDefinition<LegalResearchDocument, BsonDocument>>(),
            It.IsAny<AggregateOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.GetStatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }
}