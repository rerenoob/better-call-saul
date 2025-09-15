using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace BetterCallSaul.CaseService.Models.Entities;

[BsonIgnoreExtraElements]
public class LegalResearchDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("caseId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CaseId { get; set; } = string.Empty;

    [BsonElement("searchQuery")]
    public string SearchQuery { get; set; } = string.Empty;

    [BsonElement("searchResults")]
    public SearchResults SearchResults { get; set; } = new SearchResults();

    [BsonElement("relevantCases")]
    public List<RelevantCase> RelevantCases { get; set; } = new List<RelevantCase>();

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class SearchResults
{
    [BsonElement("courtListener")]
    public List<SearchResult> CourtListener { get; set; } = new List<SearchResult>();

    [BsonElement("justia")]
    public List<SearchResult> Justia { get; set; } = new List<SearchResult>();
}

public class SearchResult
{
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("summary")]
    public string Summary { get; set; } = string.Empty;

    [BsonElement("relevanceScore")]
    public double RelevanceScore { get; set; }

    [BsonElement("source")]
    public string Source { get; set; } = string.Empty;
}

public class RelevantCase
{
    [BsonElement("caseTitle")]
    public string CaseTitle { get; set; } = string.Empty;

    [BsonElement("citation")]
    public string Citation { get; set; } = string.Empty;

    [BsonElement("relevanceScore")]
    public double RelevanceScore { get; set; }

    [BsonElement("summary")]
    public string Summary { get; set; } = string.Empty;

    [BsonElement("keyTakeaways")]
    public List<string> KeyTakeaways { get; set; } = new List<string>();
}