using System.Text.Json;
using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public class AzureFormRecognizerService : ITextExtractionService
{
    private readonly FormRecognizerOptions _options;
    private readonly ILogger<AzureFormRecognizerService> _logger;
    private readonly DocumentAnalysisClient _client;

    public AzureFormRecognizerService(
        IOptions<FormRecognizerOptions> options,
        ILogger<AzureFormRecognizerService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            throw new InvalidOperationException("Azure Form Recognizer endpoint and API key must be configured");
        }

        _client = new DocumentAnalysisClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey));
    }

    public async Task<TextExtractionResult> ExtractTextAsync(string filePath, string fileName)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            if (!File.Exists(filePath))
            {
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = "File not found",
                    Status = TextExtractionStatus.Failed,
                    FileName = fileName
                };
            }

            var fileInfo = new FileInfo(filePath);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (!await SupportsFileTypeAsync(fileName))
            {
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = $"File type not supported: {extension}",
                    Status = TextExtractionStatus.UnsupportedFormat,
                    FileName = fileName,
                    FileSize = fileInfo.Length
                };
            }

            // Use Azure Form Recognizer to extract text
            await using var fileStream = File.OpenRead(filePath);
            
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                _options.ModelId,
                fileStream);

            AnalyzeResult result = operation.Value;

            var extractedText = ExtractTextFromResult(result);
            var pages = ExtractPagesFromResult(result);
            var confidence = CalculateOverallConfidence(result);

            return new TextExtractionResult
            {
                Success = true,
                ExtractedText = extractedText,
                ConfidenceScore = confidence,
                ProcessingTime = DateTime.UtcNow - startTime,
                FileName = fileName,
                FileSize = fileInfo.Length,
                Status = TextExtractionStatus.Success,
                Metadata = new Dictionary<string, object>
                {
                    ["file_type"] = extension,
                    ["extraction_method"] = "azure_form_recognizer",
                    ["model_id"] = _options.ModelId ?? "prebuilt-document",
                    ["azure_operation_id"] = operation.Id ?? "unknown"
                },
                Pages = pages
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Form Recognizer error for file: {FileName}. Status: {StatusCode}, ErrorCode: {ErrorCode}", 
                fileName, ex.Status, ex.ErrorCode);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"Azure Form Recognizer error: {ex.Message} (Status: {ex.Status}, Code: {ex.ErrorCode})",
                Status = TextExtractionStatus.ProcessingError,
                FileName = fileName,
                ProcessingTime = DateTime.UtcNow - startTime,
                Metadata = new Dictionary<string, object>
                {
                    ["azure_error_code"] = ex.ErrorCode ?? "unknown",
                    ["azure_status_code"] = (int)ex.Status,
                    ["azure_operation_failed"] = true
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error extracting text from file: {FileName}. Exception type: {ExceptionType}", 
                fileName, ex.GetType().Name);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"Critical extraction error: {ex.Message}",
                Status = TextExtractionStatus.ProcessingError,
                FileName = fileName,
                ProcessingTime = DateTime.UtcNow - startTime,
                Metadata = new Dictionary<string, object>
                {
                    ["exception_type"] = ex.GetType().Name,
                    ["exception_stack_trace"] = ex.StackTrace ?? "No stack trace"
                }
            };
        }
    }

    public async Task<TextExtractionResult> ExtractTextFromBytesAsync(byte[] fileContent, string fileName)
    {
        var tempFilePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFilePath, fileContent);
            return await ExtractTextAsync(tempFilePath, fileName);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    public Task<bool> SupportsFileTypeAsync(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var supportedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".tiff", ".bmp" };
        return Task.FromResult(supportedExtensions.Contains(extension));
    }

    public async Task<DocumentText> ProcessDocumentAsync(string filePath, Guid documentId)
    {
        var extractionResult = await ExtractTextAsync(filePath, Path.GetFileName(filePath));

        if (!extractionResult.Success)
        {
            throw new InvalidOperationException(extractionResult.ErrorMessage ?? "Text extraction failed");
        }

        return new DocumentText
        {
            DocumentId = documentId,
            FullText = extractionResult.ExtractedText,
            ConfidenceScore = extractionResult.ConfidenceScore,
            PageCount = extractionResult.Pages?.Count ?? 1,
            CharacterCount = extractionResult.ExtractedText?.Length ?? 0,
            Language = "en", // Azure Form Recognizer can detect language, but we default to English
            ExtractionMetadata = extractionResult.Metadata,
            Pages = extractionResult.Pages
        };
    }

    private string ExtractTextFromResult(AnalyzeResult result)
    {
        if (result?.Paragraphs == null)
            return string.Empty;

        var paragraphs = result.Paragraphs
            .OrderBy(p => p.BoundingRegions.Count > 0 ? p.BoundingRegions[0].PageNumber : 1)
            .ThenBy(p => p.BoundingRegions.Count > 0 && p.BoundingRegions[0].BoundingPolygon.Count > 0 ? p.BoundingRegions[0].BoundingPolygon[0].Y : 0)
            .Select(p => p.Content);

        return string.Join("\n\n", paragraphs);
    }

    private List<TextPage> ExtractPagesFromResult(AnalyzeResult result)
    {
        var pages = new List<TextPage>();

        if (result?.Pages == null)
            return pages;

        foreach (var page in result.Pages)
        {
            var pageText = string.Join("\n", 
                page.Lines?.Select(l => l.Content) ?? Enumerable.Empty<string>());

            pages.Add(new TextPage
            {
                PageNumber = page.PageNumber,
                Text = pageText,
                Confidence = _options.ConfidenceThreshold, // Azure Form Recognizer v4.x doesn't provide per-line confidence
                PageMetadata = new Dictionary<string, object>
                {
                    ["angle"] = page.Angle ?? 0.0,
                    ["width"] = page.Width ?? 0.0,
                    ["height"] = page.Height ?? 0.0,
                    ["unit"] = page.Unit.ToString() ?? "pixel",
                    ["line_count"] = page.Lines?.Count ?? 0,
                    ["word_count"] = page.Lines?.Sum(l => l.Content.Split(' ').Length) ?? 0
                }
            });
        }

        return pages;
    }

    private double CalculateOverallConfidence(AnalyzeResult result)
    {
        if (result?.Pages == null || !result.Pages.Any())
            return _options.ConfidenceThreshold;

        // For Azure Form Recognizer v4.x, we use the configured confidence threshold
        // since per-line confidence scores are not provided in this version
        return _options.ConfidenceThreshold;
    }
}