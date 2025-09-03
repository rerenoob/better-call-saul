# API Documentation - Better Call Saul

This document describes the internal service architecture and API patterns used in the Better Call Saul application.

## Table of Contents
- [Service Architecture](#service-architecture)
- [Interface Definitions](#interface-definitions)
- [Data Models](#data-models)
- [Configuration](#configuration)
- [Error Handling](#error-handling)
- [Usage Examples](#usage-examples)

## Service Architecture

The application follows a service-oriented architecture with dependency injection. All services are registered in `Program.cs` and follow interface-based design.

### Service Registration

```csharp
// Program.cs - Service configuration
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICaseService, CaseService>();
builder.Services.AddScoped<ITextExtractionService, TextExtractionService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<ILoggerService, LoggerService>();
builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
```

## Interface Definitions

### IAzureOpenAIService

Analyzes text content using Azure OpenAI GPT-4.

```csharp
public interface IAzureOpenAIService
{
    Task<AIAnalysisResult> AnalyzeDocumentAsync(string content, string documentName);
    Task<string> GenerateAnalysisAsync(string content);
}
```

**Methods:**
- `AnalyzeDocumentAsync`: Main analysis method returns structured results
- `GenerateAnalysisAsync`: Raw text generation for custom prompts

### IDocumentService

Manages document operations and persistence.

```csharp
public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, long fileSize);
    Task<Document> GetDocumentAsync(Guid id);
    Task<List<Document>> GetUserDocumentsAsync(string userId);
    Task DeleteDocumentAsync(Guid id);
    Task<DocumentContent> ExtractDocumentContentAsync(Guid documentId);
}
```

### ICaseService

Manages legal cases and their associated documents.

```csharp
public interface ICaseService
{
    Task<Case> CreateCaseAsync(Case caseEntity);
    Task<Case> GetCaseAsync(Guid id);
    Task<List<Case>> GetUserCasesAsync(string userId);
    Task<Case> UpdateCaseAsync(Case caseEntity);
    Task DeleteCaseAsync(Guid id);
    Task<CaseAnalysis> AnalyzeCaseAsync(Guid caseId);
}
```

### ITextExtractionService

Extracts text from various document formats.

```csharp
public interface ITextExtractionService
{
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
    Task<string> ExtractTextFromWordAsync(Stream wordStream);
    Task<string> ExtractTextFromTextFileAsync(Stream textStream);
    Task<string> ExtractTextAsync(Stream fileStream, string contentType);
}
```

### IFileStorageService

Handles physical file storage operations.

```csharp
public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName);
    Task<Stream> GetFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
}
```

## Data Models

### Document Model

```csharp
public class Document : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CaseId { get; set; }
    public Case? Case { get; set; }
}
```

### Case Model

```csharp
public class Case : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.Draft;
    public string? ClientName { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public List<Document> Documents { get; set; } = new();
    public List<CaseAnalysis> Analyses { get; set; } = new();
}

public enum CaseStatus
{
    Draft,
    InReview,
    Analyzed,
    Completed,
    Archived
}
```

### AI Analysis Result

```csharp
public class AIAnalysisResult
{
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> Risks { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public string RawResponse { get; set; } = string.Empty;
}

public class CaseAnalysis : BaseEntity
{
    public Guid CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public string Summary { get; set; } = string.Empty;
    public string AnalysisJson { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
```

## Configuration

### AppSettings Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BetterCallSaul;Trusted_Connection=true;"
  },
  "FileStorage": {
    "BasePath": "wwwroot/uploads",
    "MaxFileSize": 10000000,
    "AllowedExtensions": [".pdf", ".docx", ".doc", ".txt", ".jpg", ".jpeg", ".png"]
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4",
    "MaxTokens": 1000,
    "Temperature": 0.3
  }
}
```

### Configuration Classes

```csharp
public class FileStorageOptions
{
    public string BasePath { get; set; } = string.Empty;
    public long MaxFileSize { get; set; }
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}

public class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public double Temperature { get; set; }
}
```

## Error Handling

### Custom Exceptions

```csharp
public class FileValidationException : Exception
{
    public FileValidationException(string message) : base(message) { }
}

public class AnalysisException : Exception
{
    public AnalysisException(string message) : base(message) { }
}

public class StorageException : Exception
{
    public StorageException(string message) : base(message) { }
}
```

### Service Error Patterns

Services should:
1. Validate input parameters
2. Handle specific domain exceptions
3. Provide meaningful error messages
4. Log errors appropriately
5. Use async/await pattern consistently

## Usage Examples

### Document Upload Example

```csharp
// In a Razor component or controller
var documentService = services.GetRequiredService<IDocumentService>();
var fileStorageService = services.GetRequiredService<IFileStorageService>();

// Upload document
var document = await documentService.UploadDocumentAsync(
    fileStream, 
    fileName, 
    contentType, 
    fileSize
);

// Extract content
var content = await documentService.ExtractDocumentContentAsync(document.Id);
```

### AI Analysis Example

```csharp
var openAIService = services.GetRequiredService<IAzureOpenAIService>();
var caseService = services.GetRequiredService<ICaseService>();

// Analyze case documents
var analysisResult = await openAIService.AnalyzeDocumentAsync(
    documentContent, 
    document.FileName
);

// Save analysis to case
var caseAnalysis = await caseService.AnalyzeCaseAsync(caseId);
```

### File Storage Example

```csharp
var fileStorage = services.GetRequiredService<IFileStorageService>();

// Save file
var storagePath = await fileStorage.SaveFileAsync(fileStream, fileName);

// Retrieve file
var fileStream = await fileStorage.GetFileAsync(storagePath);
```

## Extension Points

### Custom File Storage

Implement `IFileStorageService` for alternative storage:
- Azure Blob Storage
- AWS S3
- Google Cloud Storage
- Database storage

### Custom AI Providers

Implement `IAzureOpenAIService` for:
- Different OpenAI models
- Alternative AI providers
- Local AI models
- Hybrid analysis approaches

### Custom Text Extraction

Implement `ITextExtractionService` for:
- Additional file formats
- OCR capabilities
- Specialized text extraction

## Performance Considerations

1. **File Uploads**: Stream files instead of loading into memory
2. **AI Calls**: Implement caching for repeated analyses
3. **Database**: Use eager loading for related entities
4. **Memory**: Dispose streams and resources properly
5. **Async**: Use async/await for all I/O operations

## Security Considerations

1. **File Uploads**: Validate file types and sizes
2. **AI Data**: Don't send sensitive information to external services
3. **Database**: Use parameterized queries via EF Core
4. **Authentication**: Use ASP.NET Core Identity for user management
5. **Authorization**: Implement role-based access control

---

This API documentation provides guidance for developers working with the Better Call Saul service layer. For frontend component documentation, refer to the individual Razor component files.