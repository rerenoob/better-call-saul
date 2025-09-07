#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 8.0.0"
#r "nuget: Microsoft.EntityFrameworkCore.Tools, 8.0.0"
#r "nuget: Microsoft.Extensions.Configuration, 8.0.0"
#r "nuget: Microsoft.Extensions.Configuration.Json, 8.0.0"

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Simple RegistrationCode model for the script
public class RegistrationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsUsed { get; set; } = false;
    public Guid? UsedByUserId { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? Notes { get; set; }
}

// Simple DbContext for the script
public class RegistrationCodeContext : DbContext
{
    private readonly string _connectionString;

    public RegistrationCodeContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<RegistrationCode> RegistrationCodes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_connectionString);
    }
}

// Helper methods
static string GenerateSecureCode(int length = 12)
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

static void ShowUsage()
{
    Console.WriteLine("Usage: dotnet script generate-registration-codes.cs [options]");
    Console.WriteLine("Options:");
    Console.WriteLine("  -c, --count <number>     Number of codes to generate (default: 1)");
    Console.WriteLine("  -e, --expires <days>     Days until expiration (default: 30)");
    Console.WriteLine("  -b, --created-by <name>  Who is creating the codes (default: 'Admin')");
    Console.WriteLine("  -n, --notes <text>       Notes for the codes");
    Console.WriteLine("  --connection-string <cs> Database connection string");
    Console.WriteLine("  --list                   List existing codes");
    Console.WriteLine("  --cleanup                Remove expired codes");
    Console.WriteLine("  -h, --help              Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet script generate-registration-codes.cs -c 10 -e 60");
    Console.WriteLine("  dotnet script generate-registration-codes.cs --list");
    Console.WriteLine("  dotnet script generate-registration-codes.cs --cleanup");
}

// Main execution
var args = Args.ToArray();

// Parse arguments
int count = 1;
int expireDays = 30;
string createdBy = "Admin";
string notes = "";
string? connectionString = null;
bool listCodes = false;
bool cleanup = false;
bool showHelp = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i].ToLower())
    {
        case "-c":
        case "--count":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out count))
                i++;
            break;
        case "-e":
        case "--expires":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out expireDays))
                i++;
            break;
        case "-b":
        case "--created-by":
            if (i + 1 < args.Length)
                createdBy = args[++i];
            break;
        case "-n":
        case "--notes":
            if (i + 1 < args.Length)
                notes = args[++i];
            break;
        case "--connection-string":
            if (i + 1 < args.Length)
                connectionString = args[++i];
            break;
        case "--list":
            listCodes = true;
            break;
        case "--cleanup":
            cleanup = true;
            break;
        case "-h":
        case "--help":
            showHelp = true;
            break;
    }
}

if (showHelp)
{
    ShowUsage();
    return;
}

// Default connection string for development
if (connectionString == null)
{
    connectionString = "Server=(localdb)\\mssqllocaldb;Database=BetterCallSaulDb;Trusted_Connection=true;TrustServerCertificate=true;";
}

try
{
    using var context = new RegistrationCodeContext(connectionString);

    // Ensure the database is created (for development)
    await context.Database.EnsureCreatedAsync();

    if (listCodes)
    {
        var codes = await context.RegistrationCodes
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        Console.WriteLine($"Found {codes.Count} registration codes:");
        Console.WriteLine(new string('-', 100));
        Console.WriteLine($"{"Code",-15} {"Created By",-15} {"Created",-20} {"Expires",-20} {"Used",-5} {"Notes",-20}");
        Console.WriteLine(new string('-', 100));

        foreach (var code in codes)
        {
            Console.WriteLine($"{code.Code,-15} {code.CreatedBy,-15} {code.CreatedAt:yyyy-MM-dd HH:mm,-20} {code.ExpiresAt:yyyy-MM-dd HH:mm,-20} {(code.IsUsed ? "Yes" : "No"),-5} {code.Notes,-20}");
        }
        return;
    }

    if (cleanup)
    {
        var expiredCodes = await context.RegistrationCodes
            .Where(c => c.ExpiresAt < DateTime.UtcNow || c.IsUsed)
            .ToListAsync();

        context.RegistrationCodes.RemoveRange(expiredCodes);
        var removed = await context.SaveChangesAsync();
        
        Console.WriteLine($"Cleaned up {removed} expired or used registration codes.");
        return;
    }

    // Generate codes
    var expirationDate = DateTime.UtcNow.AddDays(expireDays);
    var generatedCodes = new List<RegistrationCode>();

    Console.WriteLine($"Generating {count} registration codes...");
    Console.WriteLine($"Created by: {createdBy}");
    Console.WriteLine($"Expires: {expirationDate:yyyy-MM-dd HH:mm} UTC");
    if (!string.IsNullOrEmpty(notes))
        Console.WriteLine($"Notes: {notes}");
    Console.WriteLine();

    for (int i = 0; i < count; i++)
    {
        var code = new RegistrationCode
        {
            Code = GenerateSecureCode(),
            CreatedBy = createdBy,
            ExpiresAt = expirationDate,
            Notes = notes
        };

        // Ensure code is unique
        while (await context.RegistrationCodes.AnyAsync(c => c.Code == code.Code))
        {
            code.Code = GenerateSecureCode();
        }

        generatedCodes.Add(code);
    }

    context.RegistrationCodes.AddRange(generatedCodes);
    await context.SaveChangesAsync();

    Console.WriteLine("Generated codes:");
    Console.WriteLine(new string('-', 50));
    foreach (var code in generatedCodes)
    {
        Console.WriteLine(code.Code);
    }
    
    Console.WriteLine(new string('-', 50));
    Console.WriteLine($"Successfully generated {generatedCodes.Count} registration codes!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}