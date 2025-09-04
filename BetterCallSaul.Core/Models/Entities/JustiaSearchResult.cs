using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models.Entities;

public class JustiaSearchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Summary { get; set; }

    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Jurisdiction { get; set; }

    [MaxLength(100)]
    public string? Court { get; set; }

    public DateTime? DecisionDate { get; set; }

    [MaxLength(500)]
    public string? Url { get; set; }

    [MaxLength(50)]
    public string? Citation { get; set; }

    [MaxLength(100)]
    public string? Type { get; set; }

    public decimal RelevanceScore { get; set; }

    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string? Database { get; set; } = "Justia";
}