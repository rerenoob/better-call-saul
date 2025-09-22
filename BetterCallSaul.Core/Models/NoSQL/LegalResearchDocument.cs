using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BetterCallSaul.Core.Models.NoSQL;

public class LegalResearchDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    [BsonElement("citation")]
    public string Citation { get; set; } = string.Empty;
    
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;
    
    [BsonElement("summary")]
    public string? Summary { get; set; }
    
    [BsonElement("court")]
    public string Court { get; set; } = string.Empty;
    
    [BsonElement("jurisdiction")]
    public string Jurisdiction { get; set; } = string.Empty;
    
    [BsonElement("decisionDate")]
    public DateTime DecisionDate { get; set; }
    
    [BsonElement("docketNumber")]
    public string? DocketNumber { get; set; }
    
    [BsonElement("judge")]
    public string? Judge { get; set; }
    
    [BsonElement("fullText")]
    public string? FullText { get; set; }
    
    [BsonElement("citationFormat")]
    public string? CitationFormat { get; set; }
    
    [BsonElement("relevanceScore")]
    public decimal RelevanceScore { get; set; }
    
    [BsonElement("type")]
    public LegalDocumentType Type { get; set; }
    
    [BsonElement("source")]
    public string Source { get; set; } = "CourtListener";
    
    [BsonElement("metadata")]
    public LegalSearchMetadata Metadata { get; set; } = new();
    
    [BsonElement("indexedAt")]
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("retrievedAt")]
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class LegalSearchMetadata
{
    [BsonElement("keywords")]
    public List<string> Keywords { get; set; } = new();
    
    [BsonElement("topics")]
    public List<string> Topics { get; set; } = new();
    
    [BsonElement("searchQueries")]
    public List<string> SearchQueries { get; set; } = new();
    
    [BsonElement("citedByCount")]
    public int CitedByCount { get; set; }
    
    [BsonElement("citesCases")]
    public List<string> CitesCases { get; set; } = new();
    
    [BsonElement("relatedStatutes")]
    public List<string> RelatedStatutes { get; set; } = new();
    
    [BsonElement("precedentialValue")]
    public PrecedentialValue PrecedentialValue { get; set; }
    
    [BsonElement("practiceAreas")]
    public List<string> PracticeAreas { get; set; } = new();
}

public class CaseMatchDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    [BsonElement("caseId")]
    public Guid CaseId { get; set; }
    
    [BsonElement("userId")]
    public Guid UserId { get; set; }
    
    [BsonElement("matchedCaseId")]
    public ObjectId MatchedCaseId { get; set; }
    
    [BsonElement("similarityScore")]
    public double SimilarityScore { get; set; }
    
    [BsonElement("matchingCriteria")]
    public MatchingCriteriaInfo MatchingCriteria { get; set; } = new();
    
    [BsonElement("analysisText")]
    public string? AnalysisText { get; set; }
    
    [BsonElement("confidence")]
    public double Confidence { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class MatchingCriteriaInfo
{
    [BsonElement("factPatterns")]
    public List<string> FactPatterns { get; set; } = new();
    
    [BsonElement("legalIssues")]
    public List<string> LegalIssues { get; set; } = new();
    
    [BsonElement("jurisdiction")]
    public string? Jurisdiction { get; set; }
    
    [BsonElement("timeframe")]
    public DateRange? Timeframe { get; set; }
    
    [BsonElement("caseType")]
    public string? CaseType { get; set; }
    
    [BsonElement("outcome")]
    public string? Outcome { get; set; }
    
    [BsonElement("keyTerms")]
    public List<string> KeyTerms { get; set; } = new();
}

public class DateRange
{
    [BsonElement("startDate")]
    public DateTime? StartDate { get; set; }
    
    [BsonElement("endDate")]
    public DateTime? EndDate { get; set; }
}

public enum LegalDocumentType
{
    CaseOpinion,
    Statute,
    Regulation,
    Ordinance,
    CourtRule,
    ConstitutionalProvision,
    TreatyAgreement,
    Other
}

public enum PrecedentialValue
{
    Binding,
    Persuasive,
    Informational,
    Superseded,
    Unknown
}