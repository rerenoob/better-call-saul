using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models.Entities;

public class MatchingCriteria
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public decimal Weight { get; set; } = 1.0m;

    [MaxLength(50)]
    public string? Field { get; set; }

    [MaxLength(100)]
    public string? Operator { get; set; } = "Contains";

    public string? Value { get; set; }

    public decimal Threshold { get; set; } = 0.7m;

    public bool IsActive { get; set; } = true;

    public int Priority { get; set; } = 1;

    [MaxLength(100)]
    public string? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; } = "System";
}