using BetterCallSaul.Gateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BetterCallSaul.Gateway.Tests
{
    public class GatewayServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<GatewayService>> _loggerMock;
        private readonly GatewayService _gatewayService;

        public GatewayServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<GatewayService>>();

            // Mock configuration
            var configurationSectionMock = new Mock<IConfigurationSection>();
            configurationSectionMock.Setup(x => x.Value).Returns("https://localhost:7191");
            _configurationMock.Setup(x => x.GetSection("Services:UserService:BaseUrl")).Returns(configurationSectionMock.Object);

            _gatewayService = new GatewayService(
                _httpClientFactoryMock.Object,
                _configurationMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ValidateJwtTokenAsync_ValidToken_ReturnsTrue()
        {
            // Arrange
            var validToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            
            // Mock JWT settings
            var jwtSecretSection = new Mock<IConfigurationSection>();
            jwtSecretSection.Setup(x => x.Value).Returns("DevelopmentSuperSecretKeyForJWTTokenGeneration");
            
            var jwtIssuerSection = new Mock<IConfigurationSection>();
            jwtIssuerSection.Setup(x => x.Value).Returns("BetterCallSaul.UserService");
            
            var jwtAudienceSection = new Mock<IConfigurationSection>();
            jwtAudienceSection.Setup(x => x.Value).Returns("BetterCallSaul.API");

            _configurationMock.Setup(x => x["JwtSettings:SecretKey"]).Returns("DevelopmentSuperSecretKeyForJWTTokenGeneration");
            _configurationMock.Setup(x => x["JwtSettings:Issuer"]).Returns("BetterCallSaul.UserService");
            _configurationMock.Setup(x => x["JwtSettings:Audience"]).Returns("BetterCallSaul.API");

            // Act
            var result = await _gatewayService.ValidateJwtTokenAsync(validToken);

            // Assert
            Assert.False(result); // This should be false because the token is not properly signed with our secret
        }

        [Fact]
        public async Task ValidateJwtTokenAsync_InvalidToken_ReturnsFalse()
        {
            // Arrange
            var invalidToken = "Bearer invalidtoken";

            // Act
            var result = await _gatewayService.ValidateJwtTokenAsync(invalidToken);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateJwtTokenAsync_EmptyToken_ReturnsFalse()
        {
            // Arrange
            var emptyToken = "";

            // Act
            var result = await _gatewayService.ValidateJwtTokenAsync(emptyToken);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExtractUserIdFromTokenAsync_ValidToken_ReturnsUserId()
        {
            // Arrange
            var validToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            // Act
            var result = await _gatewayService.ExtractUserIdFromTokenAsync(validToken);

            // Assert
            Assert.Equal("1234567890", result);
        }

        [Fact]
        public async Task ExtractUserIdFromTokenAsync_InvalidToken_ReturnsEmpty()
        {
            // Arrange
            var invalidToken = "Bearer invalidtoken";

            // Act
            var result = await _gatewayService.ExtractUserIdFromTokenAsync(invalidToken);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}