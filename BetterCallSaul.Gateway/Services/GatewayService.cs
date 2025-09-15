using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BetterCallSaul.Gateway.Services
{
    public class GatewayService : IGatewayService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GatewayService> _logger;

        public GatewayService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GatewayService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> ForwardRequestAsync(HttpRequest request, string serviceName, string targetPath)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(serviceName);
                var timeout = _configuration.GetValue<int>($"Services:{serviceName}:Timeout", 30);
                client.Timeout = TimeSpan.FromSeconds(timeout);

                // Create the target URL
                var baseUrl = _configuration[$"Services:{serviceName}:BaseUrl"];
                var url = $"{baseUrl}{targetPath}{request.QueryString}";

                // Create the forwarded request
                var forwardedRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = new HttpMethod(request.Method)
                };

                // Copy headers (excluding host and content headers)
                foreach (var header in request.Headers)
                {
                    if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                        header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                        header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!forwardedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                    {
                        _logger.LogWarning("Failed to add header {Header} to forwarded request", header.Key);
                    }
                }

                // Copy authorization header if present
                if (request.Headers.TryGetValue("Authorization", out var authHeader) && !string.IsNullOrEmpty(authHeader))
                {
                    forwardedRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader!);
                }

                // Copy content for non-GET requests
                if (request.Method != "GET" && request.Method != "HEAD" && request.ContentLength > 0)
                {
                    using var ms = new MemoryStream();
                    await request.Body.CopyToAsync(ms);
                    ms.Position = 0;
                    forwardedRequest.Content = new StreamContent(ms);

                    // Copy content headers
                    if (request.Headers.TryGetValue("Content-Type", out var contentType) && !string.IsNullOrEmpty(contentType))
                    {
                        forwardedRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType!);
                    }
                }

                _logger.LogInformation("Forwarding {Method} request to {ServiceName}: {Url}", 
                    request.Method, serviceName, url);

                return await client.SendAsync(forwardedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding request to {ServiceName}", serviceName);
                throw;
            }
        }

        public async Task<bool> ValidateJwtTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer "))
                    return await Task.FromResult(false);

                var jwtToken = token.Substring("Bearer ".Length).Trim();
                
                // For now, we'll use the same validation as the authentication middleware
                // In a real implementation, you might want to call the UserService to validate
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
                return await Task.FromResult(principal != null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT token validation failed");
                return await Task.FromResult(false);
            }
        }

        public async Task<string> ExtractUserIdFromTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer "))
                    return await Task.FromResult(string.Empty);

                var jwtToken = token.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(jwtToken);
                
                var userId = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid")?.Value ?? string.Empty;
                return await Task.FromResult(userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract user ID from JWT token");
                return await Task.FromResult(string.Empty);
            }
        }
    }
}