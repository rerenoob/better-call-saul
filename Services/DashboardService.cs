using better_call_saul.Data;
using better_call_saul.Models;
using Microsoft.EntityFrameworkCore;

namespace better_call_saul.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentAnalysisService _analysisService;
    private readonly ILoggerService _logger;

    public DashboardService(
        ApplicationDbContext context,
        IDocumentAnalysisService analysisService,
        ILoggerService logger)
    {
        _context = context;
        _analysisService = analysisService;
        _logger = logger;
    }

    public async Task<DashboardData> GetDashboardDataAsync(string userId)
    {
        var data = new DashboardData();
        
        try
        {
            // Get total cases for the user
            data.TotalCases = await _context.Cases
                .Where(c => c.UserId == userId)
                .CountAsync();

            // Get cases this month
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            data.CasesThisMonth = await _context.Cases
                .Where(c => c.UserId == userId && c.CreatedAt >= startOfMonth)
                .CountAsync();

            // Get cases by status
            var casesByStatus = await _context.Cases
                .Where(c => c.UserId == userId)
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            data.CasesByStatus = casesByStatus.ToDictionary(
                x => x.Status,
                x => x.Count
            );

            // Get recent cases
            data.RecentCases = await _context.Cases
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new CaseSummary
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    DocumentCount = c.Documents.Count
                })
                .ToListAsync();

            // Get document and analysis counts
            data.DocumentsProcessed = await _context.Documents
                .Where(d => d.Case.UserId == userId)
                .CountAsync();

            data.AnalysesCompleted = await _context.CaseAnalyses
                .Where(ca => ca.Case.UserId == userId)
                .CountAsync();

            _logger.LogInformation($"Dashboard data retrieved for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving dashboard data: {ex.Message}");
            throw;
        }

        return data;
    }
}
