using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Enums;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly BetterCallSaulContext _context;
    private readonly UserManager<User> _userManager;

    public AdminController(BetterCallSaulContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("dashboard/metrics")]
    public async Task<IActionResult> GetDashboardMetrics()
    {
        var totalUsers = await _userManager.Users.CountAsync();
        var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
        
        var casesAnalyzed24h = await _context.Cases
            .Where(c => c.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .CountAsync();

        // Since AnalysisTimeSeconds doesn't exist in Case model, using a placeholder
        var avgAnalysisTime = 0.0;

        var activeIncidents = await _context.AuditLogs
            .Where(a => a.Level == AuditLogLevel.Error && a.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        return Ok(new
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            CasesAnalyzed24h = casesAnalyzed24h,
            AvgAnalysisTime = Math.Round(avgAnalysisTime, 1),
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
                u.UpdatedAt,
                CasesCount = u.Cases.Count,
                LastActivity = u.Cases.Any() ? (DateTime?)u.Cases.OrderByDescending(c => c.CreatedAt).First().CreatedAt : null
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

    [HttpGet("cases/stats")]
    public async Task<IActionResult> GetCaseStatistics()
    {
        var totalCases = await _context.Cases.CountAsync();
        var casesByStatus = await _context.Cases
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var casesByDay = await _context.Cases
            .Where(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync();

        return Ok(new
        {
            TotalCases = totalCases,
            CasesByStatus = casesByStatus,
            CasesByDay = casesByDay
        });
    }

    [HttpGet("registration-codes")]
    public async Task<IActionResult> GetRegistrationCodes([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _context.RegistrationCodes
            .Include(rc => rc.UsedByUser)
            .OrderByDescending(rc => rc.CreatedAt);

        var totalCount = await query.CountAsync();
        var codes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rc => new
            {
                rc.Id,
                rc.Code,
                rc.CreatedBy,
                rc.IsUsed,
                rc.UsedByUserId,
                rc.UsedAt,
                rc.ExpiresAt,
                rc.CreatedAt,
                rc.UpdatedAt,
                rc.Notes,
                UsedByUserName = rc.UsedByUser != null ? rc.UsedByUser.FullName : null,
                IsValid = !rc.IsUsed && rc.ExpiresAt > DateTime.UtcNow
            })
            .ToListAsync();

        return Ok(new
        {
            Codes = codes,
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
    public async Task<IActionResult> GenerateRegistrationCodes([FromBody] GenerateCodesRequest request)
    {
        var expirationDate = DateTime.UtcNow.AddDays(request.ExpireDays);
        var codes = new List<RegistrationCode>();

        for (int i = 0; i < request.Count; i++)
        {
            var code = GenerateUniqueCode();
            
            // Ensure code is unique in the database
            while (await _context.RegistrationCodes.AnyAsync(rc => rc.Code == code) || codes.Any(c => c.Code == code))
            {
                code = GenerateUniqueCode();
            }

            codes.Add(new RegistrationCode
            {
                Code = code,
                CreatedBy = request.CreatedBy ?? "System",
                ExpiresAt = expirationDate,
                Notes = request.Notes,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.RegistrationCodes.AddRangeAsync(codes);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Successfully generated {codes.Count} registration codes", codes = codes.Take(5).Select(c => c.Code) });
    }

    [HttpDelete("registration-codes/{id}")]
    public async Task<IActionResult> DeleteRegistrationCode(Guid id)
    {
        var code = await _context.RegistrationCodes.FindAsync(id);
        if (code == null)
            return NotFound();

        if (code.IsUsed)
            return BadRequest(new { message = "Cannot delete a registration code that has already been used" });

        _context.RegistrationCodes.Remove(code);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registration code deleted successfully" });
    }

    [HttpPost("registration-codes/cleanup")]
    public async Task<IActionResult> CleanupExpiredCodes()
    {
        var expiredCodes = await _context.RegistrationCodes
            .Where(rc => rc.ExpiresAt < DateTime.UtcNow && !rc.IsUsed)
            .ToListAsync();

        if (expiredCodes.Any())
        {
            _context.RegistrationCodes.RemoveRange(expiredCodes);
            var removedCount = await _context.SaveChangesAsync();
            return Ok(new { message = $"Cleaned up {removedCount} expired registration codes" });
        }

        return Ok(new { message = "No expired registration codes found to clean up" });
    }

    private static string GenerateUniqueCode(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        using var rng = RandomNumberGenerator.Create();
        var result = new StringBuilder(length);
        
        var bytes = new byte[4];
        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(bytes);
            var randomIndex = Math.Abs(BitConverter.ToInt32(bytes, 0)) % chars.Length;
            result.Append(chars[randomIndex]);
        }
        
        return result.ToString();
    }
}

public class GenerateCodesRequest
{
    public int Count { get; set; } = 10;
    public int ExpireDays { get; set; } = 365;
    public string? CreatedBy { get; set; }
    public string? Notes { get; set; }
}