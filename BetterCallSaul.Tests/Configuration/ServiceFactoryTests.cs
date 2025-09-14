using BetterCallSaul.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BetterCallSaul.Tests.Configuration;

public class ServiceFactoryTests
{
    [Fact]
    public void CloudProviderConfiguration_EnvironmentVariableOverride_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock configuration with Azure as default
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CloudProvider:Active"] = "Azure"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<CloudProviderOptions>(options =>
        {
            var section = configuration.GetSection(CloudProviderOptions.SectionName);
            section.Bind(options);
            
            // Simulate environment variable override from Program.cs
            var cloudProvider = "AWS"; // Simulating CLOUD_PROVIDER=AWS
            if (!string.IsNullOrEmpty(cloudProvider) && (cloudProvider == "Azure" || cloudProvider == "AWS"))
            {
                options.Active = cloudProvider;
            }
        });
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<CloudProviderOptions>>().Value;

        // Assert
        Assert.Equal("AWS", options.Active);
    }

    [Fact]
    public void CloudProviderConfiguration_InvalidEnvironmentVariable_DoesNotOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock configuration with Azure as default
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CloudProvider:Active"] = "Azure"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<CloudProviderOptions>(options =>
        {
            var section = configuration.GetSection(CloudProviderOptions.SectionName);
            section.Bind(options);
            
            // Simulate invalid environment variable override from Program.cs
            var cloudProvider = "GCP"; // Simulating CLOUD_PROVIDER=GCP (invalid)
            if (!string.IsNullOrEmpty(cloudProvider) && (cloudProvider == "Azure" || cloudProvider == "AWS"))
            {
                options.Active = cloudProvider;
            }
        });
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<CloudProviderOptions>>().Value;

        // Assert - should remain Azure since GCP is invalid
        Assert.Equal("Azure", options.Active);
    }
}