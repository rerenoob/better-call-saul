using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Models.ServiceResponses;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace BetterCallSaul.Tests.Services.FileProcessing;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly LocalFileStorageService _service;
    private readonly string _testBasePath;
    private readonly Mock<ILogger<LocalFileStorageService>> _loggerMock;
    private readonly Mock<IOptions<LocalStorageOptions>> _optionsMock;

    public LocalFileStorageServiceTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "BetterCallSaulTest", Guid.NewGuid().ToString("N"));
        
        _loggerMock = new Mock<ILogger<LocalFileStorageService>>();
        _optionsMock = new Mock<IOptions<LocalStorageOptions>>();
        
        var localStorageOptions = new LocalStorageOptions { BasePath = _testBasePath };
        
        _optionsMock.Setup(o => o.Value).Returns(localStorageOptions);
        
        _service = new LocalFileStorageService(_optionsMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testBasePath))
            {
                Directory.Delete(_testBasePath, true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public void LocalFileStorageService_Implements_IStorageService()
    {
        Assert.IsAssignableFrom<IStorageService>(_service);
    }

    [Fact]
    public async Task UploadFileAsync_ValidFile_ReturnsSuccessResult()
    {
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Test file content")));

        var result = await _service.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);

        Assert.True(result.Success);
        Assert.Equal("File uploaded successfully to local storage", result.Message);
        Assert.Equal(1024, result.FileSize);
        Assert.Equal(".pdf", result.FileType);
        Assert.NotNull(result.StoragePath);
        Assert.True(File.Exists(result.StoragePath));
        Assert.Equal(uploadSessionId, result.UploadSessionId);
    }

    [Fact]
    public async Task UploadFileAsync_InvalidFileExtension_ReturnsValidationFailed()
    {
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.exe");
        fileMock.Setup(f => f.ContentType).Returns("application/octet-stream");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Test file content")));

        var result = await _service.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);

        Assert.False(result.Success);
        Assert.Equal("File validation failed", result.Message);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task UploadFileAsync_FileTooLarge_ReturnsValidationFailed()
    {
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(200 * 1024 * 1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(new string('A', 1024))));

        var result = await _service.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);

        Assert.False(result.Success);
        Assert.Equal("File validation failed", result.Message);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_ReturnsTrue()
    {
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Test file content")));

        var uploadResult = await _service.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);
        Assert.True(uploadResult.Success);
        Assert.True(File.Exists(uploadResult.StoragePath));

        var deleteResult = await _service.DeleteFileAsync(uploadResult.StoragePath!);

        Assert.True(deleteResult);
        Assert.False(File.Exists(uploadResult.StoragePath));
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistentFile_ReturnsFalse()
    {
        var nonExistentPath = Path.Combine(_testBasePath, "nonexistent", "file.txt");

        var result = await _service.DeleteFileAsync(nonExistentPath);

        Assert.False(result);
    }

    [Fact]
    public async Task GenerateSecureUrlAsync_ExistingFile_ReturnsFilePath()
    {
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Test file content")));

        var uploadResult = await _service.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);
        Assert.True(uploadResult.Success);

        var url = await _service.GenerateSecureUrlAsync(uploadResult.StoragePath!, TimeSpan.FromHours(1));

        Assert.Equal(uploadResult.StoragePath, url);
    }

    [Fact]
    public async Task GenerateSecureUrlAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(_testBasePath, "nonexistent", "file.txt");

        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _service.GenerateSecureUrlAsync(nonExistentPath, TimeSpan.FromHours(1)));
    }

    [Fact]
    public async Task GenerateUniqueFileNameAsync_ValidFileName_ReturnsUniqueName()
    {
        var originalFileName = "document.pdf";

        var result1 = await _service.GenerateUniqueFileNameAsync(originalFileName);
        var result2 = await _service.GenerateUniqueFileNameAsync(originalFileName);

        Assert.NotEqual(result1, result2);
        Assert.EndsWith(".pdf", result1);
        Assert.EndsWith(".pdf", result2);
        Assert.Contains("document_", result1);
        Assert.Contains("document_", result2);
    }

    [Fact]
    public async Task ValidateFileAsync_ValidFile_ReturnsTrue()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);

        var result = await _service.ValidateFileAsync(fileMock.Object);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidExtension_ReturnsFalse()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.exe");
        fileMock.Setup(f => f.ContentType).Returns("application/octet-stream");
        fileMock.Setup(f => f.Length).Returns(1024);

        var result = await _service.ValidateFileAsync(fileMock.Object);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateFileAsync_TooLargeFile_ReturnsFalse()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(200 * 1024 * 1024);

        var result = await _service.ValidateFileAsync(fileMock.Object);

        Assert.False(result);
    }

    [Fact]
    public async Task UploadFileAsync_CreatesProperDirectoryStructure()
    {
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Test file content")));

        var result = await _service.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);

        Assert.True(result.Success);
        Assert.NotNull(result.StoragePath);
        
        Assert.Contains($"cases{Path.DirectorySeparatorChar}{caseId}", result.StoragePath);
        Assert.Contains($"documents{Path.DirectorySeparatorChar}{userId}", result.StoragePath);
        Assert.Contains("test_", result.StoragePath);
        Assert.EndsWith(".pdf", result.StoragePath);
    }

    [Fact]
    public async Task DeleteFileAsync_CleansUpEmptyDirectories()
    {
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Test file content")));

        var uploadResult = await _service.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);
        Assert.True(uploadResult.Success);
        
        var filePath = uploadResult.StoragePath!;
        var userDirectory = Path.GetDirectoryName(filePath);
        var caseDocumentsDirectory = Path.GetDirectoryName(userDirectory);
        var caseDirectory = Path.GetDirectoryName(caseDocumentsDirectory);

        var deleteResult = await _service.DeleteFileAsync(filePath);

        Assert.True(deleteResult);
        Assert.False(File.Exists(filePath));
        Assert.False(Directory.Exists(userDirectory));
        Assert.False(Directory.Exists(caseDocumentsDirectory));
        Assert.True(Directory.Exists(caseDirectory));
    }

    [Fact]
    public async Task GetFilesForCaseAsync_ReturnsFilesForCase()
    {
        var caseId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock1 = new Mock<IFormFile>();
        fileMock1.Setup(f => f.FileName).Returns("file1.pdf");
        fileMock1.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock1.Setup(f => f.Length).Returns(1024);
        fileMock1.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Test file content 1")));

        var fileMock2 = new Mock<IFormFile>();
        fileMock2.Setup(f => f.FileName).Returns("file2.pdf");
        fileMock2.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock2.Setup(f => f.Length).Returns(2048);
        fileMock2.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Test file content 2")));

        await _service.UploadFileAsync(fileMock1.Object, caseId, userId1, uploadSessionId);
        await _service.UploadFileAsync(fileMock2.Object, caseId, userId2, uploadSessionId);

        var files = await _service.GetFilesForCaseAsync(caseId);

        Assert.Equal(2, files.Count());
        Assert.All(files, file => Assert.Contains(caseId.ToString(), file));
    }
}