using BetterCallSaul.Core.Enums;
using BetterCallSaul.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models.Entities;

public class Case : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string CaseNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public CaseStatus Status { get; set; } = CaseStatus.New;
    public CaseType Type { get; set; } = CaseType.Criminal;
    
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    [MaxLength(100)]
    public string? Court { get; set; }

    [MaxLength(100)]
    public string? Judge { get; set; }

    public DateTime? FiledDate { get; set; }
    public DateTime? HearingDate { get; set; }
    public DateTime? TrialDate { get; set; }

    public decimal? SuccessProbability { get; set; }
    public decimal? EstimatedValue { get; set; }

    // Foreign keys
    public Guid UserId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual ICollection<CaseAnalysis> CaseAnalyses { get; set; } = new List<CaseAnalysis>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}