using BetterCallSaul.Core.Models;

namespace BetterCallSaul.Core.Services;

public interface IAuthenticationService
{
    Task<string> GenerateJwtToken(User user);
    Task<User?> AuthenticateUser(string email, string password);
    Task<bool> ValidateToken(string token);
    Task<string> GenerateRefreshToken();
    Task<(string Token, string RefreshToken)> RefreshToken(string refreshToken);
}