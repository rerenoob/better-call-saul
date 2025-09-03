using better_call_saul.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace better_call_saul.Services;

public class TextExtractionService : ITextExtractionService
{
    private readonly ILoggerService _logger;

    public TextExtractionService(ILoggerService logger)
    {
        _logger = logger;
    }

    public async Task<DocumentContent> ExtractTextAsync(string filePath, string fileName)
    {
        var result = new DocumentContent
        {
            FileType = Path.GetExtension(fileName).ToLowerInvariant(),
            FileSizeBytes = new FileInfo(filePath).Length
        };

        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            switch (result.FileType)
            {
                case ".pdf":
                    await ExtractFromPdfAsync(filePath, result);
                    break;
                case ".docx":
                    await ExtractFromDocxAsync(filePath, result);
                    break;
                default:
                    throw new NotSupportedException($"File type {result.FileType} is not supported for text extraction");
            }

            result.ExtractionSuccessful = true;
            _logger.LogInformation($"Text extraction successful for {fileName}: {result.PageCount} pages, {result.ExtractedText.Length} characters");
        }
        catch (Exception ex)
        {
            result.ExtractionSuccessful = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError($"Text extraction failed for {fileName}: {ex.Message}");
        }

        return result;
    }

    public bool IsSupported(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".pdf" or ".docx";
    }

    private async Task ExtractFromPdfAsync(string filePath, DocumentContent result)
    {
        await Task.Run(() =>
        {
            using var pdfReader = new PdfReader(filePath);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            result.PageCount = pdfDocument.GetNumberOfPages();
            
            var textBuilder = new System.Text.StringBuilder();
            var strategy = new LocationTextExtractionStrategy();

            for (int page = 1; page <= result.PageCount; page++)
            {
                var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
                textBuilder.AppendLine(pageText);
                
                // Add page separator for better readability
                if (page < result.PageCount)
                {
                    textBuilder.AppendLine("\n--- Page " + page + " ---\n");
                }
            }

            result.ExtractedText = textBuilder.ToString();
        });
    }

    private async Task ExtractFromDocxAsync(string filePath, DocumentContent result)
    {
        await Task.Run(() =>
        {
            using var wordDocument = WordprocessingDocument.Open(filePath, false);
            var body = wordDocument.MainDocumentPart?.Document.Body;
            
            if (body == null)
            {
                throw new InvalidOperationException("Invalid DOCX document structure");
            }

            var textBuilder = new System.Text.StringBuilder();
            
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                var paragraphText = GetTextFromParagraph(paragraph);
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    textBuilder.AppendLine(paragraphText);
                }
            }

            result.ExtractedText = textBuilder.ToString();
            result.PageCount = 1; // DOCX doesn't have explicit page count in the same way as PDF
        });
    }

    private string GetTextFromParagraph(Paragraph paragraph)
    {
        var textBuilder = new System.Text.StringBuilder();
        
        foreach (var run in paragraph.Elements<Run>())
        {
            foreach (var text in run.Elements<Text>())
            {
                textBuilder.Append(text.Text);
            }
        }

        return textBuilder.ToString().Trim();
    }
}