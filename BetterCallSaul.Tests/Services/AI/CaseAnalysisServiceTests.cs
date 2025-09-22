using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Enums;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Infrastructure.Services.AI;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.AI;

public class CaseAnalysisServiceTests : IDisposable
{
    private readonly BetterCallSaulContext _context;
    private readonly Mock<ICaseDocumentRepository> _caseDocumentRepositoryMock;
    private readonly Mock<Core.Interfaces.Services.IAIService> _openAIServiceMock;
    private readonly Mock<ILogger<CaseAnalysisService>> _loggerMock;
    private readonly CaseAnalysisService _caseAnalysisService;

    public CaseAnalysisServiceTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<BetterCallSaulContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new BetterCallSaulContext(options);

        // Ensure database is created and ready
        _context.Database.EnsureCreated();

        _caseDocumentRepositoryMock = new Mock<ICaseDocumentRepository>();
        _openAIServiceMock = new Mock<Core.Interfaces.Services.IAIService>();
        _loggerMock = new Mock<ILogger<CaseAnalysisService>>();

        _caseAnalysisService = new CaseAnalysisService(
            _context,
            _caseDocumentRepositoryMock.Object,
            _openAIServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task AnalyzeCaseAsync_ValidInput_CreatesAnalysisRecord()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var documentText = "Test legal document content";
        
        // Mock successful AI response
        var aiResponse = new Core.Models.Entities.AIResponse
        {
            Success = true,
            GeneratedText = "Test analysis result",
            ConfidenceScore = 0.85,
            ProcessingTime = TimeSpan.FromSeconds(5)
        };
        
        _openAIServiceMock.Setup(s => s.GenerateLegalAnalysisAsync(documentText, "Case analysis", It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResponse);

        // Act
        var result = await _caseAnalysisService.AnalyzeCaseAsync(caseId, documentId, documentText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(caseId, result.CaseId);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(AnalysisStatus.Completed, result.Status);
        Assert.Equal("Test analysis result", result.AnalysisText);
        Assert.Equal(0.85, result.ConfidenceScore);
        
        // Verify the record was saved to the database
        var savedAnalysis = await _context.CaseAnalyses.FirstOrDefaultAsync(ca => ca.Id == result.Id);
        Assert.NotNull(savedAnalysis);
        Assert.Equal(caseId, savedAnalysis.CaseId);
        Assert.Equal(documentId, savedAnalysis.DocumentId);
        Assert.Equal(AnalysisStatus.Completed, savedAnalysis.Status);
        
        // Verify OpenAI service was called
        _openAIServiceMock.Verify(s => s.GenerateLegalAnalysisAsync(documentText, "Case analysis", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAnalysisAsync_ExistingAnalysis_ReturnsAnalysis()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        var expectedAnalysis = new CaseAnalysis { Id = analysisId, CaseId = Guid.NewGuid() };
        
        _context.CaseAnalyses.Add(expectedAnalysis);
        await _context.SaveChangesAsync();

        // Act
        var result = await _caseAnalysisService.GetAnalysisAsync(analysisId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(analysisId, result.Id);
        Assert.Equal(expectedAnalysis.CaseId, result.CaseId);
    }

    [Fact]
    public async Task GetAnalysisAsync_NonExistentAnalysis_ThrowsKeyNotFoundException()
    {
        // Arrange
        var analysisId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _caseAnalysisService.GetAnalysisAsync(analysisId));
    }

    [Fact]
    public async Task AnalyzeCaseAsync_OpenAIFailure_MarksAnalysisAsFailed()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var documentText = "Test legal document content";
        
        // Mock failed AI response
        var aiResponse = new Core.Models.Entities.AIResponse
        {
            Success = false,
            ErrorMessage = "AI service unavailable",
            ProcessingTime = TimeSpan.Zero
        };
        
        _openAIServiceMock.Setup(s => s.GenerateLegalAnalysisAsync(documentText, "Case analysis", It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResponse);

        // Act
        var result = await _caseAnalysisService.AnalyzeCaseAsync(caseId, documentId, documentText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(caseId, result.CaseId);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(AnalysisStatus.Failed, result.Status);
        Assert.Contains("AI service unavailable", result.AnalysisText);
        
        // Verify the record was saved to the database
        var savedAnalysis = await _context.CaseAnalyses.FirstOrDefaultAsync(ca => ca.Id == result.Id);
        Assert.NotNull(savedAnalysis);
        Assert.Equal(AnalysisStatus.Failed, savedAnalysis.Status);
        
        // Verify OpenAI service was called
        _openAIServiceMock.Verify(s => s.GenerateLegalAnalysisAsync(documentText, "Case analysis", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCaseAnalysesAsync_ValidCaseId_ReturnsAnalyses()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var analyses = new List<CaseAnalysis>
        {
            new CaseAnalysis { Id = Guid.NewGuid(), CaseId = caseId, CreatedAt = DateTime.Now.AddHours(-1) },
            new CaseAnalysis { Id = Guid.NewGuid(), CaseId = caseId, CreatedAt = DateTime.Now.AddHours(-2) }
        };
        
        _context.CaseAnalyses.AddRange(analyses);
        await _context.SaveChangesAsync();

        // Act
        var result = await _caseAnalysisService.GetCaseAnalysesAsync(caseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal(caseId, a.CaseId));
        // Should be ordered by CreatedAt descending
        Assert.True(result[0].CreatedAt > result[1].CreatedAt);
    }

    [Fact]
    public async Task GetCaseAnalysesAsync_NoAnalyses_ReturnsEmptyList()
    {
        // Arrange
        var caseId = Guid.NewGuid();

        // Act
        var result = await _caseAnalysisService.GetCaseAnalysesAsync(caseId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAnalysisStatusAsync_ValidAnalysis_UpdatesStatus()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        var analysis = new CaseAnalysis { Id = analysisId, Status = AnalysisStatus.Processing };
        
        _context.CaseAnalyses.Add(analysis);
        await _context.SaveChangesAsync();

        // Act
        await _caseAnalysisService.UpdateAnalysisStatusAsync(analysisId, AnalysisStatus.Completed, "Analysis completed successfully");

        // Assert
        var updatedAnalysis = await _context.CaseAnalyses.FirstAsync(a => a.Id == analysisId);
        Assert.Equal(AnalysisStatus.Completed, updatedAnalysis.Status);
    }

    [Fact]
    public async Task UpdateAnalysisStatusAsync_NonExistentAnalysis_ThrowsKeyNotFoundException()
    {
        // Arrange
        var analysisId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _caseAnalysisService.UpdateAnalysisStatusAsync(analysisId, AnalysisStatus.Completed));
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}