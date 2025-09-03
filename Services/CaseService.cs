using better_call_saul.Data;
using better_call_saul.Models;
using better_call_saul.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace better_call_saul.Services;

public class CaseService : ICaseService
{
    private readonly ApplicationDbContext _context;
    private readonly ILoggerService _logger;

    public CaseService(ApplicationDbContext context, ILoggerService logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Case>> GetUserCasesAsync(string userId)
    {
        return await _context.Cases
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Documents.Where(d => !d.IsDeleted))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Case?> GetCaseByIdAsync(int caseId, string userId)
    {
        return await _context.Cases
            .Where(c => c.Id == caseId && c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Documents.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync();
    }

    public async Task<Case> CreateCaseAsync(CaseViewModel model, string userId)
    {
        var caseEntity = new Case
        {
            Title = model.Title,
            Description = model.Description,
            Status = model.Status,
            UserId = userId
        };

        _context.Cases.Add(caseEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Case created: {caseEntity.Id} - {caseEntity.Title} by user {userId}");
        return caseEntity;
    }

    public async Task<bool> UpdateCaseAsync(int caseId, CaseViewModel model, string userId)
    {
        var caseEntity = await GetCaseByIdAsync(caseId, userId);
        if (caseEntity == null)
        {
            _logger.LogWarning($"Case not found for update: {caseId} by user {userId}");
            return false;
        }

        caseEntity.Title = model.Title;
        caseEntity.Description = model.Description;
        caseEntity.Status = model.Status;

        // If status is being set to Closed, set the ClosedAt timestamp
        if (model.Status == CaseStatus.Closed && caseEntity.ClosedAt == null)
        {
            caseEntity.ClosedAt = DateTime.UtcNow;
        }
        // If status is being changed from Closed to something else, clear the ClosedAt
        else if (model.Status != CaseStatus.Closed && caseEntity.ClosedAt != null)
        {
            caseEntity.ClosedAt = null;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Case updated: {caseId} by user {userId}");
        return true;
    }

    public async Task<bool> DeleteCaseAsync(int caseId, string userId)
    {
        var caseEntity = await GetCaseByIdAsync(caseId, userId);
        if (caseEntity == null)
        {
            _logger.LogWarning($"Case not found for deletion: {caseId} by user {userId}");
            return false;
        }

        caseEntity.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Case soft-deleted: {caseId} by user {userId}");
        return true;
    }

    public async Task<CaseDetailViewModel> GetCaseDetailAsync(int caseId, string userId)
    {
        var caseEntity = await GetCaseByIdAsync(caseId, userId);
        if (caseEntity == null)
        {
            _logger.LogWarning($"Case not found for detail view: {caseId} by user {userId}");
            return null;
        }

        var documentSummaries = caseEntity.Documents.Select(d => new DocumentSummary
        {
            Id = d.Id,
            FileName = d.FileName,
            FileType = d.FileType,
            UploadedAt = d.CreatedAt,
            FileSize = d.FileSize
        });

        return new CaseDetailViewModel
        {
            Case = caseEntity,
            Documents = documentSummaries,
            CanEdit = true // For now, always allow edit if user can view
        };
    }

    public async Task<CaseListViewModel> GetUserCaseListAsync(string userId)
    {
        var cases = await _context.Cases
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Documents.Where(d => !d.IsDeleted))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var caseSummaries = cases.Select(c => new better_call_saul.Models.ViewModels.CaseSummary
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            DocumentCount = c.Documents.Count
        });

        var statusCounts = cases
            .GroupBy(c => c.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return new CaseListViewModel
        {
            Cases = caseSummaries,
            TotalCases = cases.Count,
            StatusCounts = statusCounts
        };
    }
}