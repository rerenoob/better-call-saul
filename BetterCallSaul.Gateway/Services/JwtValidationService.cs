using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace BetterCallSaul.Gateway.Services
{
    public class JwtValidationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtValidationService> _logger;

        public JwtValidationService(IConfiguration configuration, ILogger<JwtValidationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool ValidateToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer "))
                    return false;

                var jwtToken = token.Substring("Bearer ".Length).Trim();
                
                var handler = new JwtSecurityTokenHandler();
                var secretKey = _configuration["JwtSettings:SecretKey"];
                var key = Encoding.UTF8.GetBytes(secretKey!);
                
                var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = handler.ValidateToken(jwtToken, validationParameters, out _);
                return principal != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT token validation failed");
                return false;
            }
        }

        public string GetUserIdFromToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer "))
                    return string.Empty;

                var jwtToken = token.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(jwtToken);
                
                return jsonToken.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid")?.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract user ID from JWT token");
                return string.Empty;
            }
        }

        public string GetUserEmailFromToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer "))
                    return string.Empty;

                var jwtToken = token.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(jwtToken);
                
                return jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract email from JWT token");
                return string.Empty;
            }
        }
    }
}