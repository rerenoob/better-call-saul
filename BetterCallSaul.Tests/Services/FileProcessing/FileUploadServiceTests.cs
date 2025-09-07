using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.FileProcessing;

public class FileUploadServiceTests
{
    private readonly Mock<BetterCallSaul.Infrastructure.Data.BetterCallSaulContext> _contextMock;
    private readonly Mock<BetterCallSaul.Infrastructure.Services.FileProcessing.IFileValidationService> _fileValidationServiceMock;
    private readonly Mock<ILogger<FileUploadService>> _loggerMock;
    private readonly FileUploadService _fileUploadService;

    public FileUploadServiceTests()
    {
        _contextMock = new Mock<BetterCallSaul.Infrastructure.Data.BetterCallSaulContext>();
        _fileValidationServiceMock = new Mock<BetterCallSaul.Infrastructure.Services.FileProcessing.IFileValidationService>();
        _loggerMock = new Mock<ILogger<FileUploadService>>();
        
        _fileUploadService = new FileUploadService(
            _contextMock.Object,
            _fileValidationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UploadFileAsync_ValidFile_ReturnsSuccessResult()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 1024);
        
        var validationResult = new BetterCallSaul.Infrastructure.Services.FileProcessing.ValidationResult { IsValid = true };
        _fileValidationServiceMock.Setup(s => s.ValidateFileAsync(fileMock.Object))
            .ReturnsAsync(validationResult);

        var mockDbSet = new Mock<DbSet<Document>>();
        _contextMock.Setup(c => c.Documents).Returns(mockDbSet.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Mock user upload size check
        _contextMock.Setup(c => c.Documents)
            .Returns(CreateMockDbSet(new List<Document>()));

        // Act
        var result = await _fileUploadService.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(uploadSessionId, result.UploadSessionId);
        Assert.NotEqual(Guid.Empty, result.FileId);
        Assert.Equal("File uploaded successfully", result.Message);
        Assert.Equal(1024, result.FileSize);
        
        mockDbSet.Verify(d => d.Add(It.Is<Document>(doc => 
            doc.CaseId == caseId && 
            doc.UploadedById == userId && 
            doc.FileSize == 1024)), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadFileAsync_InvalidFile_ReturnsValidationError()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 1024);
        
        var validationResult = new BetterCallSaul.Infrastructure.Services.FileProcessing.ValidationResult 
        { 
            IsValid = false, 
            ErrorMessage = "Virus detected",
            Status = BetterCallSaul.Infrastructure.Services.FileProcessing.FileValidationStatus.Infected
        };
        
        _fileValidationServiceMock.Setup(s => s.ValidateFileAsync(fileMock.Object))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _fileUploadService.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Virus detected", result.Message);
        Assert.Equal("Infected", result.ErrorCode);
        
        _contextMock.Verify(c => c.Documents, Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadFileAsync_UploadLimitExceeded_ReturnsError()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 600 * 1024 * 1024); // 600MB
        
        var validationResult = new BetterCallSaul.Infrastructure.Services.FileProcessing.ValidationResult { IsValid = true };
        _fileValidationServiceMock.Setup(s => s.ValidateFileAsync(fileMock.Object))
            .ReturnsAsync(validationResult);

        // Mock user upload size to be near the limit
        var recentUploads = new List<Document>
        {
            new Document { FileSize = 450 * 1024 * 1024, CreatedAt = DateTime.UtcNow.AddMinutes(-30) }
        };
        
        _contextMock.Setup(c => c.Documents)
            .Returns(CreateMockDbSet(recentUploads));

        // Act
        var result = await _fileUploadService.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Upload limit exceeded. Please try again later.", result.Message);
        
        _contextMock.Verify(c => c.Documents, Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateFileAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.txt", "text/plain", 1024);

        // Act
        var result = await _fileUploadService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GenerateUniqueFileNameAsync_ValidInput_ReturnsUniqueName()
    {
        // Arrange
        var originalFileName = "legal_document.pdf";

        // Act
        var result = await _fileUploadService.GenerateUniqueFileNameAsync(originalFileName);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("legal_document_", result);
        Assert.EndsWith(".pdf", result);
        Assert.Contains(DateTime.UtcNow.ToString("yyyyMMdd"), result);
    }

    [Fact]
    public async Task StoreFileAsync_ValidFile_ReturnsFilePath()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.txt", "text/plain", 1024);
        var fileName = "test_unique_name.txt";

        // Act
        var result = await _fileUploadService.StoreFileAsync(fileMock.Object, fileName);

        // Assert
        Assert.NotNull(result);
        Assert.EndsWith(fileName, result);
        Assert.True(File.Exists(result));
        
        // Cleanup
        File.Delete(result);
    }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "test content");

        // Act
        var result = await _fileUploadService.DeleteFileAsync(tempFile);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent.txt");

        // Act
        var result = await _fileUploadService.DeleteFileAsync(nonExistentFile);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetTotalUploadSizeForUserAsync_ValidUser_ReturnsTotalSize()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var timeWindow = TimeSpan.FromHours(1);
        
        var documents = new List<Document>
        {
            new Document { UploadedById = userId, FileSize = 1000, CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
            new Document { UploadedById = userId, FileSize = 2000, CreatedAt = DateTime.UtcNow.AddMinutes(-45) }
        };
        
        _contextMock.Setup(c => c.Documents)
            .Returns(CreateMockDbSet(documents));

        // Act
        var result = await _fileUploadService.GetTotalUploadSizeForUserAsync(userId, timeWindow);

        // Assert
        Assert.Equal(3000, result);
    }

    [Fact]
    public async Task GetTotalUploadSizeForUserAsync_NoUploads_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var timeWindow = TimeSpan.FromHours(1);
        
        _contextMock.Setup(c => c.Documents)
            .Returns(CreateMockDbSet(new List<Document>()));

        // Act
        var result = await _fileUploadService.GetTotalUploadSizeForUserAsync(userId, timeWindow);

        // Assert
        Assert.Equal(0, result);
    }

    private Mock<IFormFile> CreateMockFormFile(string fileName, string contentType, long length)
    {
        var fileMock = new Mock<IFormFile>();
        var content = "Hello World from a Fake File";
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;

        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(length);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream s, CancellationToken ct) => stream.CopyToAsync(s, ct));

        return fileMock;
    }

    private DbSet<T> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();
        
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        
        return mockSet.Object;
    }
}