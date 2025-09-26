using Amazon.Textract;
using Amazon.Textract.Model;
using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using AmazonTextractDocument = Amazon.Textract.Model.Document;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public class AWSTextractService : ITextExtractionService
{
    private readonly IAmazonTextract? _textractClient;
    private readonly TextractOptions _options;
    private readonly ILogger<AWSTextractService> _logger;
    private const long MaxSyncFileSize = 5 * 1024 * 1024; // 5MB for synchronous processing
    private const int MaxSyncPages = 3000; // Textract sync limit

    public AWSTextractService(
        IOptions<AWSOptions> awsOptions,
        ILogger<AWSTextractService> logger)
    {
        _options = awsOptions.Value.Textract;
        _logger = logger;
        
        // Only initialize Textract client if AWS is configured
        if (!string.IsNullOrEmpty(_options.Region))
        {
            var region = Amazon.RegionEndpoint.GetBySystemName(_options.Region);
            _textractClient = new AmazonTextractClient(region);
        }
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

            // Check if Textract client is initialized
            if (_textractClient == null)
            {
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = "AWS Textract is not configured",
                    Status = TextExtractionStatus.Failed,
                    FileName = fileName,
                    FileSize = fileInfo.Length
                };
            }

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

            // Use AWS Textract to extract text
            await using var fileStream = File.OpenRead(filePath);

            // Choose processing method based on file size
            TextExtractionResult result;
            if (fileInfo.Length <= MaxSyncFileSize)
            {
                result = await ProcessSynchronouslyAsync(fileStream, fileName, fileInfo.Length);
            }
            else
            {
                // For async processing, fall back to sync processing if S3 upload is not configured
                _logger.LogWarning("Large file {FileName} ({FileSize} bytes) requires async processing. Falling back to sync processing with size limit warning.",
                    fileName, fileInfo.Length);
                result = await ProcessSynchronouslyAsync(fileStream, fileName, fileInfo.Length);
            }

            result.ProcessingTime = DateTime.UtcNow - startTime;
            result.FileName = fileName;
            result.FileSize = fileInfo.Length;
            
            return result;
        }
        catch (AmazonTextractException ex)
        {
            _logger.LogError(ex, "AWS Textract error for file: {FileName}. Status: {StatusCode}, ErrorCode: {ErrorCode}", 
                fileName, ex.StatusCode, ex.ErrorCode);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"AWS Textract error: {ex.Message} (Status: {ex.StatusCode}, Code: {ex.ErrorCode})",
                Status = TextExtractionStatus.ProcessingError,
                FileName = fileName,
                ProcessingTime = DateTime.UtcNow - startTime,
                Metadata = new Dictionary<string, object>
                {
                    ["aws_error_code"] = ex.ErrorCode ?? "unknown",
                    ["aws_status_code"] = (int)ex.StatusCode,
                    ["aws_operation_failed"] = true
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
        var supportedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
        return Task.FromResult(supportedExtensions.Contains(extension));
    }


    private async Task<TextExtractionResult> ProcessSynchronouslyAsync(Stream documentStream, string fileName, long fileSize)
    {
        try
        {
            // Read the document stream into a byte array
            using var memoryStream = new MemoryStream();
            documentStream.Position = 0; // Ensure we're at the beginning
            await documentStream.CopyToAsync(memoryStream);
            var documentBytes = memoryStream.ToArray();

            if (documentBytes.Length == 0)
            {
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = "Document stream is empty",
                    Status = TextExtractionStatus.Failed,
                    FileName = fileName,
                    FileSize = fileSize
                };
            }

            var detectDocumentTextRequest = new DetectDocumentTextRequest
            {
                Document = new AmazonTextractDocument
                {
                    Bytes = new MemoryStream(documentBytes)
                }
            };

            _logger.LogInformation("Sending document to AWS Textract: {FileName} ({Size} bytes)", fileName, documentBytes.Length);
            var response = await _textractClient!.DetectDocumentTextAsync(detectDocumentTextRequest);

            var extractedText = ExtractTextFromResponse(response);
            var pages = ExtractPagesFromResponse(response);
            var confidence = CalculateOverallConfidence(response);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("AWS Textract returned empty text for file: {FileName}", fileName);
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = "No text could be extracted from the document",
                    Status = TextExtractionStatus.Failed,
                    FileName = fileName,
                    FileSize = fileSize,
                    Metadata = new Dictionary<string, object>
                    {
                        ["extraction_method"] = "aws_textract_sync",
                        ["blocks_found"] = response.Blocks?.Count ?? 0,
                        ["extraction_empty"] = true
                    }
                };
            }

            _logger.LogInformation("AWS Textract successfully extracted {CharCount} characters from {FileName}",
                extractedText.Length, fileName);

            return new TextExtractionResult
            {
                Success = true,
                ExtractedText = extractedText,
                ConfidenceScore = confidence,
                Status = TextExtractionStatus.Success,
                Metadata = new Dictionary<string, object>
                {
                    ["file_type"] = Path.GetExtension(fileName).ToLowerInvariant(),
                    ["extraction_method"] = "aws_textract_sync",
                    ["aws_operation_type"] = "synchronous_detect_document_text",
                    ["block_count"] = response.Blocks?.Count ?? 0
                },
                Pages = pages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessSynchronouslyAsync for file: {FileName}", fileName);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"Synchronous processing error: {ex.Message}",
                Status = TextExtractionStatus.ProcessingError,
                FileName = fileName,
                FileSize = fileSize
            };
        }
    }

    private async Task<TextExtractionResult> ProcessAsynchronouslyAsync(Stream documentStream, string fileName, long fileSize)
    {
        var startDocumentAnalysisRequest = new StartDocumentAnalysisRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new S3Object
                {
                    Bucket = "temp-bucket", // This will be replaced with actual stream handling
                    Name = fileName
                }
            },
            FeatureTypes = new List<string> { "TABLES", "FORMS" }
        };

        // For async processing, we need to upload to S3 first
        // This is a simplified implementation - in production, you'd upload to S3
        // and then use the S3 object location for Textract analysis
        _logger.LogWarning("Async Textract processing requires S3 upload first. Using sync fallback for file: {FileName}", fileName);
        
        // Fall back to synchronous processing for now
        documentStream.Position = 0;
        return await ProcessSynchronouslyAsync(documentStream, fileName, fileSize);
    }

    private async Task<GetDocumentAnalysisResponse> WaitForAnalysisCompletionAsync(string jobId, int maxRetries = 30, int delaySeconds = 5)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var getAnalysisRequest = new GetDocumentAnalysisRequest { JobId = jobId };
            var analysisResponse = await _textractClient!.GetDocumentAnalysisAsync(getAnalysisRequest);

            if (analysisResponse.JobStatus == JobStatus.SUCCEEDED)
            {
                return analysisResponse;
            }
            else if (analysisResponse.JobStatus == JobStatus.FAILED)
            {
                throw new AmazonTextractException("Textract analysis job failed");
            }

            _logger.LogInformation("Textract job {JobId} status: {Status}, waiting {Delay}s...", 
                jobId, analysisResponse.JobStatus, delaySeconds);
            
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        throw new TimeoutException("Textract analysis job timed out");
    }

    private string ExtractTextFromResponse(DetectDocumentTextResponse response)
    {
        if (response?.Blocks == null)
            return string.Empty;

        var lines = response.Blocks
            .Where(b => b.BlockType == BlockType.LINE)
            .OrderBy(b => b.Page > 0 ? b.Page : 1)
            .ThenBy(b => b.Geometry?.BoundingBox?.Top ?? 0)
            .Select(b => b.Text);

        return string.Join("\n", lines);
    }

    private string ExtractTextFromAnalysisResult(GetDocumentAnalysisResponse response)
    {
        if (response?.Blocks == null)
            return string.Empty;

        var lines = response.Blocks
            .Where(b => b.BlockType == BlockType.LINE)
            .OrderBy(b => b.Page > 0 ? b.Page : 1)
            .ThenBy(b => b.Geometry?.BoundingBox?.Top ?? 0)
            .Select(b => b.Text);

        return string.Join("\n", lines);
    }

    private List<TextPage> ExtractPagesFromResponse(DetectDocumentTextResponse response)
    {
        var pages = new List<TextPage>();

        if (response?.Blocks == null)
            return pages;

        var pageGroups = response.Blocks
            .Where(b => b.BlockType == BlockType.PAGE || b.Page > 0)
            .GroupBy(b => b.Page > 0 ? b.Page : 1);

        foreach (var pageGroup in pageGroups)
        {
            var pageNumber = pageGroup.Key;
            var pageBlocks = pageGroup.ToList();
            
            var pageLines = pageBlocks
                .Where(b => b.BlockType == BlockType.LINE)
                .Select(b => b.Text);
            var pageText = string.Join("\n", pageLines);

            var confidenceValues = pageBlocks
                .Where(b => b.Confidence > 0)
                .Select(b => (double)b.Confidence)
                .ToList();
            
            var confidence = confidenceValues.Any() ? confidenceValues.Average() / 100.0 : 0.8;

            pages.Add(new TextPage
            {
                PageNumber = pageNumber,
                Text = pageText,
                Confidence = confidence,
                PageMetadata = new Dictionary<string, object>
                {
                    ["block_count"] = pageBlocks.Count,
                    ["line_count"] = pageBlocks.Count(b => b.BlockType == BlockType.LINE),
                    ["word_count"] = pageText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                    ["aws_page_confidence"] = confidence
                }
            });
        }

        return pages;
    }

    private List<TextPage> ExtractPagesFromAnalysisResult(GetDocumentAnalysisResponse response)
    {
        var pages = new List<TextPage>();

        if (response?.Blocks == null)
            return pages;

        var pageGroups = response.Blocks
            .Where(b => b.BlockType == BlockType.PAGE || b.Page > 0)
            .GroupBy(b => b.Page > 0 ? b.Page : 1);

        foreach (var pageGroup in pageGroups)
        {
            var pageNumber = pageGroup.Key;
            var pageBlocks = pageGroup.ToList();
            
            var pageLines = pageBlocks
                .Where(b => b.BlockType == BlockType.LINE)
                .Select(b => b.Text);
            var pageText = string.Join("\n", pageLines);

            var confidenceValues = pageBlocks
                .Where(b => b.Confidence > 0)
                .Select(b => (double)b.Confidence)
                .ToList();
            
            var confidence = confidenceValues.Any() ? confidenceValues.Average() / 100.0 : 0.8;

            pages.Add(new TextPage
            {
                PageNumber = pageNumber,
                Text = pageText,
                Confidence = confidence,
                PageMetadata = new Dictionary<string, object>
                {
                    ["block_count"] = pageBlocks.Count,
                    ["line_count"] = pageBlocks.Count(b => b.BlockType == BlockType.LINE),
                    ["word_count"] = pageText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                    ["aws_page_confidence"] = confidence
                }
            });
        }

        return pages;
    }

    private double CalculateOverallConfidence(DetectDocumentTextResponse response)
    {
        if (response?.Blocks == null || !response.Blocks.Any())
            return 0.8; // Default confidence

        var confidences = response.Blocks
            .Where(b => b.Confidence > 0)
            .Select(b => (double)b.Confidence)
            .ToList();

        if (!confidences.Any())
            return 0.8;

        return confidences.Average() / 100.0; // Convert 0-100 to 0-1
    }

    private double CalculateOverallConfidence(GetDocumentAnalysisResponse response)
    {
        if (response?.Blocks == null || !response.Blocks.Any())
            return 0.8; // Default confidence

        var confidences = response.Blocks
            .Where(b => b.Confidence > 0)
            .Select(b => (double)b.Confidence)
            .ToList();

        if (!confidences.Any())
            return 0.8;

        return confidences.Average() / 100.0; // Convert 0-100 to 0-1
    }
}