using BetterCallSaul.Core.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace BetterCallSaul.Infrastructure.Services.FileProcessing;

public interface IFileUploadService
{
    Task<UploadResult> UploadFileAsync(IFormFile file, Guid userId, string uploadSessionId);
    Task<UploadResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId);
    Task<bool> LinkDocumentToCaseAsync(Guid documentId, Guid caseId);
    Task<bool> ValidateFileAsync(IFormFile file);
    Task<string> GenerateUniqueFileNameAsync(string originalFileName);
    Task<string> StoreFileAsync(IFormFile file, string fileName);
    Task<bool> DeleteFileAsync(string filePath);
    Task<long> GetTotalUploadSizeForUserAsync(Guid userId, TimeSpan timeWindow);
}