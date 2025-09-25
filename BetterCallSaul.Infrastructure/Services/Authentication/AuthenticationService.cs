using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace BetterCallSaul.Infrastructure.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly BetterCallSaulContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationService(
        BetterCallSaulContext context,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add user roles to claims
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                        _configuration["JwtSettings:SecretKey"] ?? 
                        throw new InvalidOperationException("JWT SecretKey is not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"])),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<User?> AuthenticateUser(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive)
            return null;

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        return result.Succeeded ? user : null;
    }

    public Task<bool> ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                            _configuration["JwtSettings:SecretKey"] ?? 
                            throw new InvalidOperationException("JWT SecretKey is not configured");
            var key = Encoding.UTF8.GetBytes(secretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<string> GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Task.FromResult(Convert.ToBase64String(randomNumber));
    }

    public Task<(string Token, string RefreshToken)> RefreshToken(string refreshToken)
    {
        // Implementation for refresh token logic
        // This would typically involve validating the refresh token
        // and generating a new JWT token
        throw new NotImplementedException("Refresh token functionality not implemented yet");
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return null;

        return await _userManager.FindByIdAsync(userId.ToString());
    }
}