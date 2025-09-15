using BetterCallSaul.CaseService.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace BetterCallSaul.CaseService.Services.FileProcessing;

public interface IFileUploadService
{
    Task<UploadResult> UploadFileAsync(IFormFile file, string caseId, string userId, string uploadSessionId);
    Task<string> GenerateSecureUrlAsync(string storagePath, TimeSpan expiryTime);
    Task<bool> DeleteFileAsync(string filePath);
    Task<long> GetTotalUploadSizeForUserAsync(string userId, TimeSpan timeWindow);
}

public class UploadResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? StoragePath { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? FileType { get; set; }
    public string? UploadSessionId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorCode { get; set; }
    public Dictionary<string, string>? ValidationErrors { get; set; }
    public string? FileId { get; set; }
}