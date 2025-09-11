using BetterCallSaul.API.DTOs.Auth;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly UserManager<User> _userManager;
    private readonly BetterCallSaulContext _context;

    public AuthController(
        IAuthenticationService authenticationService,
        UserManager<User> userManager,
        BetterCallSaulContext context)
    {
        _authenticationService = authenticationService;
        _userManager = userManager;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // TEMPORARY: Production deployment compatibility for existing frontend
        // TODO: Remove this after proper user registration is implemented
        if (request.Email == "test@example.com" && request.Password == "test123")
        {
            try
            {
                // Create a mock user for JWT token generation
                var mockUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "Public Defender",
                    IsActive = true,
                    UserName = "test@example.com",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    EmailConfirmed = true,
                    LockoutEnabled = false,
                    TwoFactorEnabled = false,
                    AccessFailedCount = 0
                };

                // Add mock user to database temporarily for JWT generation to work
                _context.Users.Add(mockUser);
                await _context.SaveChangesAsync();

                // Reload the user from database to ensure all properties are properly set
                var reloadedUser = await _userManager.FindByIdAsync(mockUser.Id.ToString());
                if (reloadedUser != null)
                {
                    // Assign User role
                    await _userManager.AddToRoleAsync(reloadedUser, "User");

                    var token = await _authenticationService.GenerateJwtToken(reloadedUser);
                    var refreshToken = await _authenticationService.GenerateRefreshToken();

                    // Clean up - remove the mock user
                    _context.Users.Remove(reloadedUser);
                    await _context.SaveChangesAsync();

                    var mockResponse = new AuthResponse
                    {
                        Token = token,
                        RefreshToken = refreshToken,
                        Expiration = DateTime.Now.AddMinutes(60),
                        UserId = reloadedUser.Id.ToString(),
                        Email = "test@example.com",
                        FullName = "Test Public Defender",
                        Roles = new List<string> { "User" }
                    };
                
                    Console.WriteLine("SUCCESS: JWT token generated successfully with real authentication service");
                    return Ok(mockResponse);
                }
                else
                {
                    throw new InvalidOperationException("Failed to reload mock user from database");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in mock login with JWT generation: {ex.Message}");
                // Fallback to simple mock token if JWT generation fails
                var fallbackResponse = new AuthResponse
                {
                    Token = "mock-jwt-token-fallback",
                    RefreshToken = Guid.NewGuid().ToString(),
                    Expiration = DateTime.Now.AddMinutes(60),
                    UserId = Guid.NewGuid().ToString(),
                    Email = "test@example.com",
                    FullName = "Test Public Defender",
                    Roles = new List<string> { "User" }
                };
                return Ok(fallbackResponse);
            }
        }

        try
        {
            var user = await _authenticationService.AuthenticateUser(request.Email, request.Password);
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var token = await _authenticationService.GenerateJwtToken(user);
            var refreshToken = await _authenticationService.GenerateRefreshToken();

            var roles = await _userManager.GetRolesAsync(user);

            var response = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.Now.AddMinutes(60), // Should match JWT settings
                UserId = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roles.ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Log the exception for debugging but don't expose internal details
            Console.WriteLine($"Login error: {ex.Message}");
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validate registration code
        var registrationCode = await _context.RegistrationCodes
            .FirstOrDefaultAsync(rc => rc.Code == request.RegistrationCode);

        if (registrationCode == null)
            return BadRequest(new { message = "Invalid registration code" });

        if (registrationCode.IsUsed)
            return BadRequest(new { message = "Registration code has already been used" });

        if (registrationCode.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "Registration code has expired" });

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            BarNumber = request.BarNumber,
            LawFirm = request.LawFirm,
            IsActive = true
        };

        // Use a transaction to ensure atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            // Assign default User role
            try
            {
                await _userManager.AddToRoleAsync(user, "User");
            }
            catch (Exception ex)
            {
                // Log the error but continue with registration
                Console.WriteLine($"Warning: Could not assign User role to {user.Email}: {ex.Message}");
            }

            // Mark registration code as used with the user ID
            registrationCode.IsUsed = true;
            registrationCode.UsedByUserId = user.Id;
            registrationCode.UsedAt = DateTime.UtcNow;
            registrationCode.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        // Automatically log in the user after successful registration
        var token = await _authenticationService.GenerateJwtToken(user);
        var refreshToken = await _authenticationService.GenerateRefreshToken();
        var roles = await _userManager.GetRolesAsync(user);

        var response = new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.Now.AddMinutes(60),
            UserId = user.Id.ToString(),
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList()
        };

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var result = await _authenticationService.RefreshToken(request.RefreshToken);
            return Ok(new AuthResponse
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken,
                Expiration = DateTime.Now.AddMinutes(60)
            });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var user = await _authenticationService.GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsActive = user.IsActive
            });
        }
        catch (Exception ex)
        {
            // Log the exception for debugging but don't expose internal details
            Console.WriteLine($"Profile error: {ex.Message}");
            return Unauthorized();
        }
    }
}