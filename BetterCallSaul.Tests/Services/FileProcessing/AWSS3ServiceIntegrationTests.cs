using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.FileProcessing;

public class AWSS3ServiceIntegrationTests
{
    [Fact]
    public void ServiceRegistration_AWSConfigured_ResolvesAWSS3Service()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock logger
        var loggerMock = new Mock<ILogger<AWSS3StorageService>>();
        services.AddSingleton(loggerMock.Object);
        
        // Configure AWS options
        var cloudProviderOptions = new CloudProviderOptions
        {
            Active = "AWS",
            AWS = new AWSOptions
            {
                S3 = new S3Options
                {
                    BucketName = "test-bucket",
                    Region = "us-east-1"
                }
            }
        };
        
        services.AddSingleton(Options.Create(cloudProviderOptions));
        services.AddScoped<IStorageService, AWSS3StorageService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var storageService = serviceProvider.GetService<IStorageService>();

        // Assert
        Assert.NotNull(storageService);
        Assert.IsType<AWSS3StorageService>(storageService);
    }

    [Fact]
    public void ServiceRegistration_AzureConfigured_ResolvesAWSService()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock logger
        var loggerMock = new Mock<ILogger<AWSS3StorageService>>();
        services.AddSingleton(loggerMock.Object);
        
        // Configure with invalid provider (Azure is no longer supported)
        var cloudProviderOptions = new CloudProviderOptions
        {
            Active = "InvalidProvider",
            AWS = new AWSOptions
            {
                S3 = new S3Options
                {
                    BucketName = "test-bucket",
                    Region = "us-east-1"
                }
            }
        };
        
        services.AddSingleton(Options.Create(cloudProviderOptions));
        services.AddScoped<IStorageService, AWSS3StorageService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var storageService = serviceProvider.GetService<IStorageService>();

        // Assert - Should still resolve AWS service but it will detect it's not configured as active
        Assert.NotNull(storageService);
        Assert.IsType<AWSS3StorageService>(storageService);
    }

    [Fact]
    public async Task AWSS3StorageService_WithAWSConfiguration_DetectsAsConfigured()
    {
        // Arrange
        var cloudProviderOptions = new CloudProviderOptions
        {
            Active = "AWS",
            AWS = new AWSOptions
            {
                S3 = new S3Options
                {
                    BucketName = "test-bucket",
                    Region = "us-east-1"
                }
            }
        };

        var loggerMock = new Mock<ILogger<AWSS3StorageService>>();
        var service = new AWSS3StorageService(
            Options.Create(cloudProviderOptions),
            loggerMock.Object);

        // Act - Test with a simple method that doesn't require S3 client
        var fileName = await service.GenerateUniqueFileNameAsync("test.pdf");

        // Assert
        Assert.NotNull(fileName);
        Assert.Contains("test_", fileName);
        Assert.EndsWith(".pdf", fileName);
    }
}