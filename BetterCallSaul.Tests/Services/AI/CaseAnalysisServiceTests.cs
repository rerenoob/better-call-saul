using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.AI;

public class CaseAnalysisServiceTests
{
    private readonly Mock<BetterCallSaul.Infrastructure.Data.BetterCallSaulContext> _contextMock;
    private readonly Mock<Core.Interfaces.Services.IAzureOpenAIService> _openAIServiceMock;
    private readonly Mock<ILogger<CaseAnalysisService>> _loggerMock;
    private readonly CaseAnalysisService _caseAnalysisService;

    public CaseAnalysisServiceTests()
    {
        _contextMock = new Mock<BetterCallSaul.Infrastructure.Data.BetterCallSaulContext>();
        _openAIServiceMock = new Mock<Core.Interfaces.Services.IAzureOpenAIService>();
        _loggerMock = new Mock<ILogger<CaseAnalysisService>>();
        
        _caseAnalysisService = new CaseAnalysisService(
            _contextMock.Object,
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
        
        var mockDbSet = new Mock<DbSet<CaseAnalysis>>();
        _contextMock.Setup(c => c.CaseAnalyses).Returns(mockDbSet.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _caseAnalysisService.AnalyzeCaseAsync(caseId, documentId, documentText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(caseId, result.CaseId);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(AnalysisStatus.Processing, result.Status);
        
        mockDbSet.Verify(d => d.Add(It.Is<CaseAnalysis>(a => 
            a.CaseId == caseId && 
            a.DocumentId == documentId && 
            a.Status == AnalysisStatus.Processing)), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAnalysisAsync_ExistingAnalysis_ReturnsAnalysis()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        var expectedAnalysis = new CaseAnalysis { Id = analysisId, CaseId = Guid.NewGuid() };
        
        var mockDbSet = new Mock<DbSet<CaseAnalysis>>();
        mockDbSet.Setup(d => d.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CaseAnalysis, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAnalysis);
        
        _contextMock.Setup(c => c.CaseAnalyses).Returns(mockDbSet.Object);

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
        
        var mockDbSet = new Mock<DbSet<CaseAnalysis>>();
        mockDbSet.Setup(d => d.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CaseAnalysis, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CaseAnalysis?)null);
        
        _contextMock.Setup(c => c.CaseAnalyses).Returns(mockDbSet.Object);

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
        
        var mockDbSet = new Mock<DbSet<CaseAnalysis>>();
        mockDbSet.As<IQueryable<CaseAnalysis>>().Setup(m => m.Provider).Returns(analyses.AsQueryable().Provider);
        mockDbSet.As<IQueryable<CaseAnalysis>>().Setup(m => m.Expression).Returns(analyses.AsQueryable().Expression);
        mockDbSet.As<IQueryable<CaseAnalysis>>().Setup(m => m.ElementType).Returns(analyses.AsQueryable().ElementType);
        mockDbSet.As<IQueryable<CaseAnalysis>>().Setup(m => m.GetEnumerator()).Returns(analyses.GetEnumerator());
        
        _contextMock.Setup(c => c.CaseAnalyses).Returns(mockDbSet.Object);

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
        var emptyList = new List<CaseAnalysis>();
        
        var mockDbSet = new Mock<DbSet<CaseAnalysis>>();
        mockDbSet.As<IQueryable<CaseAnalysis>>().Setup(m => m.Provider).Returns(emptyList.AsQueryable().Provider);
        mockDbSet.As<IQueryable<CaseAnalysis>>().Setup(m => m.Expression).Returns(emptyList.AsQueryable().Expression);
        mockDbSet.As<IQueryable<CaseAnalysis>>().Setup(m => m.ElementType).Returns(emptyList.AsQueryable().ElementType);
        mockDbSet.As<IQueryable<CaseAnalysis>>().Setup(m => m.GetEnumerator()).Returns(emptyList.GetEnumerator());
        
        _contextMock.Setup(c => c.CaseAnalyses).Returns(mockDbSet.Object);

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
        
        var mockDbSet = new Mock<DbSet<CaseAnalysis>>();
        mockDbSet.Setup(d => d.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CaseAnalysis, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);
        
        _contextMock.Setup(c => c.CaseAnalyses).Returns(mockDbSet.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _caseAnalysisService.UpdateAnalysisStatusAsync(analysisId, AnalysisStatus.Completed, "Analysis completed successfully");

        // Assert
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAnalysisStatusAsync_NonExistentAnalysis_ThrowsKeyNotFoundException()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        
        var mockDbSet = new Mock<DbSet<CaseAnalysis>>();
        mockDbSet.Setup(d => d.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CaseAnalysis, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CaseAnalysis?)null);
        
        _contextMock.Setup(c => c.CaseAnalyses).Returns(mockDbSet.Object);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _caseAnalysisService.UpdateAnalysisStatusAsync(analysisId, AnalysisStatus.Completed));
    }

    // [Fact]
    // public void AnalysisProgress_Event_RaisesCorrectly()
    // {
    //     // This test is commented out because AnalysisProgressEventArgs is not defined
    //     // in the current implementation. This would need to be implemented when
    //     // the event system is fully developed.
    // }
}