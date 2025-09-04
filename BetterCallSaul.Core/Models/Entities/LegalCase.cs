using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models.Entities;

public class LegalCase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Citation { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Summary { get; set; }

    [Required]
    [MaxLength(100)]
    public string Court { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Jurisdiction { get; set; } = string.Empty;

    public DateTime DecisionDate { get; set; }

    [MaxLength(50)]
    public string? DocketNumber { get; set; }

    [MaxLength(100)]
    public string? Judge { get; set; }

    public string? FullText { get; set; }

    [MaxLength(20)]
    public string? CitationFormat { get; set; }

    public decimal RelevanceScore { get; set; }

    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

    public string Source { get; set; } = "CourtListener";
}