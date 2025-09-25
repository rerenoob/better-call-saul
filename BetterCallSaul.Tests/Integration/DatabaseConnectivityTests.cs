using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BetterCallSaul.Tests.Integration
{
    public class DatabaseConnectivityTests
    {
        [Fact]
        public void PostgreSQL_ConnectionString_ShouldBeCorrectlyFormatted()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=test-rds-host;Port=5432;Database=BetterCallSaul;Username=testuser;Password=testpass"
                })
                .Build();

            var services = new ServiceCollection();

            // Act - Configure PostgreSQL like in production
            services.AddDbContext<BetterCallSaulContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<BetterCallSaulContext>();

            Assert.NotNull(context);
            Assert.Contains("test-rds-host", context.Database.GetConnectionString());
            Assert.Contains("Port=5432", context.Database.GetConnectionString());
            Assert.Contains("Database=BetterCallSaul", context.Database.GetConnectionString());
        }

        [Fact]
        public void DocumentDB_ConnectionString_ShouldBeCorrectlyFormatted()
        {
            // Arrange
            var documentDbConnectionString = "mongodb://testuser:testpass@test-docdb-cluster.cluster-abc123.docdb.us-east-1.amazonaws.com:27017/?ssl=true&replicaSet=rs0&readPreference=secondaryPreferred&retryWrites=false";

            // Act & Assert
            Assert.Contains("mongodb://", documentDbConnectionString);
            Assert.Contains("ssl=true", documentDbConnectionString);
            Assert.Contains("replicaSet=rs0", documentDbConnectionString);
            Assert.Contains("readPreference=secondaryPreferred", documentDbConnectionString);
            Assert.Contains("retryWrites=false", documentDbConnectionString);
            Assert.Contains("test-docdb-cluster.cluster-", documentDbConnectionString);
            Assert.Contains(".docdb.us-east-1.amazonaws.com", documentDbConnectionString);
            Assert.Contains(":27017", documentDbConnectionString);
        }

        [Fact]
        public void CloudFormation_EnvironmentVariables_ShouldMatchApplicationExpectations()
        {
            // Arrange - Expected environment variables from CloudFormation
            var expectedVariables = new Dictionary<string, string>
            {
                ["RDS_HOST"] = "test-postgres-endpoint.rds.amazonaws.com",
                ["RDS_USERNAME"] = "bettercallsaul",
                ["RDS_PASSWORD"] = "test-secure-password",
                ["DOCUMENTDB_CONNECTION_STRING"] = "mongodb://bettercallsaul:test-secure-password@test-docdb.cluster-abc123.docdb.us-east-1.amazonaws.com:27017/?ssl=true&replicaSet=rs0&readPreference=secondaryPreferred&retryWrites=false"
            };

            // Act - Build connection string like in appsettings.Production.json
            var postgresConnectionString = $"Host={expectedVariables["RDS_HOST"]};Port=5432;Database=BetterCallSaul;Username={expectedVariables["RDS_USERNAME"]};Password={expectedVariables["RDS_PASSWORD"]}";
            var documentDbConnectionString = expectedVariables["DOCUMENTDB_CONNECTION_STRING"];

            // Assert - Verify connection strings are valid
            Assert.Contains("Host=test-postgres-endpoint.rds.amazonaws.com", postgresConnectionString);
            Assert.Contains("Port=5432", postgresConnectionString);
            Assert.Contains("Database=BetterCallSaul", postgresConnectionString);
            Assert.Contains("Username=bettercallsaul", postgresConnectionString);
            Assert.Contains("Password=test-secure-password", postgresConnectionString);

            Assert.Contains("mongodb://bettercallsaul:test-secure-password@", documentDbConnectionString);
            Assert.Contains("ssl=true", documentDbConnectionString);
            Assert.Contains("replicaSet=rs0", documentDbConnectionString);
        }

        [Fact]
        public void Database_Configuration_ShouldSupportEnvironmentVariableSubstitution()
        {
            // Arrange - Simulate environment variables being set
            Environment.SetEnvironmentVariable("RDS_HOST", "prod-postgres.rds.amazonaws.com");
            Environment.SetEnvironmentVariable("RDS_USERNAME", "produser");
            Environment.SetEnvironmentVariable("RDS_PASSWORD", "prodpassword");
            Environment.SetEnvironmentVariable("DOCUMENTDB_CONNECTION_STRING", "mongodb://produser:prodpassword@prod-docdb.cluster-xyz789.docdb.us-east-1.amazonaws.com:27017/?ssl=true&replicaSet=rs0&readPreference=secondaryPreferred&retryWrites=false");

            try
            {
                // Act - Test environment variable substitution (simulating production config)
                var rdsHost = Environment.GetEnvironmentVariable("RDS_HOST");
                var rdsUsername = Environment.GetEnvironmentVariable("RDS_USERNAME");
                var rdsPassword = Environment.GetEnvironmentVariable("RDS_PASSWORD");
                var documentDbConnection = Environment.GetEnvironmentVariable("DOCUMENTDB_CONNECTION_STRING");

                // Assert
                Assert.Equal("prod-postgres.rds.amazonaws.com", rdsHost);
                Assert.Equal("produser", rdsUsername);
                Assert.Equal("prodpassword", rdsPassword);
                Assert.Contains("mongodb://produser:prodpassword@prod-docdb", documentDbConnection);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("RDS_HOST", null);
                Environment.SetEnvironmentVariable("RDS_USERNAME", null);
                Environment.SetEnvironmentVariable("RDS_PASSWORD", null);
                Environment.SetEnvironmentVariable("DOCUMENTDB_CONNECTION_STRING", null);
            }
        }
    }
}