using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using BetterCallSaul.Core.Interfaces.Repositories;
using BetterCallSaul.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("ReactFrontend")]
public class CaseController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly ICaseAnalysisService _caseAnalysisService;
    private readonly ICaseManagementService _caseManagementService;
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly ILogger<CaseController> _logger;
    private readonly BetterCallSaulContext _context;

    public CaseController(
        IAIService aiService, 
        ICaseAnalysisService caseAnalysisService,
        ICaseManagementService caseManagementService,
        ICaseDocumentRepository caseDocumentRepository,
        ILogger<CaseController> logger, 
        BetterCallSaulContext context)
    {
        _aiService = aiService;
        _caseAnalysisService = caseAnalysisService;
        _caseManagementService = caseManagementService;
        _caseDocumentRepository = caseDocumentRepository;
        _logger = logger;
        _context = context;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Case>> CreateCase([FromBody] CreateCaseRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var caseItem = new Case
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                UserId = userId,
                CaseNumber = $"CASE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                Status = CaseStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Cases.Add(caseItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCase), new { id = caseItem.Id }, caseItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating case");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<CaseWithDocuments>> GetCase(Guid id)
    {
        try
        {
            var caseWithDocuments = await _caseManagementService.GetCaseWithDocumentsAsync(id);
            return Ok(caseWithDocuments);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting case {CaseId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<Case>> UpdateCase(Guid id, [FromBody] UpdateCaseRequest request)
    {
        try
        {
            var caseItem = await _context.Cases.FindAsync(id);
            if (caseItem == null)
            {
                return NotFound();
            }

            // Verify user owns the case
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty || caseItem.UserId != userId)
            {
                return Unauthorized();
            }

            // Update allowed fields
            if (!string.IsNullOrEmpty(request.Title))
                caseItem.Title = request.Title;
            
            if (!string.IsNullOrEmpty(request.Description))
                caseItem.Description = request.Description;
            
            if (request.Status.HasValue)
                caseItem.Status = request.Status.Value;
            
            if (request.HearingDate.HasValue)
                caseItem.HearingDate = request.HearingDate.Value;

            caseItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(caseItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating case {CaseId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteCase(Guid id)
    {
        try
        {
            // Verify user owns the case
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            // Get the case to verify ownership
            var caseItem = await _context.Cases.FindAsync(id);
            if (caseItem == null)
            {
                return NotFound();
            }

            if (caseItem.UserId != userId)
            {
                return Unauthorized();
            }

            // Use the service method for soft delete
            await _caseManagementService.DeleteCaseAsync(id);

            return Ok(new { message = "Case deleted successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting case {CaseId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("{id}/chat")]
    [Authorize]
    public async Task<ActionResult<AIResponse>> ChatWithAI(Guid id, [FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Processing AI chat request for case {CaseId}", id);

            var aiRequest = new AIRequest
            {
                DocumentText = request.Message,
                CaseContext = $"Case ID: {id}. User is asking: {request.Message}",
                MaxTokens = 500,
                Temperature = 0.7
            };

            var response = await _aiService.AnalyzeCaseAsync(aiRequest);

            if (!response.Success)
            {
                return BadRequest(new { error = response.ErrorMessage });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI chat request for case {CaseId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Case>>> GetCases()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        // Use SQL database for case metadata (title, description, etc.)
        var cases = await _caseManagementService.GetCasesByUserAsync(userId);
        
        // Enhance with NoSQL data for analysis results
        var caseDocuments = await _caseDocumentRepository.GetByUserIdAsync(userId);
        var caseDocumentsDict = caseDocuments.ToDictionary(cd => cd.CaseId);
        
        foreach (var caseItem in cases)
        {
            if (caseDocumentsDict.TryGetValue(caseItem.Id, out var caseDoc))
            {
                // Update success probability from NoSQL analysis data
                caseItem.SuccessProbability = caseDoc.Analyses.FirstOrDefault()?.ViabilityScore > 0 
                    ? (decimal)(caseDoc.Analyses.First().ViabilityScore / 100.0) 
                    : 0;
            }
        }

        return Ok(cases);
    }

    [HttpGet("statistics")]
    [Authorize]
    public async Task<ActionResult> GetStatistics()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var total = await _caseDocumentRepository.CountByUserAsync(userId);
        var caseDocuments = await _caseDocumentRepository.GetByUserIdAsync(userId);
        
        // Since status is not directly available, we'll use analysis status as proxy
        var active = caseDocuments.Count(c => c.Analyses.Any(a => a.Status == AnalysisStatus.Processing));
        var completed = caseDocuments.Count(c => c.Analyses.Any(a => a.Status == AnalysisStatus.Completed));
        var overdue = 0; // Hearing date not available in NoSQL model

        // Group by analysis status as proxy for case status
        var byStatus = new Dictionary<string, int>
        {
            ["New"] = caseDocuments.Count(c => !c.Analyses.Any()),
            ["Processing"] = caseDocuments.Count(c => c.Analyses.Any(a => a.Status == AnalysisStatus.Processing)),
            ["Completed"] = caseDocuments.Count(c => c.Analyses.Any(a => a.Status == AnalysisStatus.Completed)),
            ["Failed"] = caseDocuments.Count(c => c.Analyses.Any(a => a.Status == AnalysisStatus.Failed))
        };

        var statistics = new
        {
            Total = total,
            Active = active,
            Completed = completed,
            Overdue = overdue,
            ByStatus = byStatus
        };

        return Ok(statistics);
    }

    [HttpGet("{id}/detailed")]
    [Authorize]
    public async Task<ActionResult<CaseWithDocuments>> GetCaseDetailed(Guid id)
    {
        try
        {
            var caseWithDocuments = await _caseManagementService.GetCaseWithDocumentsAsync(id);
            return Ok(caseWithDocuments);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed case {CaseId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("recent")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Case>>> GetRecentCases([FromQuery] int limit = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        // Use SQL database for case metadata (title, description, etc.)
        var cases = await _caseManagementService.GetCasesByUserAsync(userId);
        var recentCases = cases
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToList();
        
        // Enhance with NoSQL data for analysis results
        var caseDocuments = await _caseDocumentRepository.GetByUserIdAsync(userId);
        var caseDocumentsDict = caseDocuments.ToDictionary(cd => cd.CaseId);
        
        foreach (var caseItem in recentCases)
        {
            if (caseDocumentsDict.TryGetValue(caseItem.Id, out var caseDoc))
            {
                // Update success probability from NoSQL analysis data
                caseItem.SuccessProbability = caseDoc.Analyses.FirstOrDefault()?.ViabilityScore > 0 
                    ? (decimal)(caseDoc.Analyses.First().ViabilityScore / 100.0) 
                    : 0;
            }
        }

        return Ok(recentCases);
    }

    [HttpOptions]
    [HttpOptions("{*path}")]
    public IActionResult OptionsHandler()
    {
        return Ok();
    }

    [HttpPost("create-with-files")]
    [Authorize]
    public async Task<ActionResult<CaseCreationResponse>> CreateCaseWithFiles([FromBody] CreateCaseWithFilesRequest request)
    {
        try
        {
            _logger.LogInformation("Creating case with files: {Title}", request.Title);

            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new CaseCreationResponse
                {
                    Success = false,
                    CaseId = string.Empty,
                    Error = "User not authenticated"
                });
            }

            // Create the case
            var caseItem = new Case
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                UserId = userId,
                CaseNumber = $"CASE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = CaseStatus.New
            };

            _context.Cases.Add(caseItem);

            // If files are provided, associate them with the case
            if (request.FileIds != null && request.FileIds.Count > 0)
            {
                var documents = await _context.Documents
                    .Where(d => request.FileIds.Contains(d.Id.ToString()))
                    .ToListAsync();

                var temporaryCaseIds = new HashSet<Guid>();
                foreach (var document in documents)
                {
                    // Track the original case ID if it was temporary
                    if (document.CaseId != Guid.Empty)
                    {
                        var originalCase = await _context.Cases
                            .Where(c => c.Id == document.CaseId && c.Title == "TEMPORARY_UPLOAD_CASE")
                            .FirstOrDefaultAsync();
                        if (originalCase != null)
                        {
                            temporaryCaseIds.Add(originalCase.Id);
                        }
                    }

                    document.CaseId = caseItem.Id;
                    document.UpdatedAt = DateTime.UtcNow;
                }

                // Clean up empty temporary cases
                foreach (var tempCaseId in temporaryCaseIds)
                {
                    var hasRemainingDocuments = await _context.Documents
                        .AnyAsync(d => d.CaseId == tempCaseId && !d.IsDeleted);

                    if (!hasRemainingDocuments)
                    {
                        var tempCase = await _context.Cases.FindAsync(tempCaseId);
                        if (tempCase != null)
                        {
                            _context.Cases.Remove(tempCase);
                            _logger.LogInformation("Cleaned up empty temporary case {TempCaseId}", tempCaseId);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully created case {CaseId} with {FileCount} files", 
                caseItem.Id, request.FileIds?.Count ?? 0);

            // Trigger case analysis for uploaded documents with proper delay for text extraction
            _ = Task.Run(async () => 
            {
                // Wait longer for text extraction to complete
                await Task.Delay(TimeSpan.FromSeconds(15));
                await AnalyzeCaseDocumentsAsync(caseItem.Id);
            });

            return Ok(new CaseCreationResponse
            {
                Success = true,
                CaseId = caseItem.Id.ToString(),
                Message = "Case created successfully. Analysis will begin shortly."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating case with files");
            return StatusCode(500, new CaseCreationResponse
            {
                Success = false,
                CaseId = string.Empty,
                Error = "Internal server error"
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }

    private async Task AnalyzeCaseDocumentsAsync(Guid caseId)
    {
        try
        {
            _logger.LogInformation("Starting background case analysis for case {CaseId}", caseId);

            // Wait a moment for text extraction to complete
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Get documents with extracted text for this case
            var documentsWithText = await _context.Documents
                .Where(d => d.CaseId == caseId && 
                           d.Status == DocumentStatus.Processed && 
                           d.ExtractedText != null &&
                           !string.IsNullOrEmpty(d.ExtractedText.FullText))
                .Include(d => d.ExtractedText)
                .ToListAsync();

            if (!documentsWithText.Any())
            {
                _logger.LogWarning("No documents with extracted text found for case {CaseId}. Retrying in 10 seconds...", caseId);
                await Task.Delay(TimeSpan.FromSeconds(10));
                
                // Retry once more
                documentsWithText = await _context.Documents
                    .Where(d => d.CaseId == caseId && 
                               d.Status == DocumentStatus.Processed && 
                               d.ExtractedText != null &&
                               !string.IsNullOrEmpty(d.ExtractedText.FullText))
                    .Include(d => d.ExtractedText)
                    .ToListAsync();

                if (!documentsWithText.Any())
                {
                    _logger.LogWarning("Still no documents with extracted text found for case {CaseId}. Skipping analysis.", caseId);
                    return;
                }
            }

            // Analyze each document
            CaseAnalysis? latestAnalysis = null;
            foreach (var document in documentsWithText)
            {
                try
                {
                    var analysis = await _caseAnalysisService.AnalyzeCaseAsync(
                        caseId, 
                        document.Id, 
                        document.ExtractedText?.FullText ?? "");

                    latestAnalysis = analysis;
                    _logger.LogInformation("Completed analysis for document {DocumentId} in case {CaseId}", document.Id, caseId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analyzing document {DocumentId} for case {CaseId}. Exception type: {ExceptionType}", 
                        document.Id, caseId, ex.GetType().Name);
                    
                    // Create audit log for document analysis failure
                    await CreateAuditLogAsync(
                        "DOCUMENT_ANALYSIS_FAILURE",
                        $"Document analysis failed for document {document.Id} in case {caseId}: {ex.Message}",
                        "Document",
                        document.Id,
                        AuditLogLevel.Error
                    );
                }
            }

            // Update case with analysis results
            if (latestAnalysis != null)
            {
                await UpdateCaseWithAnalysisResults(caseId, latestAnalysis);
            }

            _logger.LogInformation("Completed background case analysis for case {CaseId}", caseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during background case analysis for case {CaseId}. Exception type: {ExceptionType}", 
                caseId, ex.GetType().Name);
            
            // Create audit log for background analysis failure
            await CreateAuditLogAsync(
                "BACKGROUND_ANALYSIS_FAILURE",
                $"Background case analysis failed for case {caseId}: {ex.Message}",
                "Case",
                caseId,
                AuditLogLevel.Critical
            );
        }
    }

    private async Task UpdateCaseWithAnalysisResults(Guid caseId, CaseAnalysis analysis)
    {
        try
        {
            var caseItem = await _context.Cases.FindAsync(caseId);
            if (caseItem != null)
            {
                // Update case with AI analysis results
                caseItem.Description = !string.IsNullOrEmpty(analysis.AnalysisText) 
                    ? analysis.AnalysisText.Substring(0, Math.Min(analysis.AnalysisText.Length, 1000)) + "..."
                    : caseItem.Description;
                
                // Set success probability based on viability score
                if (analysis.ViabilityScore > 0)
                {
                    caseItem.SuccessProbability = (decimal)(analysis.ViabilityScore / 100.0); // Convert to decimal
                }

                caseItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated case {CaseId} with analysis results. Success probability: {Probability}", 
                    caseId, caseItem.SuccessProbability);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating case {CaseId} with analysis results. Exception type: {ExceptionType}", 
                caseId, ex.GetType().Name);
        }
    }

    private async Task CreateAuditLogAsync(string action, string description, string entityType, Guid entityId, AuditLogLevel level)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Action = action,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                Level = level,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for {Action}", action);
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class CreateCaseWithFilesRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> FileIds { get; set; } = new();
}

public class CreateCaseRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCaseRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public CaseStatus? Status { get; set; }
    public DateTime? HearingDate { get; set; }
}

public class CaseCreationResponse
{
    public bool Success { get; set; }
    public string CaseId { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Error { get; set; }
}