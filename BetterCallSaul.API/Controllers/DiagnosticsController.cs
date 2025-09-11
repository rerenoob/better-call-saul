using Microsoft.AspNetCore.Mvc;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"
        });
    }

    [HttpGet("config")]
    public IActionResult ConfigCheck()
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var openAIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        return Ok(new {
            jwtSecretConfigured = !string.IsNullOrEmpty(jwtKey),
            openAIEndpointConfigured = !string.IsNullOrEmpty(openAIEndpoint),
            openAIKeyConfigured = !string.IsNullOrEmpty(openAIKey),
            configurationStatus = "checked_safely",
            timestamp = DateTime.UtcNow
        });
    }
}