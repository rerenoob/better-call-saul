using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace BetterCallSaul.Infrastructure.Services;

public class DatabaseSeedingService
{
    private readonly BetterCallSaulContext _context;
    private readonly ILogger<DatabaseSeedingService> _logger;

    public DatabaseSeedingService(BetterCallSaulContext context, ILogger<DatabaseSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedRegistrationCodesAsync(int count = 100, int expireDays = 365, string createdBy = "System", string? notes = null)
    {
        _logger.LogInformation("Starting registration code seeding: {Count} codes, expires in {Days} days", count, expireDays);

        // Check if codes with these notes already exist to avoid duplicates
        notes ??= $"Initial seeding - {count} registration codes - {DateTime.UtcNow:yyyy-MM-dd}";
        var existingCount = await _context.RegistrationCodes.CountAsync(rc => rc.Notes == notes);
        
        if (existingCount > 0)
        {
            _logger.LogWarning("Found {ExistingCount} existing codes with notes '{Notes}'. Skipping seeding.", existingCount, notes);
            return;
        }

        var expirationDate = DateTime.UtcNow.AddDays(expireDays);
        var codes = new List<RegistrationCode>();

        _logger.LogInformation("Generating {Count} unique registration codes...", count);

        for (int i = 0; i < count; i++)
        {
            var code = GenerateUniqueCode();
            
            // Ensure code is unique in the database
            while (await _context.RegistrationCodes.AnyAsync(rc => rc.Code == code) || codes.Any(c => c.Code == code))
            {
                code = GenerateUniqueCode();
            }

            codes.Add(new RegistrationCode
            {
                Code = code,
                CreatedBy = createdBy,
                ExpiresAt = expirationDate,
                Notes = notes,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });

            if ((i + 1) % 20 == 0)
            {
                _logger.LogInformation("Generated {Current}/{Total} codes...", i + 1, count);
            }
        }

        _logger.LogInformation("Inserting {Count} registration codes into database...", codes.Count);
        
        await _context.RegistrationCodes.AddRangeAsync(codes);
        var savedCount = await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {SavedCount} registration codes", savedCount);

        // Log some sample codes for verification
        var sampleCodes = codes.Take(5).Select(c => c.Code).ToList();
        _logger.LogInformation("Sample generated codes: {SampleCodes}", string.Join(", ", sampleCodes));
    }

    public async Task<(int Total, int Active, int Used, int Expired)> GetRegistrationCodeStatsAsync()
    {
        var total = await _context.RegistrationCodes.CountAsync();
        var used = await _context.RegistrationCodes.CountAsync(rc => rc.IsUsed);
        var expired = await _context.RegistrationCodes.CountAsync(rc => !rc.IsUsed && rc.ExpiresAt < DateTime.UtcNow);
        var active = total - used - expired;

        return (total, active, used, expired);
    }

    public async Task CleanupExpiredCodesAsync()
    {
        var expiredCodes = await _context.RegistrationCodes
            .Where(rc => rc.ExpiresAt < DateTime.UtcNow && !rc.IsUsed)
            .ToListAsync();

        if (expiredCodes.Any())
        {
            _context.RegistrationCodes.RemoveRange(expiredCodes);
            var removedCount = await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {RemovedCount} expired registration codes", removedCount);
        }
        else
        {
            _logger.LogInformation("No expired registration codes found to clean up");
        }
    }

    private static string GenerateUniqueCode(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        using var rng = RandomNumberGenerator.Create();
        var result = new StringBuilder(length);
        
        var bytes = new byte[4];
        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(bytes);
            var randomIndex = Math.Abs(BitConverter.ToInt32(bytes, 0)) % chars.Length;
            result.Append(chars[randomIndex]);
        }
        
        return result.ToString();
    }
}