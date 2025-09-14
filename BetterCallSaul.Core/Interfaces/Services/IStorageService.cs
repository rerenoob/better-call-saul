using BetterCallSaul.Core.Models.ServiceResponses;
using Microsoft.AspNetCore.Http;

namespace BetterCallSaul.Core.Interfaces.Services;

public interface IStorageService
{
    Task<StorageResult> UploadFileAsync(IFormFile file, Guid caseId, Guid userId, string uploadSessionId);
    Task<bool> DeleteFileAsync(string storagePath);
    Task<string> GenerateSecureUrlAsync(string storagePath, TimeSpan expiryTime);
    Task<bool> ValidateFileAsync(IFormFile file);
    Task<string> GenerateUniqueFileNameAsync(string originalFileName);
}