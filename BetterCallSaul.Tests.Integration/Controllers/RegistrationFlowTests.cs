using BetterCallSaul.API.DTOs.Auth;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace BetterCallSaul.Tests.Integration.Controllers;

public class RegistrationFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RegistrationFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidCode_ShouldNotCauseForeignKeyConstraintError()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BetterCallSaulContext>();
        
        // Get an unused registration code
        var unusedCode = await context.RegistrationCodes
            .FirstOrDefaultAsync(rc => !rc.IsUsed && rc.ExpiresAt > DateTime.UtcNow);
            
        Assert.NotNull(unusedCode);

        var request = new RegisterRequest
        {
            Email = "test-{Guid.NewGuid()}@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            RegistrationCode = unusedCode.Code
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.NotNull(authResponse.Token);
        Assert.NotNull(authResponse.RefreshToken);

        // Verify registration code was properly updated
        var updatedCode = await context.RegistrationCodes
            .FirstOrDefaultAsync(rc => rc.Code == unusedCode.Code);
            
        Assert.NotNull(updatedCode);
        Assert.True(updatedCode.IsUsed);
        Assert.NotNull(updatedCode.UsedByUserId);
        Assert.NotNull(updatedCode.UsedAt);

        // Verify user was created
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(request.Email);
        Assert.NotNull(user);
        Assert.Equal(updatedCode.UsedByUserId, user.Id);
    }

    [Fact]
    public async Task Register_WithInvalidCode_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test-{Guid.NewGuid()}@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            RegistrationCode = "INVALID-CODE-123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid registration code", content);
    }

    [Fact]
    public async Task Register_WithUsedCode_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BetterCallSaulContext>();
        
        // Get a used registration code
        var usedCode = await context.RegistrationCodes
            .FirstOrDefaultAsync(rc => rc.IsUsed);
            
        Assert.NotNull(usedCode);

        var request = new RegisterRequest
        {
            Email = "test-{Guid.NewGuid()}@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            RegistrationCode = usedCode.Code
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("already been used", content);
    }

    [Fact]
    public async Task Register_WithExpiredCode_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BetterCallSaulContext>();
        
        // Create an expired registration code
        var expiredCode = new RegistrationCode
        {
            Code = "EXPIRED-{Guid.NewGuid()}",
            CreatedBy = "Test",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        
        context.RegistrationCodes.Add(expiredCode);
        await context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "test-{Guid.NewGuid()}@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            RegistrationCode = expiredCode.Code
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("expired", content);

        // Cleanup
        context.RegistrationCodes.Remove(expiredCode);
        await context.SaveChangesAsync();
    }
}