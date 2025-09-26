using BetterCallSaul.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

/// <summary>
/// Composite text extraction service that combines multiple extraction methods
/// for different file types, ensuring comprehensive file support in production
/// </summary>
public class CompositeTextExtractionService : ITextExtractionService
{
    private readonly AWSTextractService _awsTextractService;
    private readonly ILogger<CompositeTextExtractionService> _logger;

    public CompositeTextExtractionService(
        AWSTextractService awsTextractService,
        ILogger<CompositeTextExtractionService> logger)
    {
        _awsTextractService = awsTextractService;
        _logger = logger;
    }

    public async Task<TextExtractionResult> ExtractTextAsync(string filePath, string fileName)
    {
        var startTime = DateTime.UtcNow;
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        try
        {
            _logger.LogInformation("Starting text extraction for file: {FileName} (Type: {Extension})", fileName, extension);

            // Handle plain text files directly
            if (extension == ".txt")
            {
                return await ExtractFromTextFileAsync(filePath, fileName);
            }

            // Handle DOCX files with basic text extraction
            if (extension == ".docx")
            {
                return await ExtractFromDocxFileAsync(filePath, fileName);
            }

            // Use AWS Textract for PDF and image files
            if (await _awsTextractService.SupportsFileTypeAsync(fileName))
            {
                var result = await _awsTextractService.ExtractTextAsync(filePath, fileName);

                if (result.Success)
                {
                    _logger.LogInformation("AWS Textract extraction successful for {FileName}: {CharCount} characters extracted",
                        fileName, result.ExtractedText?.Length ?? 0);
                    return result;
                }
                else
                {
                    _logger.LogError("AWS Textract extraction failed for {FileName}: {Error}. Attempting fallback extraction.",
                        fileName, result.ErrorMessage);

                    // Try fallback extraction for PDFs
                    if (extension == ".pdf")
                    {
                        var fallbackResult = await AttemptFallbackPdfExtractionAsync(filePath, fileName);
                        if (fallbackResult.Success)
                        {
                            _logger.LogInformation("Fallback PDF extraction successful for {FileName}: {CharCount} characters extracted",
                                fileName, fallbackResult.ExtractedText?.Length ?? 0);
                            return fallbackResult;
                        }
                    }

                    // Return original AWS Textract error if fallback also fails
                    return result;
                }
            }

            // Unsupported file type
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"File type not supported: {extension}",
                Status = TextExtractionStatus.UnsupportedFormat,
                FileName = fileName,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in composite text extraction for file: {FileName}", fileName);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"Critical extraction error: {ex.Message}",
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

        // Comprehensive file type support
        var supportedExtensions = new[]
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif", // AWS Textract supported
            ".txt", ".docx" // Direct extraction supported
        };

        return Task.FromResult(supportedExtensions.Contains(extension));
    }

