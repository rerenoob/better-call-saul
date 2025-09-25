using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Services.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Moq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace BetterCallSaul.Tests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly BetterCallSaul.Infrastructure.Services.Authentication.AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), 
            Mock.Of<IOptions<IdentityOptions>>(), 
            Mock.Of<IPasswordHasher<User>>(), 
            new IUserValidator<User>[0], 
            new IPasswordValidator<User>[0], 
            Mock.Of<ILookupNormalizer>(), 
            Mock.Of<IdentityErrorDescriber>(), 
            Mock.Of<IServiceProvider>(), 
            Mock.Of<ILogger<UserManager<User>>>());
        
        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object, 
            Mock.Of<IHttpContextAccessor>(), 
            Mock.Of<IUserClaimsPrincipalFactory<User>>(), 
            Mock.Of<IOptions<IdentityOptions>>(), 
            Mock.Of<ILogger<SignInManager<User>>>(), 
            Mock.Of<IAuthenticationSchemeProvider>(), 
            Mock.Of<IUserConfirmation<User>>());

        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["JwtSettings:SecretKey"]).Returns("TestSecretKey123456789012345678901234567890");
        _configurationMock.Setup(c => c["JwtSettings:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["JwtSettings:Audience"]).Returns("TestAudience");
        _configurationMock.Setup(c => c["JwtSettings:ExpiryMinutes"]).Returns("60");

        _authService = new BetterCallSaul.Infrastructure.Services.Authentication.AuthenticationService(
            Mock.Of<BetterCallSaul.Infrastructure.Data.BetterCallSaulContext>(),
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _configurationMock.Object,
            Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>());
    }

    [Fact]
    public async Task GenerateJwtToken_ValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com", 
            FirstName = "Test",
            LastName = "User",
            IsActive = true 
        };
        
        _userManagerMock.Setup(um => um.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Attorney" });

        // Act
        var token = await _authService.GenerateJwtToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        Assert.Equal(user.Id.ToString(), jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(user.FullName, jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Attorney");
    }

    [Fact]
    public async Task AuthenticateUser_ValidCredentials_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", FirstName = "Test", LastName = "User", IsActive = true };
        
        _userManagerMock.Setup(um => um.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, "password", false))
            .ReturnsAsync(SignInResult.Success);

        // Act
        var result = await _authService.AuthenticateUser("test@example.com", "password");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task AuthenticateUser_InvalidEmail_ReturnsNull()
    {
        // Arrange
        _userManagerMock.Setup(um => um.FindByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.AuthenticateUser("nonexistent@example.com", "password");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateUser_InactiveUser_ReturnsNull()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "inactive@example.com", IsActive = false };
        
        _userManagerMock.Setup(um => um.FindByEmailAsync("inactive@example.com"))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.AuthenticateUser("inactive@example.com", "password");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateUser_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", FirstName = "Test", LastName = "User", IsActive = true };
        
        _userManagerMock.Setup(um => um.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);
        
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, "wrongpassword", false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _authService.AuthenticateUser("test@example.com", "wrongpassword");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", FirstName = "Test", LastName = "User", IsActive = true };
        _userManagerMock.Setup(um => um.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Attorney" });
        var token = await _authService.GenerateJwtToken(user);

        // Act
        var isValid = await _authService.ValidateToken(token);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateToken_InvalidToken_ReturnsFalse()
    {
        // Act
        var isValid = await _authService.ValidateToken("invalid.token.here");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateToken_ExpiredToken_ReturnsFalse()
    {
        // Arrange - Create a token with short expiration
        _configurationMock.Setup(c => c["JwtSettings:ExpiryMinutes"]).Returns("0.001"); // 60ms
        
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", FirstName = "Test", LastName = "User", IsActive = true };
        _userManagerMock.Setup(um => um.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Attorney" });
        var token = await _authService.GenerateJwtToken(user);
        
        await Task.Delay(100); // Wait for token to expire

        // Act
        var isValid = await _authService.ValidateToken(token);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task GenerateRefreshToken_ReturnsValidToken()
    {
        // Act
        var refreshToken = await _authService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
        // Refresh tokens should be base64 encoded 32-byte values
        var bytes = Convert.FromBase64String(refreshToken);
        Assert.Equal(32, bytes.Length);
    }

    [Fact]
    public void RefreshToken_NotImplemented_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsAsync<NotImplementedException>(() => _authService.RefreshToken("refresh_token"));
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsNull()
    {
        // Act
        var user = await _authService.GetCurrentUserAsync();

        // Assert
        Assert.Null(user);
    }
}