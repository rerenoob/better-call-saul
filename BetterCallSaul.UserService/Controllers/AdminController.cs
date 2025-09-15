using BetterCallSaul.UserService.Models.Entities;
using BetterCallSaul.UserService.Models.Enums;
using BetterCallSaul.UserService.Data;
using BetterCallSaul.UserService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetterCallSaul.UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserServiceContext _context;
    private readonly UserManager<User> _userManager;

    public AdminController(UserServiceContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("dashboard/metrics")]
    public async Task<IActionResult> GetDashboardMetrics()
    {
        var totalUsers = await _userManager.Users.CountAsync();
        var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
        
        var activeIncidents = await _context.AuditLogs
            .Where(a => a.Level == AuditLogLevel.Error && a.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        return Ok(new
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            ActiveIncidents = activeIncidents
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _userManager.Users
            .OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync();
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.FullName,
                u.BarNumber,
                u.LawFirm,
                u.IsActive,
                u.CreatedAt,
                u.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Users = users,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userManager.Users
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.FullName,
                u.BarNumber,
                u.LawFirm,
                u.IsActive,
                u.CreatedAt,
                u.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "User status updated successfully" });
    }

    [HttpGet("system/health")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var databaseStatus = await _context.Database.CanConnectAsync() ? "Healthy" : "Unhealthy";
        
        var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024; // MB
        var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

        var recentErrors = await _context.AuditLogs
            .Where(a => a.Level == AuditLogLevel.Error && a.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .CountAsync();

        return Ok(new
        {
            Database = databaseStatus,
            MemoryUsageMB = memoryUsage,
            Uptime = uptime.ToString("d'd 'h'h 'm'm'"),
            RecentErrors = recentErrors,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync();
        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                Message = a.Description,
                a.Level,
                a.Action,
                a.CreatedAt,
                User = a.User != null ? new { a.User.FullName, a.User.Email } : null,
                a.IpAddress,
                a.UserAgent
            })
            .ToListAsync();

        return Ok(new
        {
            Logs = logs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("registration-codes/stats")]
    public async Task<IActionResult> GetRegistrationCodeStats()
    {
        var total = await _context.RegistrationCodes.CountAsync();
        var used = await _context.RegistrationCodes.CountAsync(rc => rc.IsUsed);
        var expired = await _context.RegistrationCodes.CountAsync(rc => !rc.IsUsed && rc.ExpiresAt < DateTime.UtcNow);
        var active = total - used - expired;

        return Ok(new
        {
            Total = total,
            Active = active,
            Used = used,
            Expired = expired
        });
    }

    [HttpPost("registration-codes/generate")]
    public async Task<IActionResult> GenerateRegistrationCodes([FromQuery] int count = 100, [FromQuery] int expireDays = 365)
    {
        var seedingService = new DatabaseSeedingService(_context, null!, null!);
        await seedingService.SeedRegistrationCodesAsync(count, expireDays, User.Identity?.Name ?? "Admin");

        return Ok(new { message = $"Successfully generated {count} registration codes" });
    }
}