using System.ComponentModel.DataAnnotations;

namespace BetterCallSaul.Core.Models.Entities;

public class ViabilityAssessment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CaseId { get; set; }
    public virtual Case Case { get; set; } = null!;
    
    public double ViabilityScore { get; set; }
    
    public string ConfidenceLevel { get; set; } = string.Empty;
    
    public string Reasoning { get; set; } = string.Empty;
    
    public string[] StrengthFactors { get; set; } = Array.Empty<string>();
    
    public string[] WeaknessFactors { get; set; } = Array.Empty<string>();
    
    public string RecommendedStrategy { get; set; } = string.Empty;
    
    public string CaseFacts { get; set; } = string.Empty;
    
    public string[] Charges { get; set; } = Array.Empty<string>();
    
    public string[] Evidence { get; set; } = Array.Empty<string>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid CreatedBy { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
    
    public Dictionary<string, object>? Metadata { get; set; }
}