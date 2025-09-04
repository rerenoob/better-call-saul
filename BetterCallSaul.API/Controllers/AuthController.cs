using BetterCallSaul.API.Models;
using BetterCallSaul.Core.Models;
using BetterCallSaul.Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly UserManager<User> _userManager;

    public AuthController(
        IAuthenticationService authenticationService,
        UserManager<User> userManager)
    {
        _authenticationService = authenticationService;
        _userManager = userManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
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

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // Assign default role
        await _userManager.AddToRoleAsync(user, "PublicDefender");

        return Ok(new { message = "User registered successfully" });
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
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? BarNumber { get; set; }
    public string? LawFirm { get; set; }
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}