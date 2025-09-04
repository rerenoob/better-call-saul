using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models;

public class CourtOpinion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Citation { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CaseName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Court { get; set; } = string.Empty;

    public DateTime DecisionDate { get; set; }

    [MaxLength(50)]
    public string? DocketNumber { get; set; }

    [MaxLength(100)]
    public string? Author { get; set; }

    [Required]
    public string OpinionText { get; set; } = string.Empty;

    [MaxLength(20)]
    public string OpinionType { get; set; } = "Majority";

    public int PageCount { get; set; }

    public string? Headnotes { get; set; }

    public string? Syllabus { get; set; }

    public string? Holding { get; set; }

    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

    public string Source { get; set; } = "CourtListener";

    public decimal RelevanceScore { get; set; }
}