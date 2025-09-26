using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Core.Models.ServiceResponses;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.FileProcessing;

public class StorageServiceTests
{
    private readonly Mock<ILogger<FileUploadService>> _fileUploadLoggerMock;
    private readonly FileUploadService _fileUploadService;

    public StorageServiceTests()
    {
        _fileUploadLoggerMock = new Mock<ILogger<FileUploadService>>();

        // For FileUploadService, we need to mock the dependencies
        var contextMock = new Mock<BetterCallSaul.Infrastructure.Data.BetterCallSaulContext>();
        var caseDocumentRepositoryMock = new Mock<ICaseDocumentRepository>();
        var fileValidationServiceMock = new Mock<BetterCallSaul.Infrastructure.Services.FileProcessing.IFileValidationService>();
        var textExtractionServiceMock = new Mock<BetterCallSaul.Infrastructure.Services.FileProcessing.ITextExtractionService>();

        // Mock storage service to avoid circular dependency in tests
        var storageServiceMock = new Mock<IStorageService>();
        storageServiceMock.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new StorageResult { Success = true, StoragePath = "test/path" });

        _fileUploadService = new FileUploadService(
            contextMock.Object,
            caseDocumentRepositoryMock.Object,
            fileValidationServiceMock.Object,
            textExtractionServiceMock.Object,
            storageServiceMock.Object,
            _fileUploadLoggerMock.Object);
    }

    [Fact]
    public void FileUploadService_Implements_IStorageService()
    {
        // Assert
        Assert.IsAssignableFrom<IStorageService>(_fileUploadService);
    }

    [Fact]
    public async Task IStorageService_ValidateFileAsync_Works_With_FileUploadService()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.txt", "text/plain", 1024);

        // Act
        var fileUploadResult = await ((IStorageService)_fileUploadService).ValidateFileAsync(fileMock.Object);

        // Assert
        Assert.True(fileUploadResult);
    }

    [Fact]
    public async Task IStorageService_GenerateUniqueFileNameAsync_Works_With_FileUploadService()
    {
        // Arrange
        var originalFileName = "legal_document.pdf";

        // Act
        var fileUploadResult = await ((IStorageService)_fileUploadService).GenerateUniqueFileNameAsync(originalFileName);

        // Assert
        Assert.NotNull(fileUploadResult);
        Assert.StartsWith("legal_document_", fileUploadResult);
        Assert.EndsWith(".pdf", fileUploadResult);
    }

    [Fact]
    public async Task IStorageService_DeleteFileAsync_Works_With_FileUploadService()
    {
        // Arrange
        var filePath = "/some/test/path/file.pdf";

        // Act
        var fileUploadResult = await ((IStorageService)_fileUploadService).DeleteFileAsync(filePath);

        // Assert
        Assert.False(fileUploadResult); // Should fail because file doesn't exist
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