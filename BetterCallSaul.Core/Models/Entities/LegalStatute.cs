using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models.Entities;

public class LegalStatute
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Section { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Jurisdiction { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? AmendmentDate { get; set; }

    public string? FullText { get; set; }

    [MaxLength(500)]
    public string? SourceUrl { get; set; }

    public decimal RelevanceScore { get; set; }

    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string? Database { get; set; } = "Justia";
}