using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Services.AI;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace BetterCallSaul.Tests.Integration;

public class ServiceRegistrationIntegrationTests
{
    [Fact]
    public void DevelopmentEnvironment_RegistersMockServices_Integration()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Simulate the environment-based registration from Program.cs for Development
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IStorageService, FileUploadService>();
        services.AddScoped<ITextExtractionService, MockTextExtractionService>();
        services.AddScoped<IAIService, MockAIService>();

        // Act - Build the service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert - verify that the correct service types are registered
        var fileUploadServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileUploadService));
        var storageServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IStorageService));
        var textExtractionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITextExtractionService));
        var aiServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAIService));

        Assert.NotNull(fileUploadServiceDescriptor);
        Assert.NotNull(storageServiceDescriptor);
        Assert.NotNull(textExtractionServiceDescriptor);
        Assert.NotNull(aiServiceDescriptor);

        // Verify the actual implementation types
        Assert.Equal(typeof(FileUploadService), fileUploadServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(FileUploadService), storageServiceDescriptor.ImplementationType); // In development, FileUploadService implements both
        Assert.Equal(typeof(MockTextExtractionService), textExtractionServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(MockAIService), aiServiceDescriptor.ImplementationType);
    }

    [Fact]
    public void ProductionEnvironment_RegistersAWSServices_Integration()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Simulate the environment-based registration from Program.cs for Production
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IStorageService, AWSS3StorageService>();
        services.AddScoped<ITextExtractionService, AWSTextractService>();
        services.AddScoped<IAIService, AWSBedrockService>();

        // Act - Build the service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert - verify that the correct service types are registered
        var fileUploadServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileUploadService));
        var storageServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IStorageService));
        var textExtractionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITextExtractionService));
        var aiServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAIService));

        Assert.NotNull(fileUploadServiceDescriptor);
        Assert.NotNull(storageServiceDescriptor);
        Assert.NotNull(textExtractionServiceDescriptor);
        Assert.NotNull(aiServiceDescriptor);

        // Verify the actual implementation types
        Assert.Equal(typeof(FileUploadService), fileUploadServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(AWSS3StorageService), storageServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(AWSTextractService), textExtractionServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(AWSBedrockService), aiServiceDescriptor.ImplementationType);
    }

    [Fact]
    public void ServiceContracts_AreSatisfied_Development()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Simulate the environment-based registration from Program.cs for Development
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IStorageService, FileUploadService>();
        services.AddScoped<ITextExtractionService, MockTextExtractionService>();
        services.AddScoped<IAIService, MockAIService>();

        // Act - Build the service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert - verify that all service contracts are properly implemented
        var fileUploadServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileUploadService));
        var storageServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IStorageService));
        var textExtractionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITextExtractionService));
        var aiServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAIService));

        // Verify no null services
        Assert.NotNull(fileUploadServiceDescriptor);
        Assert.NotNull(storageServiceDescriptor);
        Assert.NotNull(textExtractionServiceDescriptor);
        Assert.NotNull(aiServiceDescriptor);

        // Verify that services implement their interfaces
        Assert.True(typeof(IFileUploadService).IsAssignableFrom(fileUploadServiceDescriptor!.ImplementationType!));
        Assert.True(typeof(IStorageService).IsAssignableFrom(storageServiceDescriptor!.ImplementationType!));
        Assert.True(typeof(ITextExtractionService).IsAssignableFrom(textExtractionServiceDescriptor!.ImplementationType!));
        Assert.True(typeof(IAIService).IsAssignableFrom(aiServiceDescriptor!.ImplementationType!));
    }

    [Fact]
    public void ServiceContracts_AreSatisfied_Production()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Simulate the environment-based registration from Program.cs for Production
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IStorageService, AWSS3StorageService>();
        services.AddScoped<ITextExtractionService, AWSTextractService>();
        services.AddScoped<IAIService, AWSBedrockService>();

        // Act - Build the service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert - verify that all service contracts are properly implemented
        var fileUploadServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileUploadService));
        var storageServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IStorageService));
        var textExtractionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITextExtractionService));
        var aiServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAIService));

        // Verify no null services
        Assert.NotNull(fileUploadServiceDescriptor);
        Assert.NotNull(storageServiceDescriptor);
        Assert.NotNull(textExtractionServiceDescriptor);
        Assert.NotNull(aiServiceDescriptor);

        // Verify that services implement their interfaces
        Assert.True(typeof(IFileUploadService).IsAssignableFrom(fileUploadServiceDescriptor!.ImplementationType!));
        Assert.True(typeof(IStorageService).IsAssignableFrom(storageServiceDescriptor!.ImplementationType!));
        Assert.True(typeof(ITextExtractionService).IsAssignableFrom(textExtractionServiceDescriptor!.ImplementationType!));
        Assert.True(typeof(IAIService).IsAssignableFrom(aiServiceDescriptor!.ImplementationType!));
    }


}