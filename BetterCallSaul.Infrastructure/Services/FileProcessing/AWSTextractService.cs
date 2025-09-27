using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon.S3;
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
    private readonly IAmazonS3? _s3Client;
    private readonly AWSOptions _awsOptions;
    private readonly ILogger<AWSTextractService> _logger;
    private const long MaxSyncFileSize = 5 * 1024 * 1024; // 5MB for synchronous processing
    private const int MaxSyncPages = 3000; // Textract sync limit

    public AWSTextractService(
        IOptions<AWSOptions> awsOptions,
        ILogger<AWSTextractService> logger)
    {
        _awsOptions = awsOptions.Value;
        _logger = logger;

        // Only initialize AWS clients if AWS is configured
        if (!string.IsNullOrEmpty(_awsOptions.Textract.Region))
        {
            var region = Amazon.RegionEndpoint.GetBySystemName(_awsOptions.Textract.Region);
            _textractClient = new AmazonTextractClient(region);
            _s3Client = new AmazonS3Client(region);
        }
    }

    public async Task<TextExtractionResult> ExtractTextAsync(string filePath, string fileName)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            bool isS3Path = IsS3Path(filePath);
            long fileSize = 0;

            // Check if file exists (local) or is accessible (S3)
            if (isS3Path)
            {
                if (_s3Client == null)
                {
                    return new TextExtractionResult
                    {
                        Success = false,
                        ErrorMessage = "AWS S3 client is not configured for S3 path processing",
                        Status = TextExtractionStatus.Failed,
                        FileName = fileName
                    };
                }

                var (bucketName, s3Key) = ParseS3Path(filePath);
                try
                {
                    var metadata = await _s3Client.GetObjectMetadataAsync(bucketName, s3Key);
                    fileSize = metadata.ContentLength;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "S3 object not found or not accessible: s3://{Bucket}/{Key}", bucketName, s3Key);
                    return new TextExtractionResult
                    {
                        Success = false,
                        ErrorMessage = "File not found in S3",
                        Status = TextExtractionStatus.Failed,
                        FileName = fileName
                    };
                }
            }
            else
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
                fileSize = fileInfo.Length;
            }

            // Check if Textract client is initialized
            if (_textractClient == null)
            {
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = "AWS Textract is not configured",
                    Status = TextExtractionStatus.Failed,
                    FileName = fileName,
                    FileSize = fileSize
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
                    FileSize = fileSize
                };
            }

            // Use AWS Textract to extract text
            TextExtractionResult result;
            if (isS3Path)
            {
                // For S3 files, use S3 location directly
                if (fileSize <= MaxSyncFileSize)
                {
                    result = await ProcessS3SynchronouslyAsync(filePath, fileName, fileSize);
                }
                else
                {
                    // For async processing, fall back to sync processing if async is not configured
                    _logger.LogWarning("Large S3 file {FileName} ({FileSize} bytes) requires async processing. Falling back to sync processing with size limit warning.",
                        fileName, fileSize);
                    result = await ProcessS3SynchronouslyAsync(filePath, fileName, fileSize);
                }
            }
            else
            {
                // For local files, read from file system
                await using var fileStream = File.OpenRead(filePath);

                // Choose processing method based on file size
                if (fileSize <= MaxSyncFileSize)
                {
                    result = await ProcessSynchronouslyAsync(fileStream, fileName, fileSize);
                }
                else
                {
                    // For async processing, fall back to sync processing if S3 upload is not configured
                    _logger.LogWarning("Large file {FileName} ({FileSize} bytes) requires async processing. Falling back to sync processing with size limit warning.",
                        fileName, fileSize);
                    result = await ProcessSynchronouslyAsync(fileStream, fileName, fileSize);
                }
            }

            result.ProcessingTime = DateTime.UtcNow - startTime;
            result.FileName = fileName;
            result.FileSize = fileSize;
            
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


    private async Task<TextExtractionResult> ProcessS3SynchronouslyAsync(string s3Path, string fileName, long fileSize)
    {
        try
        {
            var (bucketName, s3Key) = ParseS3Path(s3Path);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            string extractedText;
            List<TextPage> pages;
            double confidence;

            // Use AnalyzeDocument for PDF files, DetectDocumentText for images
            if (extension == ".pdf")
            {
                var analyzeDocumentRequest = new AnalyzeDocumentRequest
                {
                    Document = new AmazonTextractDocument
                    {
                        S3Object = new Amazon.Textract.Model.S3Object
                        {
                            Bucket = bucketName,
                            Name = s3Key
                        }
                    }
                    // No FeatureTypes for basic text extraction from PDFs
                };

                _logger.LogInformation("Sending S3 PDF document to AWS Textract AnalyzeDocument: s3://{Bucket}/{Key} ({Size} bytes)", bucketName, s3Key, fileSize);
                var analyzeResponse = await _textractClient!.AnalyzeDocumentAsync(analyzeDocumentRequest);

                extractedText = ExtractTextFromAnalyzeDocumentResult(analyzeResponse);
                pages = ExtractPagesFromAnalyzeDocumentResult(analyzeResponse);
                confidence = CalculateOverallConfidence(analyzeResponse);
            }
            else
            {
                var detectDocumentTextRequest = new DetectDocumentTextRequest
                {
                    Document = new AmazonTextractDocument
                    {
                        S3Object = new Amazon.Textract.Model.S3Object
                        {
                            Bucket = bucketName,
                            Name = s3Key
                        }
                    }
                };

                _logger.LogInformation("Sending S3 image document to AWS Textract DetectDocumentText: s3://{Bucket}/{Key} ({Size} bytes)", bucketName, s3Key, fileSize);
                var detectResponse = await _textractClient!.DetectDocumentTextAsync(detectDocumentTextRequest);

                extractedText = ExtractTextFromResponse(detectResponse);
                pages = ExtractPagesFromResponse(detectResponse);
                confidence = CalculateOverallConfidence(detectResponse);
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("AWS Textract returned empty text for S3 file: s3://{Bucket}/{Key}", bucketName, s3Key);
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = "No text could be extracted from the document",
                    Status = TextExtractionStatus.Failed,
                    FileName = fileName,
                    FileSize = fileSize,
                    Metadata = new Dictionary<string, object>
                    {
                        ["extraction_method"] = "aws_textract_s3_sync",
                        ["s3_bucket"] = bucketName,
                        ["s3_key"] = s3Key,
                        ["extraction_empty"] = true
                    }
                };
            }

            _logger.LogInformation("AWS Textract successfully extracted {CharCount} characters from S3 file: s3://{Bucket}/{Key}",
                extractedText.Length, bucketName, s3Key);

            return new TextExtractionResult
            {
                Success = true,
                ExtractedText = extractedText,
                ConfidenceScore = confidence,
                Status = TextExtractionStatus.Success,
                Metadata = new Dictionary<string, object>
                {
                    ["file_type"] = Path.GetExtension(fileName).ToLowerInvariant(),
                    ["extraction_method"] = "aws_textract_s3_sync",
                    ["aws_operation_type"] = extension == ".pdf" ? "analyze_document" : "detect_document_text",
                    ["s3_bucket"] = bucketName,
                    ["s3_key"] = s3Key
                },
                Pages = pages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessS3SynchronouslyAsync for file: {FileName} at {S3Path}", fileName, s3Path);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"S3 synchronous processing error: {ex.Message}",
                Status = TextExtractionStatus.ProcessingError,
                FileName = fileName,
                FileSize = fileSize
            };
        }
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

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            string extractedText;
            List<TextPage> pages;
            double confidence;

            // Use AnalyzeDocument for PDF files, DetectDocumentText for images
            if (extension == ".pdf")
            {
                var analyzeDocumentRequest = new AnalyzeDocumentRequest
                {
                    Document = new AmazonTextractDocument
                    {
                        Bytes = new MemoryStream(documentBytes)
                    }
                    // No FeatureTypes for basic text extraction from PDFs
                };

                _logger.LogInformation("Sending PDF document to AWS Textract AnalyzeDocument: {FileName} ({Size} bytes)", fileName, documentBytes.Length);
                var analyzeResponse = await _textractClient!.AnalyzeDocumentAsync(analyzeDocumentRequest);

                extractedText = ExtractTextFromAnalyzeDocumentResult(analyzeResponse);
                pages = ExtractPagesFromAnalyzeDocumentResult(analyzeResponse);
                confidence = CalculateOverallConfidence(analyzeResponse);
            }
            else
            {
                var detectDocumentTextRequest = new DetectDocumentTextRequest
                {
                    Document = new AmazonTextractDocument
                    {
                        Bytes = new MemoryStream(documentBytes)
                    }
                };

                _logger.LogInformation("Sending image document to AWS Textract DetectDocumentText: {FileName} ({Size} bytes)", fileName, documentBytes.Length);
                var detectResponse = await _textractClient!.DetectDocumentTextAsync(detectDocumentTextRequest);

                extractedText = ExtractTextFromResponse(detectResponse);
                pages = ExtractPagesFromResponse(detectResponse);
                confidence = CalculateOverallConfidence(detectResponse);
            }

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
                    ["aws_operation_type"] = extension == ".pdf" ? "analyze_document" : "detect_document_text"
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

    private string ExtractTextFromAnalyzeDocumentResult(AnalyzeDocumentResponse response)
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

    private List<TextPage> ExtractPagesFromAnalyzeDocumentResult(AnalyzeDocumentResponse response)
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

    private double CalculateOverallConfidence(AnalyzeDocumentResponse response)
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

    private bool IsS3Path(string path)
    {
        return path.StartsWith("s3://", StringComparison.OrdinalIgnoreCase);
    }

    private (string bucketName, string key) ParseS3Path(string s3Path)
    {
        if (!IsS3Path(s3Path))
            throw new ArgumentException("Path is not a valid S3 path", nameof(s3Path));

        var uri = new Uri(s3Path);
        var bucketName = uri.Host;
        var key = uri.AbsolutePath.TrimStart('/');

        return (bucketName, key);
    }
}