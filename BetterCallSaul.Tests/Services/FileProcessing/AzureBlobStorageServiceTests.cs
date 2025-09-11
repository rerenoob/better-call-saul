using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.FileProcessing;

public class AzureBlobStorageServiceTests
{
    private readonly AzureBlobStorageOptions _options;
    private readonly Mock<ILogger<AzureBlobStorageService>> _loggerMock;
    private readonly AzureBlobStorageService _azureBlobStorageService;

    public AzureBlobStorageServiceTests()
    {
        _loggerMock = new Mock<ILogger<AzureBlobStorageService>>();

        _options = new AzureBlobStorageOptions
        {
            ConnectionString = "", // Empty connection string for testing
            ContainerName = "test-container",
            UseAzureStorage = false,
            SasTokenExpiryMinutes = 60,
            MaxRetries = 3,
            RetryDelayMilliseconds = 100
        };

        // Create service with empty connection string (will fail to initialize Azure client)
        _azureBlobStorageService = new AzureBlobStorageService(
            Options.Create(_options),
            _loggerMock.Object);
    }

    [Fact]
    public async Task ValidateFileAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.txt", "text/plain", 1024);

        // Act
        var result = await _azureBlobStorageService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidExtension_ReturnsFalse()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.exe", "application/octet-stream", 1024);

        // Act
        var result = await _azureBlobStorageService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateFileAsync_TooLargeFile_ReturnsFalse()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 200 * 1024 * 1024); // 200MB

        // Act
        var result = await _azureBlobStorageService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GenerateUniqueFileNameAsync_ValidInput_ReturnsUniqueName()
    {
        // Arrange
        var originalFileName = "legal_document.pdf";

        // Act
        var result = await _azureBlobStorageService.GenerateUniqueFileNameAsync(originalFileName);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("legal_document_", result);
        Assert.EndsWith(".pdf", result);
        Assert.Contains(DateTime.UtcNow.ToString("yyyyMMdd"), result);
    }

    [Fact]
    public async Task GetTotalUploadSizeForUserAsync_Always_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var timeWindow = TimeSpan.FromHours(1);

        // Act
        var result = await _azureBlobStorageService.GetTotalUploadSizeForUserAsync(userId, timeWindow);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task UploadFileAsync_WithEmptyConnectionString_ReturnsErrorResult()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 1024);

        // Act
        var result = await _azureBlobStorageService.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);

        // Assert - Should fail due to empty connection string
        Assert.False(result.Success);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task DeleteFileAsync_WithEmptyConnectionString_ReturnsFalse()
    {
        // Arrange
        var blobUrl = "https://testaccount.blob.core.windows.net/test-container/test-file.pdf";

        // Act
        var result = await _azureBlobStorageService.DeleteFileAsync(blobUrl);

        // Assert - Should fail due to empty connection string
        Assert.False(result);
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
}