using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models.Entities;

public class CaseMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SourceCaseId { get; set; }

    [Required]
    public Guid MatchedCaseId { get; set; }

    [Required]
    [MaxLength(50)]
    public string MatchedCaseCitation { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string MatchedCaseTitle { get; set; } = string.Empty;

    [Required]
    public decimal SimilarityScore { get; set; }

    [MaxLength(20)]
    public string? MatchType { get; set; } = "Semantic";

    [MaxLength(500)]
    public string? Reasoning { get; set; }

    [MaxLength(100)]
    public string? JurisdictionMatch { get; set; }

    [MaxLength(100)]
    public string? LegalIssueMatch { get; set; }

    public DateTime MatchDate { get; set; } = DateTime.UtcNow;

    public bool IsPrecedent { get; set; }

    public decimal ConfidenceLevel { get; set; } = 0.8m;

    [MaxLength(1000)]
    public string? KeySimilarities { get; set; }

    [MaxLength(1000)]
    public string? KeyDifferences { get; set; }

    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}