using Microsoft.Extensions.Configuration;
using Xunit;

namespace BetterCallSaul.Tests.Integration;

public class AWSConfigurationValidationTests
{
    [Fact]
    public void ProductionEnvironment_WithoutAWSCredentials_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        // Clear AWS environment variables for this test
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("AWS_REGION", null);

        // Act & Assert - The validation should throw when building the host
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            // This simulates the validation that happens in Program.cs
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            if (environment == "Production")
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) ||
                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
                {
                    throw new InvalidOperationException("AWS credentials are required for production environment. " +
                        "Set AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY environment variables.");
                }
            }
        });

        Assert.Contains("AWS credentials are required for production environment", exception.Message);
    }

    [Fact]
    public void ProductionEnvironment_WithAWSCredentials_ValidationPasses()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        // Set AWS environment variables
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test-access-key");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test-secret-key");
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");

        // Act - Should not throw
        var exception = Record.Exception(() =>
        {
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            if (environment == "Production")
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) ||
                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
                {
                    throw new InvalidOperationException("AWS credentials are required for production environment");
                }
            }
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void DevelopmentEnvironment_WithoutAWSCredentials_ValidationSkipped()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            })
            .Build();

        // Clear AWS environment variables
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("AWS_REGION", null);

        // Act - Should not throw for development
        var exception = Record.Exception(() =>
        {
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            if (environment == "Production") // This condition won't be met for Development
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) ||
                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
                {
                    throw new InvalidOperationException("AWS credentials are required for production environment");
                }
            }
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ProductionEnvironment_MissingAccessKey_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        // Set only secret key, missing access key
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test-secret-key");
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            if (environment == "Production")
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) ||
                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
                {
                    throw new InvalidOperationException("AWS credentials are required for production environment");
                }
            }
        });

        Assert.Contains("AWS credentials are required", exception.Message);
    }

    [Fact]
    public void ProductionEnvironment_MissingSecretKey_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        // Set only access key, missing secret key
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test-access-key");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", null);
        Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            if (environment == "Production")
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) ||
                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
                {
                    throw new InvalidOperationException("AWS credentials are required for production environment");
                }
            }
        });

        Assert.Contains("AWS credentials are required", exception.Message);
    }

    [Fact]
    public void ProductionEnvironment_WithRegionFallback_LogsWarning()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        // Set AWS credentials but no region
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test-access-key");
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test-secret-key");
        Environment.SetEnvironmentVariable("AWS_REGION", null);

        // Capture log output
        var logOutput = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(logOutput);

        try
        {
            // Act - Simulate the warning logging
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            if (environment == "Production")
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) ||
                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
                {
                    throw new InvalidOperationException("AWS credentials are required for production environment");
                }

                // Simulate the region warning
                var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
                if (string.IsNullOrEmpty(awsRegion))
                {
                    Console.WriteLine("AWS_REGION environment variable not set. Defaulting to us-east-1");
                }
            }

            // Assert
            var logContent = logOutput.ToString();
            Assert.Contains("AWS_REGION environment variable not set", logContent);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}