    private async Task<TextExtractionResult> ExtractFromTextFileAsync(string filePath, string fileName)
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
                    FileName = fileName,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }

            var text = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(text))
            {
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = "File is empty or contains no text",
                    Status = TextExtractionStatus.Failed,
                    FileName = fileName,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }

            _logger.LogInformation("Successfully extracted {CharCount} characters from text file: {FileName}",
                text.Length, fileName);

            return new TextExtractionResult
            {
                Success = true,
                ExtractedText = text,
                FileName = fileName,
                FileSize = new FileInfo(filePath).Length,
                ConfidenceScore = 1.0, // Perfect confidence for plain text
                Status = TextExtractionStatus.Success,
                ProcessingTime = DateTime.UtcNow - startTime,
                Pages = new List<TextPage>
                {
                    new TextPage
                    {
                        PageNumber = 1,
                        Text = text,
                        Confidence = 1.0
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from file: {FileName}", fileName);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"Error reading text file: {ex.Message}",
                Status = TextExtractionStatus.ProcessingError,
                FileName = fileName,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task<TextExtractionResult> ExtractFromDocxFileAsync(string filePath, string fileName)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Simple DOCX text extraction using basic ZIP reading
            // Note: This is a basic implementation. For production, consider using a proper library like DocumentFormat.OpenXml

            var text = await ExtractTextFromDocxAsync(filePath);

            if (string.IsNullOrWhiteSpace(text))
            {
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = "No text found in DOCX file",
                    Status = TextExtractionStatus.Failed,
                    FileName = fileName,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }

            _logger.LogInformation("Successfully extracted {CharCount} characters from DOCX file: {FileName}",
                text.Length, fileName);

            return new TextExtractionResult
            {
                Success = true,
                ExtractedText = text,
                FileName = fileName,
                FileSize = new FileInfo(filePath).Length,
                ConfidenceScore = 0.95, // High confidence for DOCX extraction
                Status = TextExtractionStatus.Success,
                ProcessingTime = DateTime.UtcNow - startTime,
                Pages = new List<TextPage>
                {
                    new TextPage
                    {
                        PageNumber = 1,
                        Text = text,
                        Confidence = 0.95
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from DOCX file: {FileName}", fileName);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"Error reading DOCX file: {ex.Message}",
                Status = TextExtractionStatus.ProcessingError,
                FileName = fileName,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task<string> ExtractTextFromDocxAsync(string filePath)
    {
        try
        {
            // DOCX files are ZIP archives containing XML files
            // We'll extract text from the main document part
            using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
            
            // Find the main document part
            var documentEntry = archive.Entries.FirstOrDefault(e => 
                e.FullName.Equals("word/document.xml", StringComparison.OrdinalIgnoreCase));

            if (documentEntry == null)
            {
                _logger.LogWarning("Could not find document.xml in DOCX file: {FilePath}", filePath);
                return string.Empty;
            }

            using var stream = documentEntry.Open();
            using var reader = new StreamReader(stream);
            var xmlContent = await reader.ReadToEndAsync();

            // Simple XML text extraction - remove tags and extract text content
            var text = System.Text.RegularExpressions.Regex.Replace(xmlContent, "<[^>]+>", " ");
            text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("No text content found in DOCX file: {FilePath}", filePath);
                return string.Empty;
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from DOCX file: {FilePath}", filePath);
            return string.Empty;
        }
    }

    private async Task<TextExtractionResult> AttemptFallbackPdfExtractionAsync(string filePath, string fileName)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Attempting fallback PDF text extraction using PdfPig for: {FileName}", fileName);

            var fileInfo = new FileInfo(filePath);
            var extractedText = await ExtractTextFromPdfAsync(filePath);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("Fallback PDF extraction returned empty text for: {FileName}", fileName);
                return new TextExtractionResult
                {
                    Success = false,
                    ErrorMessage = "No text could be extracted from the PDF using fallback method",
                    Status = TextExtractionStatus.Failed,
                    FileName = fileName,
                    FileSize = fileInfo.Length,
                    ProcessingTime = DateTime.UtcNow - startTime,
                    Metadata = new Dictionary<string, object>
                    {
                        ["extraction_method"] = "fallback_pdf_pig",
                        ["aws_textract_failed"] = true,
                        ["fallback_used"] = true,
                        ["extraction_empty"] = true
                    }
                };
            }

            _logger.LogInformation("Fallback PDF extraction successful for {FileName}: {CharCount} characters extracted",
                fileName, extractedText.Length);

            return new TextExtractionResult
            {
                Success = true,
                ExtractedText = extractedText,
                FileName = fileName,
                FileSize = fileInfo.Length,
                ConfidenceScore = 0.7, // Lower confidence for fallback
                Status = TextExtractionStatus.Success,
                ProcessingTime = DateTime.UtcNow - startTime,
                Metadata = new Dictionary<string, object>
                {
                    ["extraction_method"] = "fallback_pdf_pig",
                    ["aws_textract_failed"] = true,
                    ["fallback_used"] = true,
                    ["character_count"] = extractedText.Length,
                    ["file_size_bytes"] = fileInfo.Length
                },
                Pages = ExtractPagesFromPdf(filePath)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback PDF extraction failed for: {FileName}", fileName);
            return new TextExtractionResult
            {
                Success = false,
                ErrorMessage = $"Fallback PDF extraction failed: {ex.Message}",
                Status = TextExtractionStatus.ProcessingError,
                FileName = fileName,
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
    }

    private Task<string> ExtractTextFromPdfAsync(string filePath)
    {
        try
        {
            using var document = PdfDocument.Open(filePath);
            var pages = new List<string>();

            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    pages.Add(pageText.Trim());
                }
            }

            return Task.FromResult(string.Join("\n\n", pages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF using PdfPig: {FilePath}", filePath);
            throw;
        }
    }

    private List<TextPage> ExtractPagesFromPdf(string filePath)
    {
        var pages = new List<TextPage>();

        try
        {
            using var document = PdfDocument.Open(filePath);
            var pageNumber = 1;

            foreach (var page in document.GetPages())
            {
                var pageText = page.Text?.Trim() ?? string.Empty;
                
                pages.Add(new TextPage
                {
                    PageNumber = pageNumber,
                    Text = pageText,
                    Confidence = 0.7, // Lower confidence for fallback extraction
                    PageMetadata = new Dictionary<string, object>
                    {
                        ["word_count"] = pageText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                        ["character_count"] = pageText.Length,
                        ["extraction_method"] = "pdfpig_fallback"
                    }
                });

                pageNumber++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting page information from PDF: {FilePath}", filePath);
            // Return at least one page with empty text if extraction fails
            pages.Add(new TextPage
            {
                PageNumber = 1,
                Text = string.Empty,
                Confidence = 0.1,
                PageMetadata = new Dictionary<string, object>
                {
                    ["extraction_error"] = ex.Message
                }
            });
        }

        return pages;
    }
}