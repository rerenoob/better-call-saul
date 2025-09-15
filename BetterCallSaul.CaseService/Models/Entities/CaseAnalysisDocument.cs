using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace BetterCallSaul.CaseService.Models.Entities;

[BsonIgnoreExtraElements]
public class CaseAnalysisDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("caseId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CaseId { get; set; } = string.Empty;

    [BsonElement("documentId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string DocumentId { get; set; } = string.Empty;

    [BsonElement("analysisText")]
    public string AnalysisText { get; set; } = string.Empty;

    [BsonElement("scores")]
    public AnalysisScores Scores { get; set; } = new AnalysisScores();

    [BsonElement("legalIssues")]
    public List<string> LegalIssues { get; set; } = new List<string>();

    [BsonElement("potentialDefenses")]
    public List<string> PotentialDefenses { get; set; } = new List<string>();

    [BsonElement("evidenceEvaluation")]
    public EvidenceEvaluation EvidenceEvaluation { get; set; } = new EvidenceEvaluation();

    [BsonElement("timelineAnalysis")]
    public TimelineAnalysis TimelineAnalysis { get; set; } = new TimelineAnalysis();

    [BsonElement("recommendations")]
    public List<Recommendation> Recommendations { get; set; } = new List<Recommendation>();

    [BsonElement("status")]
    public string Status { get; set; } = "Pending";

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("processingTime")]
    public string ProcessingTime { get; set; } = string.Empty;

    [BsonElement("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class AnalysisScores
{
    [BsonElement("viability")]
    public double Viability { get; set; }

    [BsonElement("confidence")]
    public double Confidence { get; set; }
}

public class EvidenceEvaluation
{
    [BsonElement("strengthScore")]
    public double StrengthScore { get; set; }

    [BsonElement("strongEvidence")]
    public List<string> StrongEvidence { get; set; } = new List<string>();

    [BsonElement("weakEvidence")]
    public List<string> WeakEvidence { get; set; } = new List<string>();

    [BsonElement("evidenceGaps")]
    public List<string> EvidenceGaps { get; set; } = new List<string>();

    [BsonElement("additionalNeeded")]
    public List<string> AdditionalNeeded { get; set; } = new List<string>();
}

public class TimelineAnalysis
{
    [BsonElement("events")]
    public List<TimelineEvent> Events { get; set; } = new List<TimelineEvent>();

    [BsonElement("chronologicalIssues")]
    public List<string> ChronologicalIssues { get; set; } = new List<string>();

    [BsonElement("criticalTimePoints")]
    public List<string> CriticalTimePoints { get; set; } = new List<string>();
}

public class TimelineEvent
{
    [BsonElement("date")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Date { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("significance")]
    public string Significance { get; set; } = string.Empty;

    [BsonElement("confidence")]
    public double Confidence { get; set; }
}

public class Recommendation
{
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    [BsonElement("rationale")]
    public string Rationale { get; set; } = string.Empty;

    [BsonElement("priority")]
    public string Priority { get; set; } = "Medium";

    [BsonElement("impactScore")]
    public double ImpactScore { get; set; }
}