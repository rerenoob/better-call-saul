using Microsoft.AspNetCore.Mvc;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BetterCallSaul.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentViewerController : ControllerBase
{
    private readonly BetterCallSaulContext _context;
    private readonly ILogger<DocumentViewerController> _logger;

    public DocumentViewerController(BetterCallSaulContext context, ILogger<DocumentViewerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("document/{documentId}/content")]
    public async Task<ActionResult<DocumentViewerData>> GetDocumentContent(Guid documentId)
    {
        try
        {
            var document = await _context.Documents
                .Include(d => d.ExtractedText)
                .ThenInclude(et => et!.Pages)
                .Include(d => d.Case)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            var annotations = await _context.DocumentAnnotations
                .Where(a => a.DocumentId == documentId)
                .OrderBy(a => a.PageNumber)
                .ThenBy(a => a.CreatedAt)
                .ToListAsync();

            var viewerData = new DocumentViewerData
            {
                DocumentId = document.Id,
                FileName = document.FileName,
                FileType = document.FileType,
                FileSize = document.FileSize,
                CaseId = document.CaseId,
                CaseNumber = document.Case?.CaseNumber ?? "",
                IsProcessed = document.IsProcessed,
                ProcessedAt = document.ProcessedAt,
                ExtractedText = document.ExtractedText?.FullText,
                PageCount = document.ExtractedText?.PageCount ?? 0,
                Pages = document.ExtractedText?.Pages?.Select(p => new DocumentPageData
                {
                    PageNumber = p.PageNumber,
                    Text = p.Text,
                    Confidence = p.Confidence,
                    BoundingBoxes = p.TextBlocks?.Select(tb => new TextBoundingBox
                    {
                        Text = tb.Text,
                        X = tb.BoundingBox?.X ?? 0,
                        Y = tb.BoundingBox?.Y ?? 0,
                        Width = tb.BoundingBox?.Width ?? 0,
                        Height = tb.BoundingBox?.Height ?? 0,
                        Confidence = tb.Confidence
                    }).ToArray() ?? Array.Empty<TextBoundingBox>()
                }).ToArray() ?? Array.Empty<DocumentPageData>(),
                Annotations = annotations.Select(a => new DocumentAnnotationData
                {
                    Id = a.Id,
                    PageNumber = a.PageNumber,
                    Type = a.Type.ToString(),
                    Content = a.Content,
                    X = a.X,
                    Y = a.Y,
                    Width = a.Width,
                    Height = a.Height,
                    Color = a.Color,
                    CreatedBy = a.CreatedBy.ToString(),
                    CreatedAt = a.CreatedAt
                }).ToArray()
            };

            return Ok(viewerData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document content for document {DocumentId}", documentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("document/{documentId}/annotations")]
    public async Task<ActionResult<DocumentAnnotationData>> AddAnnotation(Guid documentId, [FromBody] CreateAnnotationRequest request)
    {
        try
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var annotation = new DocumentAnnotation
            {
                DocumentId = documentId,
                PageNumber = request.PageNumber,
                Type = Enum.Parse<AnnotationType>(request.Type),
                Content = request.Content,
                X = request.X,
                Y = request.Y,
                Width = request.Width,
                Height = request.Height,
                Color = request.Color,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentAnnotations.Add(annotation);
            await _context.SaveChangesAsync();

            var response = new DocumentAnnotationData
            {
                Id = annotation.Id,
                PageNumber = annotation.PageNumber,
                Type = annotation.Type.ToString(),
                Content = annotation.Content,
                X = annotation.X,
                Y = annotation.Y,
                Width = annotation.Width,
                Height = annotation.Height,
                Color = annotation.Color,
                CreatedBy = annotation.CreatedBy.ToString(),
                CreatedAt = annotation.CreatedAt
            };

            return Created($"/api/documentviewer/document/{documentId}/annotations/{annotation.Id}", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating annotation for document {DocumentId}", documentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("document/{documentId}/annotations/{annotationId}")]
    public async Task<ActionResult<DocumentAnnotationData>> UpdateAnnotation(Guid documentId, Guid annotationId, [FromBody] UpdateAnnotationRequest request)
    {
        try
        {
            var annotation = await _context.DocumentAnnotations
                .FirstOrDefaultAsync(a => a.Id == annotationId && a.DocumentId == documentId);

            if (annotation == null)
            {
                return NotFound(new { error = "Annotation not found" });
            }

            var userId = GetCurrentUserId();
            if (annotation.CreatedBy != userId)
            {
                return Forbid("You can only edit your own annotations");
            }

            annotation.Content = request.Content;
            annotation.Color = request.Color;
            annotation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new DocumentAnnotationData
            {
                Id = annotation.Id,
                PageNumber = annotation.PageNumber,
                Type = annotation.Type.ToString(),
                Content = annotation.Content,
                X = annotation.X,
                Y = annotation.Y,
                Width = annotation.Width,
                Height = annotation.Height,
                Color = annotation.Color,
                CreatedBy = annotation.CreatedBy.ToString(),
                CreatedAt = annotation.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating annotation {AnnotationId} for document {DocumentId}", annotationId, documentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("document/{documentId}/annotations/{annotationId}")]
    public async Task<ActionResult> DeleteAnnotation(Guid documentId, Guid annotationId)
    {
        try
        {
            var annotation = await _context.DocumentAnnotations
                .FirstOrDefaultAsync(a => a.Id == annotationId && a.DocumentId == documentId);

            if (annotation == null)
            {
                return NotFound(new { error = "Annotation not found" });
            }

            var userId = GetCurrentUserId();
            if (annotation.CreatedBy != userId)
            {
                return Forbid("You can only delete your own annotations");
            }

            _context.DocumentAnnotations.Remove(annotation);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting annotation {AnnotationId} for document {DocumentId}", annotationId, documentId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("document/{documentId}/search")]
    public async Task<ActionResult<DocumentSearchResult>> SearchDocument(Guid documentId, [FromQuery] string query, [FromQuery] bool caseSensitive = false)
    {
        try
        {
            var document = await _context.Documents
                .Include(d => d.ExtractedText)
                .ThenInclude(et => et!.Pages)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Search query cannot be empty" });
            }

            var results = new List<SearchMatch>();
            var stringComparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Search in full text
            if (!string.IsNullOrEmpty(document.ExtractedText?.FullText))
            {
                var text = document.ExtractedText.FullText;
                var index = 0;
                while ((index = text.IndexOf(query, index, stringComparison)) != -1)
                {
                    var contextStart = Math.Max(0, index - 50);
                    var contextEnd = Math.Min(text.Length, index + query.Length + 50);
                    var context = text.Substring(contextStart, contextEnd - contextStart);

                    results.Add(new SearchMatch
                    {
                        PageNumber = 0, // Would need to determine page from index
                        Position = index,
                        Context = context,
                        MatchText = query
                    });

                    index += query.Length;
                }
            }

            // Search in individual pages for more precise page numbers
            if (document.ExtractedText?.Pages != null)
            {
                foreach (var page in document.ExtractedText.Pages)
                {
                    if (string.IsNullOrEmpty(page.Text)) continue;

                    var pageIndex = 0;
                    while ((pageIndex = page.Text.IndexOf(query, pageIndex, stringComparison)) != -1)
                    {
                        var contextStart = Math.Max(0, pageIndex - 50);
                        var contextEnd = Math.Min(page.Text.Length, pageIndex + query.Length + 50);
                        var context = page.Text.Substring(contextStart, contextEnd - contextStart);

                        results.Add(new SearchMatch
                        {
                            PageNumber = page.PageNumber,
                            Position = pageIndex,
                            Context = context,
                            MatchText = query
                        });

                        pageIndex += query.Length;
                    }
                }
            }

            var searchResult = new DocumentSearchResult
            {
                DocumentId = documentId,
                Query = query,
                CaseSensitive = caseSensitive,
                TotalMatches = results.Count,
                Matches = results.Take(100).ToArray() // Limit to first 100 matches
            };

            return Ok(searchResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching document {DocumentId} for query '{Query}'", documentId, query);
            return StatusCode(500, new { error = "Internal server error" });
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
}

// DTOs for document viewer
public class DocumentViewerData
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ExtractedText { get; set; }
    public int PageCount { get; set; }
    public DocumentPageData[] Pages { get; set; } = Array.Empty<DocumentPageData>();
    public DocumentAnnotationData[] Annotations { get; set; } = Array.Empty<DocumentAnnotationData>();
}

public class DocumentPageData
{
    public int PageNumber { get; set; }
    public string? Text { get; set; }
    public double Confidence { get; set; }
    public TextBoundingBox[] BoundingBoxes { get; set; } = Array.Empty<TextBoundingBox>();
}

public class TextBoundingBox
{
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Confidence { get; set; }
}

public class DocumentAnnotationData
{
    public Guid Id { get; set; }
    public int PageNumber { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Color { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateAnnotationRequest
{
    public int PageNumber { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Color { get; set; } = "#ffff00";
}

public class UpdateAnnotationRequest
{
    public string Content { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class DocumentSearchResult
{
    public Guid DocumentId { get; set; }
    public string Query { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; }
    public int TotalMatches { get; set; }
    public SearchMatch[] Matches { get; set; } = Array.Empty<SearchMatch>();
}

public class SearchMatch
{
    public int PageNumber { get; set; }
    public int Position { get; set; }
    public string Context { get; set; } = string.Empty;
    public string MatchText { get; set; } = string.Empty;
}