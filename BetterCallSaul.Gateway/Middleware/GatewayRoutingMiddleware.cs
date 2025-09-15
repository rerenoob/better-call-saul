using BetterCallSaul.Gateway.Services;
using Microsoft.AspNetCore.Http;

namespace BetterCallSaul.Gateway.Middleware
{
    public class GatewayRoutingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GatewayRoutingMiddleware> _logger;
        private readonly IGatewayService _gatewayService;

        public GatewayRoutingMiddleware(RequestDelegate next, ILogger<GatewayRoutingMiddleware> logger, IGatewayService gatewayService)
        {
            _next = next;
            _logger = logger;
            _gatewayService = gatewayService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            
            // Skip if it's a health check or swagger request
            if (path.StartsWith("/health") || path.StartsWith("/swagger") || path == "/")
            {
                await _next(context);
                return;
            }

            // Extract authorization token
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            // Validate JWT token for all requests except auth endpoints
            if (!path.StartsWith("/api/auth") && !path.StartsWith("/auth"))
            {
                if (string.IsNullOrEmpty(authHeader) || !await _gatewayService.ValidateJwtTokenAsync(authHeader))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized: Invalid or missing JWT token");
                    return;
                }
            }

            // Route requests to appropriate services
            string? serviceName = null;
            string targetPath = path;

            if (path.StartsWith("/api/auth") || path.StartsWith("/auth") || 
                path.StartsWith("/api/users") || path.StartsWith("/users") ||
                path.StartsWith("/api/admin") || path.StartsWith("/admin"))
            {
                serviceName = "UserService";
                // Remove /api prefix if present for service routing
                targetPath = path.StartsWith("/api") ? path.Substring(4) : path;
            }
            else if (path.StartsWith("/api/cases") || path.StartsWith("/cases") ||
                     path.StartsWith("/api/documents") || path.StartsWith("/documents") ||
                     path.StartsWith("/api/analysis") || path.StartsWith("/analysis") ||
                     path.StartsWith("/api/research") || path.StartsWith("/research"))
            {
                serviceName = "CaseService";
                // Remove /api prefix if present for service routing
                targetPath = path.StartsWith("/api") ? path.Substring(4) : path;
            }

            if (serviceName != null)
            {
                try
                {
                    var response = await _gatewayService.ForwardRequestAsync(context.Request, serviceName, targetPath);
                    
                    // Copy response back to the original context
                    context.Response.StatusCode = (int)response.StatusCode;
                    
                    foreach (var header in response.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }

                    foreach (var header in response.Content.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }

                    // Remove content-length header as we'll set it properly
                    context.Response.Headers.Remove("content-length");

                    var content = await response.Content.ReadAsByteArrayAsync();
                    await context.Response.Body.WriteAsync(content, 0, content.Length);
                    
                    _logger.LogInformation("Successfully routed {Method} {Path} to {ServiceName}", 
                        context.Request.Method, path, serviceName);
                    
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error routing request to {ServiceName}", serviceName);
                    context.Response.StatusCode = StatusCodes.Status502BadGateway;
                    await context.Response.WriteAsync($"Service unavailable: {serviceName}");
                    return;
                }
            }

            // If no service matched, continue with normal pipeline
            await _next(context);
        }
    }
}