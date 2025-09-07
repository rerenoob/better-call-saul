using BetterCallSaul.Core.Models.Entities;
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
    private readonly Mock<Core.Interfaces.Services.IAzureOpenAIService> _openAIServiceMock;
    private readonly Mock<ILogger<CaseAnalysisService>> _loggerMock;
    private readonly CaseAnalysisService _caseAnalysisService;

    public CaseAnalysisServiceTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<BetterCallSaulContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new BetterCallSaulContext(options);
        _openAIServiceMock = new Mock<Core.Interfaces.Services.IAzureOpenAIService>();
        _loggerMock = new Mock<ILogger<CaseAnalysisService>>();
        
        _caseAnalysisService = new CaseAnalysisService(
            _context,
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

        // Act
        var result = await _caseAnalysisService.AnalyzeCaseAsync(caseId, documentId, documentText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(caseId, result.CaseId);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(AnalysisStatus.Processing, result.Status);
        
        // Verify the record was saved to the database
        var savedAnalysis = await _context.CaseAnalyses.FirstOrDefaultAsync(ca => ca.Id == result.Id);
        Assert.NotNull(savedAnalysis);
        Assert.Equal(caseId, savedAnalysis.CaseId);
        Assert.Equal(documentId, savedAnalysis.DocumentId);
        Assert.Equal(AnalysisStatus.Processing, savedAnalysis.Status);
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