using better_call_saul.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace better_call_saul.Services;

public interface IDocumentService
{
    Task<FileUploadResult> UploadDocumentAsync(IBrowserFile file, string userId);
    Task<FileValidationResult> ValidateFileAsync(IBrowserFile file);
    string[] GetSupportedFileTypes();
}