using BetterCallSaul.API.Controllers;
using BetterCallSaul.API.DTOs.Auth;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests.Integration.Controllers;

public class AuthControllerTests
{
    private readonly Mock<Core.Interfaces.Services.IAuthenticationService> _authServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<BetterCallSaulContext> _contextMock;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<Core.Interfaces.Services.IAuthenticationService>();
        
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        
        _contextMock = new Mock<BetterCallSaulContext>();
        
        _authController = new AuthController(
            _authServiceMock.Object,
            _userManagerMock.Object,
            _contextMock.Object);
    }

    [Fact]
    public async Task Login_ValidMockCredentials_ReturnsMockToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "test123"
        };

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        
        Assert.Equal("mock-jwt-token-for-testing", response.Token);
        Assert.Equal("mock-refresh-token", response.RefreshToken);
        Assert.Equal("test@example.com", response.Email);
        Assert.Equal("Test Public Defender", response.FullName);
        Assert.Contains("PublicDefender", response.Roles);
    }

    [Fact]
    public async Task Login_ValidAdminMockCredentials_ReturnsAdminToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "admin@bettercallsaul.com",
            Password = "admin123"
        };

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        
        Assert.Equal("mock-jwt-token-for-admin", response.Token);
        Assert.Equal("mock-refresh-token-admin", response.RefreshToken);
        Assert.Equal("admin@bettercallsaul.com", response.Email);
        Assert.Equal("System Administrator", response.FullName);
        Assert.Contains("Administrator", response.Roles);
        Assert.Contains("PublicDefender", response.Roles);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "invalid@example.com",
            Password = "wrongpassword"
        };

        _authServiceMock.Setup(s => s.AuthenticateUser("invalid@example.com", "wrongpassword"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task Login_ValidRealCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "realuser@example.com",
            Password = "realpassword"
        };

        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "realuser@example.com", 
            FullName = "Real User",
            IsActive = true 
        };

        _authServiceMock.Setup(s => s.AuthenticateUser("realuser@example.com", "realpassword"))
            .ReturnsAsync(user);
        _authServiceMock.Setup(s => s.GenerateJwtToken(user))
            .ReturnsAsync("real-jwt-token");
        _authServiceMock.Setup(s => s.GenerateRefreshToken())
            .ReturnsAsync("real-refresh-token");

        _userManagerMock.Setup(um => um.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "PublicDefender" });

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        
        Assert.Equal("real-jwt-token", response.Token);
        Assert.Equal("real-refresh-token", response.RefreshToken);
        Assert.Equal("realuser@example.com", response.Email);
        Assert.Equal("Real User", response.FullName);
        Assert.Contains("PublicDefender", response.Roles);
    }

    [Fact]
    public async Task Register_ValidRequest_CreatesUser()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            BarNumber = "12345",
            LawFirm = "Public Defender Office",
            RegistrationCode = "VALID-CODE-123"
        };

        var registrationCode = new RegistrationCode
        {
            Code = "VALID-CODE-123",
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var mockDbSet = new Mock<DbSet<RegistrationCode>>();
        mockDbSet.Setup(d => d.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RegistrationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrationCode);

        _contextMock.Setup(c => c.RegistrationCodes).Returns(mockDbSet.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), "Password123!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<User>(), "PublicDefender"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "PublicDefender" });

        _authServiceMock.Setup(s => s.GenerateJwtToken(It.IsAny<User>()))
            .ReturnsAsync("new-user-jwt-token");
        _authServiceMock.Setup(s => s.GenerateRefreshToken())
            .ReturnsAsync("new-user-refresh-token");

        // Act
        var result = await _authController.Register(registerRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        
        Assert.Equal("new-user-jwt-token", response.Token);
        Assert.Equal("new-user-refresh-token", response.RefreshToken);
        Assert.Equal("newuser@example.com", response.Email);
        Assert.Contains("PublicDefender", response.Roles);
        
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Register_InvalidRegistrationCode_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            RegistrationCode = "INVALID-CODE"
        };

        var mockDbSet = new Mock<DbSet<RegistrationCode>>();
        mockDbSet.Setup(d => d.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RegistrationCode, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RegistrationCode?)null);

        _contextMock.Setup(c => c.RegistrationCodes).Returns(mockDbSet.Object);

        // Act
        var result = await _authController.Register(registerRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Refresh_ValidRequest_ReturnsNewTokens()
    {
        // Arrange
        var refreshRequest = new RefreshRequest { RefreshToken = "valid-refresh-token" };
        
        _authServiceMock.Setup(s => s.RefreshToken("valid-refresh-token"))
            .ReturnsAsync(("new-jwt-token", "new-refresh-token"));

        // Act
        var result = await _authController.Refresh(refreshRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        
        Assert.Equal("new-jwt-token", response.Token);
        Assert.Equal("new-refresh-token", response.RefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidRequest_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshRequest { RefreshToken = "invalid-token" };
        
        _authServiceMock.Setup(s => s.RefreshToken("invalid-token"))
            .ThrowsAsync(new Exception("Invalid refresh token"));

        // Act
        var result = await _authController.Refresh(refreshRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task GetProfile_MockToken_ReturnsMockProfile()
    {
        // Arrange
        var mockToken = "mock-jwt-token-for-testing";
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _authController.ControllerContext.HttpContext.Request.Headers["Authorization"] = $"Bearer {mockToken}";

        // Act
        var result = await _authController.GetProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic profile = okResult.Value!;
        
        Assert.Equal("test@example.com", profile.Email);
        Assert.Equal("Test Public Defender", profile.FullName);
    }

    [Fact]
    public async Task GetProfile_AdminMockToken_ReturnsAdminProfile()
    {
        // Arrange
        var mockToken = "mock-jwt-token-for-admin";
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _authController.ControllerContext.HttpContext.Request.Headers["Authorization"] = $"Bearer {mockToken}";

        // Act
        var result = await _authController.GetProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic profile = okResult.Value!;
        
        Assert.Equal("admin@bettercallsaul.com", profile.Email);
        Assert.Equal("System Administrator", profile.FullName);
    }

    [Fact]
    public async Task GetProfile_NoToken_ReturnsUnauthorized()
    {
        // Arrange
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _authController.GetProfile();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
}