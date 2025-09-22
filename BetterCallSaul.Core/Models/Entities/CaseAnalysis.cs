using BetterCallSaul.Core.Enums;

namespace BetterCallSaul.Core.Models.Entities;

public class CaseAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public Guid DocumentId { get; set; }
    public string? AnalysisText { get; set; }
    public double ViabilityScore { get; set; } // 0-100%
    public double ConfidenceScore { get; set; } // 0-1
    public List<string> KeyLegalIssues { get; set; } = new();
    public List<string> PotentialDefenses { get; set; } = new();
    public EvidenceEvaluation EvidenceEvaluation { get; set; } = new();
    public TimelineAnalysis TimelineAnalysis { get; set; } = new();
    public List<Recommendation> Recommendations { get; set; } = new();
    public AnalysisStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class EvidenceEvaluation
{
    public double StrengthScore { get; set; } // 0-1
    public List<string> StrongEvidence { get; set; } = new();
    public List<string> WeakEvidence { get; set; } = new();
    public List<string> EvidenceGaps { get; set; } = new();
    public List<string> AdditionalEvidenceNeeded { get; set; } = new();
}

public class TimelineAnalysis
{
    public List<TimelineEvent> Events { get; set; } = new();
    public List<string> ChronologicalIssues { get; set; } = new();
    public List<string> CriticalTimePoints { get; set; } = new();
}

public class TimelineEvent
{
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public string? Significance { get; set; }
    public double Confidence { get; set; }
}

public class Recommendation
{
    public string? Action { get; set; }
    public string? Rationale { get; set; }
    public PriorityLevel Priority { get; set; }
    public double ImpactScore { get; set; } // 0-1
}

