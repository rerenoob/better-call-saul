using BetterCallSaul.Core.Models.Entities;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public class MockTextExtractionService : ITextExtractionService
{
    private readonly ILogger<MockTextExtractionService> _logger;
    private readonly Random _random = new();

    public MockTextExtractionService(ILogger<MockTextExtractionService> logger)
    {
        _logger = logger;
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

            // Simulate processing delay
            await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(500, 2000)));

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

            // Read file content and extract text based on file type
            string extractedText;
            double confidence;

            switch (extension)
            {
                case ".txt":
                    extractedText = await File.ReadAllTextAsync(filePath);
                    confidence = 0.95;
                    break;

                case ".pdf":
                    // Simulate PDF text extraction
                    extractedText = SimulatePdfExtraction(filePath);
                    confidence = 0.85;
                    break;

                case ".docx":
                case ".doc":
                    // Simulate Word document extraction
                    extractedText = SimulateWordExtraction(filePath);
                    confidence = 0.88;
                    break;

                default:
                    return new TextExtractionResult
                    {
                        Success = false,
                        ErrorMessage = $"Unsupported file type: {extension}",
                        Status = TextExtractionStatus.UnsupportedFormat,
                        FileName = fileName,
                        FileSize = fileInfo.Length
                    };
            }

            var pages = new List<TextPage>
            {
                new TextPage
                {
                    PageNumber = 1,
                    Text = extractedText,
                    Confidence = confidence,
                    PageMetadata = new Dictionary<string, object>
                    {
                        ["lines"] = extractedText.Split('\n').Length,
                        ["words"] = extractedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                    }
                }
            };

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
                    ["extraction_method"] = "mock",
                    ["simulated"] = true
                },
                Pages = pages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from file: {FileName}", fileName);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"Extraction error: {ex.Message}",
                Status = TextExtractionStatus.ProcessingError,
                FileName = fileName,
                ProcessingTime = DateTime.UtcNow - startTime
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
        var supportedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
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
            Language = "en", // Default to English for mock
            ExtractionMetadata = extractionResult.Metadata,
            Pages = extractionResult.Pages
        };
    }

    private string SimulatePdfExtraction(string filePath)
    {
        // Simulate PDF text extraction with some structure
        var lines = new List<string>
        {
            "LEGAL DOCUMENT ANALYSIS REPORT",
            "",
            "Case Number: 2024-CR-00123",
            "Date: " + DateTime.Now.ToString("yyyy-MM-dd"),
            "",
            "SECTION 1: CASE OVERVIEW",
            "This document contains legal proceedings related to the case.",
            "The defendant is charged with multiple counts of alleged violations.",
            "",
            "SECTION 2: EVIDENCE SUMMARY",
            "- Document exhibits: A through F",
            "- Witness statements: 5 recorded",
            "- Physical evidence: 3 items cataloged",
            "",
            "SECTION 3: LEGAL ANALYSIS",
            "Based on the available evidence, this case presents several",
            "complex legal questions that require thorough examination.",
            "",
            "END OF DOCUMENT"
        };

        return string.Join("\n", lines);
    }

    private string SimulateWordExtraction(string filePath)
    {
        // Simulate Word document extraction
        var lines = new List<string>
        {
            "MEMORANDUM OF LAW",
            "",
            "TO: Legal Team",
            "FROM: Senior Attorney",
            "DATE: " + DateTime.Now.ToString("yyyy-MM-dd"),
            "RE: Case Strategy Meeting",
            "",
            "This memorandum outlines the proposed strategy for the upcoming",
            "hearing scheduled for next month. Key points include:",
            "",
            "1. Motion to suppress certain evidence",
            "2. Witness preparation schedule",
            "3. Legal research priorities",
            "4. Timeline for filings",
            "",
            "Please review and provide feedback by the end of the week.",
            "",
            "Respectfully submitted,",
            "Senior Attorney"
        };

        return string.Join("\n", lines);
    }
}