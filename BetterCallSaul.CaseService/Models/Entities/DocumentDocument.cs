using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace BetterCallSaul.CaseService.Models.Entities;

[BsonIgnoreExtraElements]
public class DocumentDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("caseId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CaseId { get; set; } = string.Empty;

    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("originalFileName")]
    public string OriginalFileName { get; set; } = string.Empty;

    [BsonElement("fileType")]
    public string FileType { get; set; } = string.Empty;

    [BsonElement("fileSize")]
    public long FileSize { get; set; }

    [BsonElement("storagePath")]
    public string StoragePath { get; set; } = string.Empty;

    [BsonElement("documentType")]
    public string DocumentType { get; set; } = "Other";

    [BsonElement("status")]
    public string Status { get; set; } = "Uploaded";

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("isProcessed")]
    public bool IsProcessed { get; set; } = false;

    [BsonElement("processedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ProcessedAt { get; set; }

    [BsonElement("uploadedBy")]
    public string UploadedBy { get; set; } = string.Empty;

    [BsonElement("extractedText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExtractedText? ExtractedText { get; set; }

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

public class ExtractedText
{
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("confidence")]
    public double Confidence { get; set; }

    [BsonElement("pages")]
    public List<TextPage> Pages { get; set; } = new List<TextPage>();

    [BsonElement("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class TextPage
{
    [BsonElement("pageNumber")]
    public int PageNumber { get; set; }

    [BsonElement("text")]
    public string Text { get; set; } = string.Empty;

    [BsonElement("confidence")]
    public double Confidence { get; set; }

    [BsonElement("dimensions")]
    public PageDimensions Dimensions { get; set; } = new PageDimensions();
}

public class PageDimensions
{
    [BsonElement("width")]
    public double Width { get; set; }

    [BsonElement("height")]
    public double Height { get; set; }

    [BsonElement("unit")]
    public string Unit { get; set; } = "pixels";
}