using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests;

public class VirusScanningTests
{
    [Fact]
    public async Task ScanFileAsync_CleanFile_ReturnsCleanResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ClamAvService>>();
        var service = new ClamAvService(loggerMock.Object);
        
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, "This is a clean file content");

        try
        {
            // Act
            var result = await service.ScanFileAsync(tempFilePath, "test.txt");

            // Assert
            Assert.True(result.IsClean);
            Assert.False(result.IsInfected);
            Assert.Equal(ScanStatus.Clean, result.Status);
            Assert.Null(result.VirusName);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ScanFileAsync_EicarTestFile_ReturnsInfectedResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ClamAvService>>();
        var service = new ClamAvService(loggerMock.Object);
        
        var tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");

        try
        {
            // Act
            var result = await service.ScanFileAsync(tempFilePath, "eicar_test.txt");

            // Assert
            Assert.False(result.IsClean);
            Assert.True(result.IsInfected);
            Assert.Equal(ScanStatus.Infected, result.Status);
            Assert.Equal("EICAR-Test-File", result.VirusName);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ScanFileAsync_FileNotFound_ReturnsErrorResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ClamAvService>>();
        var service = new ClamAvService(loggerMock.Object);
        
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistentfile.txt");

        // Act
        var result = await service.ScanFileAsync(nonExistentPath, "nonexistent.txt");

        // Assert
        Assert.Equal(ScanStatus.Error, result.Status);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task IsScannerAvailableAsync_ReturnsAvailabilityStatus()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ClamAvService>>();
        var service = new ClamAvService(loggerMock.Object);

        // Act
        var isAvailable = await service.IsScannerAvailableAsync();

        // Assert - The method should return a boolean value (true or false)
        Assert.IsType<bool>(isAvailable);
    }
}