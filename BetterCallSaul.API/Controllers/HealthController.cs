using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BetterCallSaul.Core.Enums;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly BetterCallSaulContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(BetterCallSaulContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetServiceStatus()
    {
        try
        {
            var lastHour = DateTime.UtcNow.AddHours(-1);
            
            // Get error statistics for OCR and AI services
            var ocrErrors = await _context.AuditLogs
                .Where(a => a.Action.Contains("OCR") && a.Level == AuditLogLevel.Error && a.CreatedAt >= lastHour)
                .CountAsync();

            var aiErrors = await _context.AuditLogs
                .Where(a => (a.Action.Contains("AI") || a.Action.Contains("ANALYSIS")) && a.Level == AuditLogLevel.Error && a.CreatedAt >= lastHour)
                .CountAsync();

            var criticalErrors = await _context.AuditLogs
                .Where(a => a.Level == AuditLogLevel.Critical && a.CreatedAt >= lastHour)
                .CountAsync();

            return Ok(new 
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                services = new
                {
                    ocr = new { errors_last_hour = ocrErrors, status = ocrErrors > 5 ? "Degraded" : "Healthy" },
                    ai_analysis = new { errors_last_hour = aiErrors, status = aiErrors > 3 ? "Degraded" : "Healthy" },
                    critical_errors = criticalErrors
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service status");
            return StatusCode(500, new { status = "Unhealthy", error = "Failed to retrieve service status" });
        }
    }
}