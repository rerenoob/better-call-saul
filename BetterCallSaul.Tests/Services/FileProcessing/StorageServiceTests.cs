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

public class StorageServiceTests
{
    private readonly Mock<ILogger<AzureBlobStorageService>> _azureLoggerMock;
    private readonly Mock<ILogger<FileUploadService>> _fileUploadLoggerMock;
    private readonly AzureBlobStorageService _azureStorageService;
    private readonly FileUploadService _fileUploadService;

    public StorageServiceTests()
    {
        _azureLoggerMock = new Mock<ILogger<AzureBlobStorageService>>();
        _fileUploadLoggerMock = new Mock<ILogger<FileUploadService>>();

        var azureOptions = new AzureBlobStorageOptions
        {
            ConnectionString = "", // Empty connection string for testing
            ContainerName = "test-container",
            UseAzureStorage = false,
            SasTokenExpiryMinutes = 60,
            MaxRetries = 3,
            RetryDelayMilliseconds = 100
        };

        _azureStorageService = new AzureBlobStorageService(
            Options.Create(azureOptions),
            _azureLoggerMock.Object);

        // For FileUploadService, we need to mock the dependencies
        var contextMock = new Mock<BetterCallSaul.Infrastructure.Data.BetterCallSaulContext>();
        var fileValidationServiceMock = new Mock<BetterCallSaul.Infrastructure.Services.FileProcessing.IFileValidationService>();
        var textExtractionServiceMock = new Mock<BetterCallSaul.Infrastructure.Services.FileProcessing.ITextExtractionService>();

        _fileUploadService = new FileUploadService(
            contextMock.Object,
            fileValidationServiceMock.Object,
            textExtractionServiceMock.Object,
            _fileUploadLoggerMock.Object);
    }

    [Fact]
    public void AzureBlobStorageService_Implements_IStorageService()
    {
        // Assert
        Assert.IsAssignableFrom<IStorageService>(_azureStorageService);
    }

    [Fact]
    public void FileUploadService_Implements_IStorageService()
    {
        // Assert
        Assert.IsAssignableFrom<IStorageService>(_fileUploadService);
    }

    [Fact]
    public async Task IStorageService_UploadFileAsync_Returns_StorageResult()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 1024);

        // Act - Test with AzureBlobStorageService
        var azureResult = await ((IStorageService)_azureStorageService).UploadFileAsync(
            fileMock.Object, caseId, userId, uploadSessionId);

        // Assert
        Assert.IsType<StorageResult>(azureResult);
        Assert.False(azureResult.Success); // Should fail due to empty connection string
        Assert.NotNull(azureResult.Message);
    }

    [Fact]
    public async Task IStorageService_ValidateFileAsync_Works_With_Both_Services()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.txt", "text/plain", 1024);

        // Act
        var azureResult = await ((IStorageService)_azureStorageService).ValidateFileAsync(fileMock.Object);
        var fileUploadResult = await ((IStorageService)_fileUploadService).ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.True(azureResult);
        Assert.True(fileUploadResult);
    }

    [Fact]
    public async Task IStorageService_GenerateUniqueFileNameAsync_Works_With_Both_Services()
    {
        // Arrange
        var originalFileName = "legal_document.pdf";

        // Act
        var azureResult = await ((IStorageService)_azureStorageService).GenerateUniqueFileNameAsync(originalFileName);
        var fileUploadResult = await ((IStorageService)_fileUploadService).GenerateUniqueFileNameAsync(originalFileName);

        // Assert
        Assert.NotNull(azureResult);
        Assert.NotNull(fileUploadResult);
        Assert.StartsWith("legal_document_", azureResult);
        Assert.StartsWith("legal_document_", fileUploadResult);
        Assert.EndsWith(".pdf", azureResult);
        Assert.EndsWith(".pdf", fileUploadResult);
    }

    [Fact]
    public async Task IStorageService_DeleteFileAsync_Works_With_Both_Services()
    {
        // Arrange
        var filePath = "/some/test/path/file.pdf";

        // Act
        var azureResult = await ((IStorageService)_azureStorageService).DeleteFileAsync(filePath);
        var fileUploadResult = await ((IStorageService)_fileUploadService).DeleteFileAsync(filePath);

        // Assert
        Assert.False(azureResult); // Should fail due to empty connection string
        Assert.False(fileUploadResult); // Should fail because file doesn't exist
    }

    [Fact]
    public async Task IStorageService_GenerateSecureUrlAsync_Works_With_AzureService()
    {
        // Arrange
        var storagePath = "https://testaccount.blob.core.windows.net/test-container/test-file.pdf";
        var expiryTime = TimeSpan.FromMinutes(60);

        // Act & Assert - Should throw due to empty connection string
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((IStorageService)_azureStorageService).GenerateSecureUrlAsync(storagePath, expiryTime));
    }

    [Fact]
    public async Task IStorageService_GenerateSecureUrlAsync_Works_With_FileUploadService()
    {
        // Arrange
        var storagePath = "/some/test/path/file.pdf";
        var expiryTime = TimeSpan.FromMinutes(60);

        // Act
        var result = await ((IStorageService)_fileUploadService).GenerateSecureUrlAsync(storagePath, expiryTime);

        // Assert - FileUploadService should return the same path for local storage
        Assert.Equal(storagePath, result);
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