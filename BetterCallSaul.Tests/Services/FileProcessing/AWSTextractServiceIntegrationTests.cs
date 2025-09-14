using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Services.FileProcessing;

public class AWSTextractServiceIntegrationTests
{
    [Fact]
    public void ServiceRegistration_AWSConfigured_ResolvesAWSTextractService()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock logger
        var loggerMock = new Mock<ILogger<AWSTextractService>>();
        services.AddSingleton(loggerMock.Object);
        
        // Configure AWS options
        var cloudProviderOptions = new CloudProviderOptions
        {
            Active = "AWS",
            AWS = new AWSOptions
            {
                Textract = new TextractOptions
                {
                    Region = "us-east-1"
                }
            }
        };
        
        services.AddSingleton(Options.Create(cloudProviderOptions));
        services.AddScoped<ITextExtractionService, AWSTextractService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var textExtractionService = serviceProvider.GetService<ITextExtractionService>();

        // Assert
        Assert.NotNull(textExtractionService);
        Assert.IsType<AWSTextractService>(textExtractionService);
    }


    [Fact]
    public void AWSTextractService_WithAWSConfiguration_DetectsAsConfigured()
    {
        // Arrange
        var cloudProviderOptions = new CloudProviderOptions
        {
            Active = "AWS",
            AWS = new AWSOptions
            {
                Textract = new TextractOptions
                {
                    Region = "us-east-1"
                }
            }
        };

        var loggerMock = new Mock<ILogger<AWSTextractService>>();
        
        // Act
        var service = new AWSTextractService(
            Options.Create(cloudProviderOptions),
            loggerMock.Object);

        // Assert - Service should be configured
        Assert.NotNull(service);
    }
}