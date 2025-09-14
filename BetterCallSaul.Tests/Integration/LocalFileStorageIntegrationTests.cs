using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace BetterCallSaul.Tests.Integration;

public class LocalFileStorageIntegrationTests
{
    [Fact]
    public async Task LocalFileStorageService_Integration_Test()
    {
        var testBasePath = Path.Combine(Path.GetTempPath(), "BetterCallSaulIntegrationTest", Guid.NewGuid().ToString("N"));
        
        var loggerMock = new Mock<ILogger<LocalFileStorageService>>();
        var optionsMock = new Mock<IOptions<LocalStorageOptions>>();
        
        var localStorageOptions = new LocalStorageOptions { BasePath = testBasePath };
        
        optionsMock.Setup(o => o.Value).Returns(localStorageOptions);
        
        var service = new LocalFileStorageService(optionsMock.Object, loggerMock.Object);
        
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var uploadSessionId = "test-session-123";
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Test file content")));
        
        var uploadResult = await service.UploadFileAsync(fileMock.Object, caseId, userId, uploadSessionId);
        
        Assert.True(uploadResult.Success);
        Assert.NotNull(uploadResult.StoragePath);
        Assert.Contains(testBasePath, uploadResult.StoragePath);
        Assert.Contains(caseId.ToString(), uploadResult.StoragePath);
        Assert.Contains(userId.ToString(), uploadResult.StoragePath);
        
        Assert.True(File.Exists(uploadResult.StoragePath));
        
        var secureUrl = await service.GenerateSecureUrlAsync(uploadResult.StoragePath!, TimeSpan.FromHours(1));
        Assert.Equal(uploadResult.StoragePath, secureUrl);
        
        var deleteResult = await service.DeleteFileAsync(uploadResult.StoragePath!);
        Assert.True(deleteResult);
        
        Assert.False(File.Exists(uploadResult.StoragePath));
        
        try
        {
            if (Directory.Exists(testBasePath))
            {
                Directory.Delete(testBasePath, true);
            }
        }
        catch
        {
        }
    }
    
    [Fact]
    public async Task LocalFileStorageService_File_Operations_Work_End_To_End()
    {
        var testBasePath = Path.Combine(Path.GetTempPath(), "BetterCallSaulIntegrationTest", Guid.NewGuid().ToString("N"));
        
        var loggerMock = new Mock<ILogger<LocalFileStorageService>>();
        var optionsMock = new Mock<IOptions<LocalStorageOptions>>();
        
        var localStorageOptions = new LocalStorageOptions { BasePath = testBasePath };
        
        optionsMock.Setup(o => o.Value).Returns(localStorageOptions);
        
        var service = new LocalFileStorageService(optionsMock.Object, loggerMock.Object);
        
        var caseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        for (int i = 0; i < 3; i++)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns($"test{i}.pdf");
            fileMock.Setup(f => f.ContentType).Returns("application/pdf");
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes($"Test file content {i}")));
            
            var uploadResult = await service.UploadFileAsync(fileMock.Object, caseId, userId, $"session-{i}");
            Assert.True(uploadResult.Success);
            Assert.True(File.Exists(uploadResult.StoragePath));
        }
        
        var files = await service.GetFilesForCaseAsync(caseId);
        Assert.Equal(3, files.Count());
        
        foreach (var file in files)
        {
            var deleteResult = await service.DeleteFileAsync(file);
            Assert.True(deleteResult);
            Assert.False(File.Exists(file));
        }
        
        files = await service.GetFilesForCaseAsync(caseId);
        Assert.Empty(files);
        
        try
        {
            if (Directory.Exists(testBasePath))
            {
                Directory.Delete(testBasePath, true);
            }
        }
        catch
        {
        }
    }
}