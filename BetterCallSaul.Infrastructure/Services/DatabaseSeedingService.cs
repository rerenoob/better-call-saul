using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace BetterCallSaul.Infrastructure.Services;

public class DatabaseSeedingService
{
    private readonly BetterCallSaulContext _context;
    private readonly ILogger<DatabaseSeedingService> _logger;
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;

    public DatabaseSeedingService(BetterCallSaulContext context, ILogger<DatabaseSeedingService> logger,
        RoleManager<Role> roleManager, UserManager<User> userManager)
    {
        _context = context;
        _logger = logger;
        _roleManager = roleManager;
        _userManager = userManager;
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

    public async Task SeedRolesAsync()
    {
        _logger.LogInformation("Starting role seeding...");

        var requiredRoles = new[] { "User", "Admin" };

        foreach (var roleName in requiredRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new Role 
                { 
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                };
                
                var result = await _roleManager.CreateAsync(role);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Role {RoleName} already exists", roleName);
            }
        }

        _logger.LogInformation("Role seeding completed");
    }

    public async Task SeedAdminUserAsync(string email = "admin@bettercallsaul.com",
        string password = "Admin123!", string fullName = "System Administrator")
    {
        _logger.LogInformation("Starting admin user seeding for email: {Email}", email);

        // Check if admin user already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            _logger.LogInformation("Admin user with email {Email} already exists", email);

            // Ensure user has admin role
            if (!await _userManager.IsInRoleAsync(existingUser, "Admin"))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(existingUser, "Admin");
                if (addRoleResult.Succeeded)
                {
                    _logger.LogInformation("Added Admin role to existing user {Email}", email);
                }
                else
                {
                    _logger.LogError("Failed to add Admin role to user {Email}: {Errors}",
                        email, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                }
            }
            return;
        }

        // Ensure Admin role exists
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            _logger.LogWarning("Admin role does not exist. Creating it first...");
            await SeedRolesAsync();
        }

        // Parse full name into first and last name
        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : "Admin";
        var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "User";

        // Create admin user
        var adminUser = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(adminUser, password);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create admin user {Email}: {Errors}",
                email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        _logger.LogInformation("Successfully created admin user: {Email}", email);

        // Add admin role
        var roleResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
        if (roleResult.Succeeded)
        {
            _logger.LogInformation("Successfully assigned Admin role to user: {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to assign Admin role to user {Email}: {Errors}",
                email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("Admin user seeding completed for: {Email}", email);
    }
}