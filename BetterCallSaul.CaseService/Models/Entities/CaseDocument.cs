using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace BetterCallSaul.CaseService.Models.Entities;

[BsonIgnoreExtraElements]
public class CaseDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("caseNumber")]
    public string CaseNumber { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "New";

    [BsonElement("type")]
    public string Type { get; set; } = "Criminal";

    [BsonElement("priority")]
    public string Priority { get; set; } = "Medium";

    [BsonElement("court")]
    public string? Court { get; set; }

    [BsonElement("judge")]
    public string? Judge { get; set; }

    [BsonElement("dates")]
    public CaseDates Dates { get; set; } = new CaseDates();

    [BsonElement("probability")]
    public CaseProbability Probability { get; set; } = new CaseProbability();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class CaseDates
{
    [BsonElement("filed")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? Filed { get; set; }

    [BsonElement("hearing")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? Hearing { get; set; }

    [BsonElement("trial")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? Trial { get; set; }
}

public class CaseProbability
{
    [BsonElement("success")]
    public double Success { get; set; }

    [BsonElement("estimated_value")]
    public decimal EstimatedValue { get; set; }
}