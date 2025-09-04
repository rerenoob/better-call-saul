using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace BetterCallSaul.Infrastructure.Validators;

public class FileUploadValidator
{
    private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
    {
        {".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } }},
        {".doc", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }},
        {".docx", new List<byte[]> { 
            new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 
            new byte[] { 0x50, 0x4B, 0x05, 0x06 }, 
            new byte[] { 0x50, 0x4B, 0x07, 0x08 } 
        }},
        {".txt", new List<byte[]>()}
    };

    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
    private static readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".txt" };

    public static (bool IsValid, Dictionary<string, string> Errors) ValidateFile(IFormFile file)
    {
        var errors = new Dictionary<string, string>();

        // Check file size
        if (file.Length == 0)
        {
            errors["file"] = "File is empty";
        }
        else if (file.Length > MaxFileSize)
        {
            errors["file"] = $"File size exceeds the maximum limit of {MaxFileSize / (1024 * 1024)}MB";
        }

        // Check file extension
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(fileExtension) || !_allowedExtensions.Contains(fileExtension))
        {
            errors["extension"] = $"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}";
        }

        // Validate file signature if we have signatures for this extension
        if (_fileSignatures.ContainsKey(fileExtension) && _fileSignatures[fileExtension].Count > 0)
        {
            using var stream = file.OpenReadStream();
            if (!IsValidFileSignature(stream, fileExtension))
            {
                errors["signature"] = "File signature does not match the expected file type";
            }
        }

        return (!errors.Any(), errors);
    }

    private static bool IsValidFileSignature(Stream stream, string fileExtension)
    {
        var signatures = _fileSignatures[fileExtension];
        var headerBytes = new byte[signatures[0].Length];
        var bytesRead = stream.Read(headerBytes, 0, headerBytes.Length);
        stream.Position = 0;

        if (bytesRead < headerBytes.Length)
            return false;

        return signatures.Any(signature => 
            headerBytes.Take(signature.Length).SequenceEqual(signature));
    }
}