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

public class CaseDocumentRepositoryTests
{
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<CaseDocument>> _mockCollection;
    private readonly Mock<IOptions<NoSqlOptions>> _mockOptions;
    private readonly CaseDocumentRepository _repository;

    public CaseDocumentRepositoryTests()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<CaseDocument>>();
        _mockOptions = new Mock<IOptions<NoSqlOptions>>();

        var mockClient = new Mock<IMongoClient>();
        mockClient.Setup(c => c.GetDatabase(It.IsAny<string>(), null))
            .Returns(_mockDatabase.Object);

        _mockDatabase.Setup(d => d.GetCollection<CaseDocument>(It.IsAny<string>(), null))
            .Returns(_mockCollection.Object);

        _mockOptions.Setup(o => o.Value)
            .Returns(new NoSqlOptions
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "test_db"
            });

        var context = new NoSqlContext(_mockOptions.Object);
        _repository = new CaseDocumentRepository(context);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsCaseDocument()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var expectedDocument = new CaseDocument
        {
            CaseId = caseId,
            Title = "Test Case",
            UserId = Guid.NewGuid()
        };

        var mockCursor = new Mock<IAsyncCursor<CaseDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(new List<CaseDocument> { expectedDocument });

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<CaseDocument>>(),
            It.IsAny<FindOptions<CaseDocument, CaseDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.GetByIdAsync(caseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(caseId, result.CaseId);
        Assert.Equal("Test Case", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var caseId = Guid.NewGuid();

        var mockCursor = new Mock<IAsyncCursor<CaseDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(new List<CaseDocument>());

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<CaseDocument>>(),
            It.IsAny<FindOptions<CaseDocument, CaseDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.GetByIdAsync(caseId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ValidDocument_ReturnsCreatedDocument()
    {
        // Arrange
        var document = new CaseDocument
        {
            CaseId = Guid.NewGuid(),
            Title = "New Case",
            UserId = Guid.NewGuid()
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
        Assert.Equal(document.CaseId, result.CaseId);
        _mockCollection.Verify(c => c.InsertOneAsync(
            document,
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_ValidUserId_ReturnsUserCases()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedDocuments = new List<CaseDocument>
        {
            new() { CaseId = Guid.NewGuid(), UserId = userId, Title = "Case 1" },
            new() { CaseId = Guid.NewGuid(), UserId = userId, Title = "Case 2" }
        };

        var mockCursor = new Mock<IAsyncCursor<CaseDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(expectedDocuments);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<CaseDocument>>(),
            It.IsAny<FindOptions<CaseDocument, CaseDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(userId, d.UserId));
    }

    [Fact]
    public async Task SearchAsync_WithCriteria_ReturnsMatchingCases()
    {
        // Arrange
        var criteria = new CaseSearchCriteria
        {
            UserId = Guid.NewGuid(),
            SearchText = "test",
            Tags = new List<string> { "criminal", "felony" }
        };

        var expectedDocuments = new List<CaseDocument>
        {
            new() { CaseId = Guid.NewGuid(), UserId = criteria.UserId.Value, Title = "Test Case 1" },
            new() { CaseId = Guid.NewGuid(), UserId = criteria.UserId.Value, Title = "Test Case 2" }
        };

        var mockCursor = new Mock<IAsyncCursor<CaseDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(expectedDocuments);

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<CaseDocument>>(),
            It.IsAny<FindOptions<CaseDocument, CaseDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.SearchAsync(criteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(criteria.UserId.Value, d.UserId));
    }

    [Fact]
    public async Task ExistsAsync_ExistingCase_ReturnsTrue()
    {
        // Arrange
        var caseId = Guid.NewGuid();

        var mockCursor = new Mock<IAsyncCursor<CaseDocument>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(new List<CaseDocument> { new() { CaseId = caseId } });

        _mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<CaseDocument>>(),
            It.IsAny<FindOptions<CaseDocument, CaseDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _repository.ExistsAsync(caseId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CountAsync_ReturnsTotalCount()
    {
        // Arrange
        var expectedCount = 5L;

        _mockCollection.Setup(c => c.CountDocumentsAsync(
            It.IsAny<FilterDefinition<CaseDocument>>(),
            It.IsAny<CountOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        Assert.Equal(expectedCount, result);
    }
}