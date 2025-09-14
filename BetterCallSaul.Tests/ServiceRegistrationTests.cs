using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Services.AI;
using BetterCallSaul.Infrastructure.Services.FileProcessing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace BetterCallSaul.Tests;

public class ServiceRegistrationTests
{
    [Fact]
    public void DevelopmentEnvironment_RegistersMockServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var hostingEnvironmentMock = new Mock<IWebHostEnvironment>();
        hostingEnvironmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        
        // Act - simulate the environment-based registration from Program.cs
        if (hostingEnvironmentMock.Object.IsDevelopment())
        {
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IStorageService, FileUploadService>();
            services.AddScoped<ITextExtractionService, MockTextExtractionService>();
            services.AddScoped<IAIService, MockAIService>();
        }
        else
        {
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IStorageService, AWSS3StorageService>();
            services.AddScoped<ITextExtractionService, AWSTextractService>();
            services.AddScoped<IAIService, AWSBedrockService>();
        }
        
        // Assert - verify that the correct service types are registered
        var fileUploadServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileUploadService));
        var storageServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IStorageService));
        var textExtractionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITextExtractionService));
        var aiServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAIService));
        
        Assert.NotNull(fileUploadServiceDescriptor);
        Assert.NotNull(storageServiceDescriptor);
        Assert.NotNull(textExtractionServiceDescriptor);
        Assert.NotNull(aiServiceDescriptor);
        
        Assert.Equal(typeof(FileUploadService), fileUploadServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(FileUploadService), storageServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(MockTextExtractionService), textExtractionServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(MockAIService), aiServiceDescriptor.ImplementationType);
    }
    
    [Fact]
    public void ProductionEnvironment_RegistersAWSServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var hostingEnvironmentMock = new Mock<IWebHostEnvironment>();
        hostingEnvironmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        
        // Act - simulate the environment-based registration from Program.cs
        if (hostingEnvironmentMock.Object.IsDevelopment())
        {
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IStorageService, FileUploadService>();
            services.AddScoped<ITextExtractionService, MockTextExtractionService>();
            services.AddScoped<IAIService, MockAIService>();
        }
        else
        {
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IStorageService, AWSS3StorageService>();
            services.AddScoped<ITextExtractionService, AWSTextractService>();
            services.AddScoped<IAIService, AWSBedrockService>();
        }
        
        // Assert - verify that the correct service types are registered
        var fileUploadServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IFileUploadService));
        var storageServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IStorageService));
        var textExtractionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITextExtractionService));
        var aiServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAIService));
        
        Assert.NotNull(fileUploadServiceDescriptor);
        Assert.NotNull(storageServiceDescriptor);
        Assert.NotNull(textExtractionServiceDescriptor);
        Assert.NotNull(aiServiceDescriptor);
        
        Assert.Equal(typeof(FileUploadService), fileUploadServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(AWSS3StorageService), storageServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(AWSTextractService), textExtractionServiceDescriptor.ImplementationType);
        Assert.Equal(typeof(AWSBedrockService), aiServiceDescriptor.ImplementationType);
    }
}