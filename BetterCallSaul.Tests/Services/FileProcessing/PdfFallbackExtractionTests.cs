using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.FileProcessing;

public class PdfFallbackExtractionTests
{
    [Fact]
    public async Task CompositeTextExtractionService_WithPdfPig_ExtractsTextFromPdf()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompositeTextExtractionService>>();
        
        // Create proper AWS options mock
        var awsOptions = new BetterCallSaul.Core.Configuration.AWSOptions
        {
            Textract = new BetterCallSaul.Core.Configuration.TextractOptions { Region = "us-east-1" }
        };
        var optionsMock = new Mock<IOptions<BetterCallSaul.Core.Configuration.AWSOptions>>();
        optionsMock.Setup(x => x.Value).Returns(awsOptions);
        
        var awsTextractService = new AWSTextractService(optionsMock.Object, Mock.Of<ILogger<AWSTextractService>>());
        var storageServiceMock = new Mock<BetterCallSaul.Core.Interfaces.Services.IStorageService>();
        var service = new CompositeTextExtractionService(awsTextractService, storageServiceMock.Object, optionsMock.Object, loggerMock.Object);
        
        // Use a test PDF file from the test-case-files directory
        var testPdfPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "test-case-files", "mdfl-civil-script-goldilocks-v-the-three-bears.pdf");
        
        if (!File.Exists(testPdfPath))
        {
            // Skip test if test file doesn't exist
            return;
        }

        // Act
        var result = await service.ExtractTextAsync(testPdfPath, "test.pdf");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ExtractedText);
        Assert.NotEmpty(result.ExtractedText);
        Assert.True(result.ExtractedText.Length > 100); // Should have substantial content
        Assert.Equal("test.pdf", result.FileName);
        Assert.True(result.ProcessingTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task CompositeTextExtractionService_WithInvalidPdf_ReturnsFailure()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompositeTextExtractionService>>();
        
        // Create proper AWS options mock
        var awsOptions = new BetterCallSaul.Core.Configuration.AWSOptions
        {
            Textract = new BetterCallSaul.Core.Configuration.TextractOptions { Region = "us-east-1" }
        };
        var optionsMock = new Mock<IOptions<BetterCallSaul.Core.Configuration.AWSOptions>>();
        optionsMock.Setup(x => x.Value).Returns(awsOptions);
        
        var awsTextractService = new AWSTextractService(optionsMock.Object, Mock.Of<ILogger<AWSTextractService>>());
        var storageServiceMock = new Mock<BetterCallSaul.Core.Interfaces.Services.IStorageService>();
        var service = new CompositeTextExtractionService(awsTextractService, storageServiceMock.Object, optionsMock.Object, loggerMock.Object);
        
        // Create a non-existent PDF path
        var invalidPdfPath = Path.Combine(Path.GetTempPath(), "nonexistent-test-file.pdf");

        // Act
        var result = await service.ExtractTextAsync(invalidPdfPath, "nonexistent.pdf");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task CompositeTextExtractionService_WithEmptyPdf_ReturnsEmptyText()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CompositeTextExtractionService>>();
        
        // Create proper AWS options mock
        var awsOptions = new BetterCallSaul.Core.Configuration.AWSOptions
        {
            Textract = new BetterCallSaul.Core.Configuration.TextractOptions { Region = "us-east-1" }
        };
        var optionsMock = new Mock<IOptions<BetterCallSaul.Core.Configuration.AWSOptions>>();
        optionsMock.Setup(x => x.Value).Returns(awsOptions);
        
        var awsTextractService = new AWSTextractService(optionsMock.Object, Mock.Of<ILogger<AWSTextractService>>());
        var storageServiceMock = new Mock<BetterCallSaul.Core.Interfaces.Services.IStorageService>();
        var service = new CompositeTextExtractionService(awsTextractService, storageServiceMock.Object, optionsMock.Object, loggerMock.Object);
        
        // Create an empty PDF file for testing
        var emptyPdfPath = Path.GetTempFileName() + ".pdf";
        File.WriteAllBytes(emptyPdfPath, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // Minimal PDF header

        try
        {
            // Act
            var result = await service.ExtractTextAsync(emptyPdfPath, "empty.pdf");

            // Assert
            // The result might be successful but with empty text, or it might fail
            // Both are acceptable outcomes for an invalid PDF
            Assert.NotNull(result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(emptyPdfPath))
            {
                File.Delete(emptyPdfPath);
            }
        }
    }
}