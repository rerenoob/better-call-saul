using BetterCallSaul.Core.Enums;

namespace BetterCallSaul.Core.Models.Entities;

public class SuccessPrediction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public Guid AnalysisId { get; set; }
    public double SuccessProbability { get; set; } // 0-100%
    public double ConfidenceInterval { get; set; } // 0-1
    public List<PredictionFactor> KeyFactors { get; set; } = new();
    public RiskAssessment RiskAssessment { get; set; } = new();
    public List<Recommendation> StrategyRecommendations { get; set; } = new();
    public List<HistoricalComparison> HistoricalComparisons { get; set; } = new();
    public string? PredictionRationale { get; set; }
    public PredictionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class PredictionFactor
{
    public string? Factor { get; set; }
    public double Weight { get; set; } // 0-1
    public ImpactDirection Impact { get; set; }
    public string? Explanation { get; set; }
}

public class RiskAssessment
{
    public List<Risk> IdentifiedRisks { get; set; } = new();
    public double OverallRiskScore { get; set; } // 0-1
    public List<MitigationStrategy> MitigationStrategies { get; set; } = new();
}

public class Risk
{
    public string? Description { get; set; }
    public double Probability { get; set; } // 0-1
    public double Impact { get; set; } // 0-1
    public RiskLevel Level { get; set; }
}

public class MitigationStrategy
{
    public string? Strategy { get; set; }
    public double Effectiveness { get; set; } // 0-1
    public PriorityLevel Priority { get; set; }
}

public class HistoricalComparison
{
    public string? CasePattern { get; set; }
    public double SimilarityScore { get; set; } // 0-1
    public string? Outcome { get; set; }
    public string? KeyLessons { get; set; }
}

public enum ImpactDirection
{
    Positive,
    Negative,
    Neutral
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum PredictionStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}