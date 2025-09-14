using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Models.ServiceResponses;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.FileProcessing;

public class AWSS3StorageServiceTests
{
    private readonly Mock<ILogger<AWSS3StorageService>> _loggerMock;
    private readonly AWSS3StorageService _awsS3StorageService;

    public AWSS3StorageServiceTests()
    {
        _loggerMock = new Mock<ILogger<AWSS3StorageService>>();

        var cloudProviderOptions = new CloudProviderOptions
        {
            AWS = new AWSOptions
            {
                S3 = new S3Options
                {
                    BucketName = "", // Empty for testing unconfigured state
                    Region = ""
                }
            }
        };

        _awsS3StorageService = new AWSS3StorageService(
            Options.Create(cloudProviderOptions),
            _loggerMock.Object);
    }

    [Fact]
    public void AWSS3StorageService_Implements_IStorageService()
    {
        // Assert
        Assert.IsAssignableFrom<IStorageService>(_awsS3StorageService);
    }

    [Fact]
    public async Task UploadFileAsync_AWSNotConfigured_ReturnsNotConfiguredError()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 1024);

        // Act
        var result = await _awsS3StorageService.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("AWS S3 is not configured", result.Message);
        Assert.Equal("AWS_NOT_CONFIGURED", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteFileAsync_AWSNotConfigured_ReturnsFalse()
    {
        // Arrange
        var storagePath = "cases/123/documents/456/test.pdf";

        // Act
        var result = await _awsS3StorageService.DeleteFileAsync(storagePath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GenerateSecureUrlAsync_AWSNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var storagePath = "cases/123/documents/456/test.pdf";
        var expiryTime = TimeSpan.FromMinutes(60);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _awsS3StorageService.GenerateSecureUrlAsync(storagePath, expiryTime));
    }

    [Fact]
    public async Task ValidateFileAsync_ValidPdf_ReturnsTrue()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 1024);

        // Act
        var result = await _awsS3StorageService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidExtension_ReturnsFalse()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.exe", "application/octet-stream", 1024);

        // Act
        var result = await _awsS3StorageService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateFileAsync_FileTooLarge_ReturnsFalse()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 200 * 1024 * 1024); // 200MB

        // Act
        var result = await _awsS3StorageService.ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GenerateUniqueFileNameAsync_ValidName_ReturnsUniqueName()
    {
        // Arrange
        var originalFileName = "legal_document.pdf";

        // Act
        var result = await _awsS3StorageService.GenerateUniqueFileNameAsync(originalFileName);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("legal_document_", result);
        Assert.EndsWith(".pdf", result);
        Assert.Contains(DateTime.UtcNow.ToString("yyyyMMdd"), result);
    }

    [Fact]
    public async Task Integration_AllMethods_ReturnConsistentResults()
    {
        // This test verifies that all IStorageService methods work together consistently
        
        // Arrange
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 1024);

        // Act - Test all methods
        var validateResult = await _awsS3StorageService.ValidateFileAsync(fileMock.Object);
        var fileNameResult = await _awsS3StorageService.GenerateUniqueFileNameAsync("test.pdf");
        var uploadResult = await _awsS3StorageService.UploadFileAsync(fileMock.Object, Guid.NewGuid(), Guid.NewGuid(), "test-session");
        var deleteResult = await _awsS3StorageService.DeleteFileAsync("test/path.pdf");

        // Assert - All should behave consistently for unconfigured AWS
        Assert.True(validateResult); // File validation should pass
        Assert.NotNull(fileNameResult); // Filename generation should work
        Assert.False(uploadResult.Success); // Upload should fail due to unconfigured AWS
        Assert.False(deleteResult); // Delete should fail due to unconfigured AWS
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