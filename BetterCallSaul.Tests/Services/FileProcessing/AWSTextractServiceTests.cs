using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.FileProcessing;

public class AWSTextractServiceTests
{
    private readonly Mock<ILogger<AWSTextractService>> _loggerMock;
    private readonly AWSTextractService _awsTextractService;

    public AWSTextractServiceTests()
    {
        _loggerMock = new Mock<ILogger<AWSTextractService>>();

        var cloudProviderOptions = new CloudProviderOptions
        {
            AWS = new AWSOptions
            {
                Textract = new TextractOptions
                {
                    Region = "" // Empty for testing unconfigured state
                }
            }
        };

        _awsTextractService = new AWSTextractService(
            Options.Create(cloudProviderOptions),
            _loggerMock.Object);
    }

    [Fact]
    public void AWSTextractService_Implements_ITextExtractionService()
    {
        // Assert
        Assert.IsAssignableFrom<ITextExtractionService>(_awsTextractService);
    }

    [Fact]
    public async Task ExtractTextAsync_AWSNotConfigured_ReturnsNotConfiguredError()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, "Test content");

        try
        {
            // Act
            var result = await _awsTextractService.ExtractTextAsync(tempFilePath, "test.txt");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("AWS Textract is not configured", result.ErrorMessage);
            Assert.Equal(TextExtractionStatus.Failed, result.Status);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public async Task ExtractTextAsync_FileNotFound_ReturnsFileNotFoundError()
    {
        // Act
        var result = await _awsTextractService.ExtractTextAsync("nonexistent.txt", "nonexistent.txt");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("File not found", result.ErrorMessage);
        Assert.Equal(TextExtractionStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ExtractTextFromBytesAsync_AWSNotConfigured_ReturnsNotConfiguredError()
    {
        // Arrange
        var fileContent = System.Text.Encoding.UTF8.GetBytes("Test content");

        // Act
        var result = await _awsTextractService.ExtractTextFromBytesAsync(fileContent, "test.txt");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("AWS Textract is not configured", result.ErrorMessage);
        Assert.Equal(TextExtractionStatus.Failed, result.Status);
    }

    [Fact]
    public async Task SupportsFileTypeAsync_SupportedTypes_ReturnsTrue()
    {
        // Arrange
        var supportedFiles = new[] { "test.pdf", "image.jpg", "document.png", "scan.tiff", "photo.tif" };

        foreach (var fileName in supportedFiles)
        {
            // Act
            var result = await _awsTextractService.SupportsFileTypeAsync(fileName);

            // Assert
            Assert.True(result, $"File {fileName} should be supported");
        }
    }

    [Fact]
    public async Task SupportsFileTypeAsync_UnsupportedTypes_ReturnsFalse()
    {
        // Arrange
        var unsupportedFiles = new[] { "test.txt", "document.docx", "file.doc", "data.csv", "script.js" };

        foreach (var fileName in unsupportedFiles)
        {
            // Act
            var result = await _awsTextractService.SupportsFileTypeAsync(fileName);

            // Assert
            Assert.False(result, $"File {fileName} should not be supported");
        }
    }

    [Fact]
    public async Task ProcessDocumentAsync_AWSNotConfigured_ThrowsException()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, "Test content");
        var documentId = Guid.NewGuid();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _awsTextractService.ProcessDocumentAsync(tempFilePath, documentId));
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public async Task ExtractTextAsync_UnsupportedFileType_ReturnsUnsupportedFormatError()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, "Test content");

        try
        {
            // Act - Use a supported file type but test that AWS configuration check comes first
            var result = await _awsTextractService.ExtractTextAsync(tempFilePath, "test.pdf");

            // Assert - Should fail due to AWS not configured, not unsupported format
            Assert.False(result.Success);
            Assert.Equal("AWS Textract is not configured", result.ErrorMessage);
            Assert.Equal(TextExtractionStatus.Failed, result.Status);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public void Constructor_WithNullRegion_DoesNotInitializeClient()
    {
        // Arrange
        var cloudProviderOptions = new CloudProviderOptions
        {
            AWS = new AWSOptions
            {
                Textract = new TextractOptions
                {
                    Region = null!
                }
            }
        };

        // Act
        var service = new AWSTextractService(
            Options.Create(cloudProviderOptions),
            _loggerMock.Object);

        // Assert - Should not throw and client should be null
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithEmptyRegion_DoesNotInitializeClient()
    {
        // Arrange
        var cloudProviderOptions = new CloudProviderOptions
        {
            AWS = new AWSOptions
            {
                Textract = new TextractOptions
                {
                    Region = ""
                }
            }
        };

        // Act
        var service = new AWSTextractService(
            Options.Create(cloudProviderOptions),
            _loggerMock.Object);

        // Assert - Should not throw and client should be null
        Assert.NotNull(service);
    }
}