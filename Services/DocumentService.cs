using better_call_saul.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace better_call_saul.Services;

public class DocumentService : IDocumentService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly ILoggerService _logger;
    private readonly FileStorageOptions _options;
    private readonly string[] _supportedFileTypes;

    public DocumentService(IFileStorageService fileStorageService, 
                          ITextExtractionService textExtractionService,
                          ILoggerService logger,
                          IOptions<FileStorageOptions> options)
    {
        _fileStorageService = fileStorageService;
        _textExtractionService = textExtractionService;
        _logger = logger;
        _options = options.Value;
        _supportedFileTypes = _options.AllowedExtensions;
    }

    public async Task<FileUploadResult> UploadDocumentAsync(IBrowserFile file, string userId)
    {
        var result = new FileUploadResult
        {
            OriginalFileName = file.Name,
            FileSize = file.Size
        };

        try
        {
            var validationResult = await ValidateFileAsync(file);
            if (!validationResult.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = validationResult.ErrorMessage ?? "File validation failed";
                return result;
            }

            // Convert IBrowserFile to IFormFile for storage service
            var formFile = await ConvertToFormFileAsync(file);
            var filePath = await _fileStorageService.SaveFileAsync(formFile, userId);

            // Extract text from the document
            var extractedContent = await _textExtractionService.ExtractTextAsync(filePath, file.Name);
            
            result.Success = true;
            result.FilePath = filePath;
            _logger.LogInformation($"Document uploaded successfully: {file.Name} by user {userId}");
            
            if (extractedContent.ExtractionSuccessful)
            {
                _logger.LogInformation($"Text extraction successful: {extractedContent.ExtractedText.Length} characters extracted");
            }
            else
            {
                _logger.LogWarning($"Text extraction failed: {extractedContent.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError($"Document upload failed: {ex.Message}");
        }

        return result;
    }

    public Task<FileValidationResult> ValidateFileAsync(IBrowserFile file)
    {
        var result = new FileValidationResult { IsValid = true };

        if (file == null || file.Size == 0)
        {
            result.IsValid = false;
            result.ErrorMessage = "File is empty or not selected.";
            return Task.FromResult(result);
        }

        if (file.Size > _options.MaxFileSize)
        {
            result.IsValid = false;
            result.ErrorMessage = $"File size ({file.Size / 1024 / 1024}MB) exceeds maximum allowed size of {_options.MaxFileSize / 1024 / 1024}MB.";
            return Task.FromResult(result);
        }

        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!_supportedFileTypes.Contains(extension))
        {
            result.IsValid = false;
            result.ErrorMessage = $"File extension '{extension}' is not supported. Supported types: {string.Join(", ", _supportedFileTypes)}";
            return Task.FromResult(result);
        }

        return Task.FromResult(result);
    }

    public string[] GetSupportedFileTypes()
    {
        return _supportedFileTypes;
    }

    private async Task<IFormFile> ConvertToFormFileAsync(IBrowserFile browserFile)
    {
        var stream = browserFile.OpenReadStream(_options.MaxFileSize);
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return new FormFile(
            memoryStream,
            0,
            memoryStream.Length,
            browserFile.Name,
            browserFile.Name
        )
        {
            Headers = new HeaderDictionary(),
            ContentType = browserFile.ContentType
        };
    }
}