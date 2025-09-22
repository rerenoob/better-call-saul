using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using BetterCallSaul.Core.Enums;

namespace BetterCallSaul.Core.Models.NoSQL;

public class CaseDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    [BsonElement("caseId")]
    public Guid CaseId { get; set; }
    
    [BsonElement("userId")]
    public Guid UserId { get; set; }
    
    [BsonElement("documents")]
    public List<DocumentInfo> Documents { get; set; } = new();
    
    [BsonElement("analyses")]
    public List<CaseAnalysisResult> Analyses { get; set; } = new();
    
    [BsonElement("metadata")]
    public CaseMetadata Metadata { get; set; } = new();
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("version")]
    public int Version { get; set; } = 1;
}

public class DocumentInfo
{
    [BsonElement("id")]
    public Guid Id { get; set; }
    
    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;
    
    [BsonElement("originalFileName")]
    public string? OriginalFileName { get; set; }
    
    [BsonElement("fileType")]
    public string FileType { get; set; } = string.Empty;
    
    [BsonElement("fileSize")]
    public long FileSize { get; set; }
    
    [BsonElement("storagePath")]
    public string? StoragePath { get; set; }
    
    [BsonElement("type")]
    public DocumentType Type { get; set; }
    
    [BsonElement("status")]
    public DocumentStatus Status { get; set; }
    
    [BsonElement("description")]
    public string? Description { get; set; }
    
    [BsonElement("isProcessed")]
    public bool IsProcessed { get; set; }
    
    [BsonElement("processedAt")]
    public DateTime? ProcessedAt { get; set; }
    
    [BsonElement("uploadedById")]
    public Guid? UploadedById { get; set; }
    
    [BsonElement("extractedText")]
    public DocumentTextInfo? ExtractedText { get; set; }
    
    [BsonElement("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class DocumentTextInfo
{
    [BsonElement("id")]
    public Guid Id { get; set; }
    
    [BsonElement("fullText")]
    public string? FullText { get; set; }
    
    [BsonElement("confidenceScore")]
    public double ConfidenceScore { get; set; }
    
    [BsonElement("pageCount")]
    public int PageCount { get; set; }
    
    [BsonElement("characterCount")]
    public long CharacterCount { get; set; }
    
    [BsonElement("language")]
    public string? Language { get; set; }
    
    [BsonElement("extractionMetadata")]
    public Dictionary<string, object>? ExtractionMetadata { get; set; }
    
    [BsonElement("pages")]
    public List<TextPageInfo>? Pages { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class TextPageInfo
{
    [BsonElement("pageNumber")]
    public int PageNumber { get; set; }
    
    [BsonElement("text")]
    public string? Text { get; set; }
    
    [BsonElement("confidence")]
    public double Confidence { get; set; }
    
    [BsonElement("blocks")]
    public List<TextBlockInfo>? Blocks { get; set; }
}

public class TextBlockInfo
{
    [BsonElement("blockType")]
    public string? BlockType { get; set; }
    
    [BsonElement("text")]
    public string? Text { get; set; }
    
    [BsonElement("confidence")]
    public double Confidence { get; set; }
    
    [BsonElement("boundingBox")]
    public BoundingBoxInfo? BoundingBox { get; set; }
}

public class BoundingBoxInfo
{
    [BsonElement("left")]
    public double Left { get; set; }
    
    [BsonElement("top")]
    public double Top { get; set; }
    
    [BsonElement("width")]
    public double Width { get; set; }
    
    [BsonElement("height")]
    public double Height { get; set; }
}

public class CaseAnalysisResult
{
    [BsonElement("id")]
    public Guid Id { get; set; }
    
    [BsonElement("documentId")]
    public Guid DocumentId { get; set; }
    
    [BsonElement("analysisText")]
    public string? AnalysisText { get; set; }
    
    [BsonElement("viabilityScore")]
    public double ViabilityScore { get; set; }
    
    [BsonElement("confidenceScore")]
    public double ConfidenceScore { get; set; }
    
    [BsonElement("keyLegalIssues")]
    public List<string> KeyLegalIssues { get; set; } = new();
    
    [BsonElement("potentialDefenses")]
    public List<string> PotentialDefenses { get; set; } = new();
    
    [BsonElement("evidenceEvaluation")]
    public EvidenceEvaluationInfo EvidenceEvaluation { get; set; } = new();
    
    [BsonElement("timelineAnalysis")]
    public TimelineAnalysisInfo TimelineAnalysis { get; set; } = new();
    
    [BsonElement("recommendations")]
    public List<RecommendationInfo> Recommendations { get; set; } = new();
    
    [BsonElement("status")]
    public AnalysisStatus Status { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }
    
    [BsonElement("processingTime")]
    public TimeSpan ProcessingTime { get; set; }
    
    [BsonElement("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class EvidenceEvaluationInfo
{
    [BsonElement("strengthScore")]
    public double StrengthScore { get; set; }
    
    [BsonElement("strongEvidence")]
    public List<string> StrongEvidence { get; set; } = new();
    
    [BsonElement("weakEvidence")]
    public List<string> WeakEvidence { get; set; } = new();
    
    [BsonElement("evidenceGaps")]
    public List<string> EvidenceGaps { get; set; } = new();
    
    [BsonElement("additionalEvidenceNeeded")]
    public List<string> AdditionalEvidenceNeeded { get; set; } = new();
}

public class TimelineAnalysisInfo
{
    [BsonElement("events")]
    public List<TimelineEventInfo> Events { get; set; } = new();
    
    [BsonElement("chronologicalIssues")]
    public List<string> ChronologicalIssues { get; set; } = new();
    
    [BsonElement("criticalTimePoints")]
    public List<string> CriticalTimePoints { get; set; } = new();
}

public class TimelineEventInfo
{
    [BsonElement("date")]
    public DateTime Date { get; set; }
    
    [BsonElement("description")]
    public string? Description { get; set; }
    
    [BsonElement("significance")]
    public string? Significance { get; set; }
    
    [BsonElement("confidence")]
    public double Confidence { get; set; }
}

public class RecommendationInfo
{
    [BsonElement("action")]
    public string? Action { get; set; }
    
    [BsonElement("rationale")]
    public string? Rationale { get; set; }
    
    [BsonElement("priority")]
    public PriorityLevel Priority { get; set; }
    
    [BsonElement("impactScore")]
    public double ImpactScore { get; set; }
}

public class CaseMetadata
{
    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();
    
    [BsonElement("customFields")]
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    [BsonElement("lastAnalyzedAt")]
    public DateTime? LastAnalyzedAt { get; set; }
    
    [BsonElement("totalDocuments")]
    public int TotalDocuments { get; set; }
    
    [BsonElement("totalAnalyses")]
    public int TotalAnalyses { get; set; }
}