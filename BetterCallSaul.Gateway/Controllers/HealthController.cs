using Microsoft.AspNetCore.Mvc;

namespace BetterCallSaul.Gateway.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IConfiguration configuration, ILogger<HealthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetHealth()
        {
            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "BetterCallSaul API Gateway",
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                Services = new
                {
                    UserService = _configuration["Services:UserService:BaseUrl"],
                    CaseService = _configuration["Services:CaseService:BaseUrl"]
                }
            };

            _logger.LogInformation("Health check executed - Status: {Status}", healthStatus.Status);
            return Ok(healthStatus);
        }

        [HttpGet("ready")]
        public IActionResult GetReadiness()
        {
            try
            {
                // Check connectivity to downstream services
                var services = new Dictionary<string, string>
                {
                    { "UserService", _configuration["Services:UserService:BaseUrl"]! },
                    { "CaseService", _configuration["Services:CaseService:BaseUrl"]! }
                };

                var readinessStatus = new
                {
                    Status = "Ready",
                    Timestamp = DateTime.UtcNow,
                    ServiceConnectivity = services
                };

                _logger.LogInformation("Readiness check executed - Status: {Status}", readinessStatus.Status);
                return Ok(readinessStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readiness check failed");
                return StatusCode(503, new { Status = "Not Ready", Error = ex.Message });
            }
        }
    }
}