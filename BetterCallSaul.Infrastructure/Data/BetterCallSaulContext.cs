using BetterCallSaul.Core.Interfaces;
using BetterCallSaul.Core.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BetterCallSaul.Infrastructure.Data;

public class BetterCallSaulContext : IdentityDbContext<User, Role, Guid>
{
    public BetterCallSaulContext(DbContextOptions<BetterCallSaulContext> options) : base(options)
    {
    }

    // Parameterless constructor for testing
    protected BetterCallSaulContext() : base()
    {
    }

    public virtual DbSet<Case> Cases { get; set; } = null!;
    public virtual DbSet<Document> Documents { get; set; } = null!;
    public virtual DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public virtual DbSet<CaseAnalysis> CaseAnalyses { get; set; } = null!;
    public virtual DbSet<LegalCase> LegalCases { get; set; } = null!;
    public virtual DbSet<CourtOpinion> CourtOpinions { get; set; } = null!;
    public virtual DbSet<JustiaSearchResult> JustiaSearchResults { get; set; } = null!;
    public virtual DbSet<LegalStatute> LegalStatutes { get; set; } = null!;
    public virtual DbSet<CaseMatch> CaseMatches { get; set; } = null!;
    public virtual DbSet<MatchingCriteria> MatchingCriteria { get; set; } = null!;
    public virtual DbSet<RegistrationCode> RegistrationCodes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entity relationships and constraints
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BetterCallSaulContext).Assembly);

        // Configure CaseAnalysis Metadata property
        modelBuilder.Entity<CaseAnalysis>()
            .Property(e => e.Metadata)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        // Configure complex types as JSON
        modelBuilder.Entity<CaseAnalysis>()
            .Property(e => e.KeyLegalIssues)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        modelBuilder.Entity<CaseAnalysis>()
            .Property(e => e.PotentialDefenses)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        modelBuilder.Entity<CaseAnalysis>()
            .Property(e => e.Recommendations)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Recommendation>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Recommendation>());

        modelBuilder.Entity<CaseAnalysis>()
            .Property(e => e.EvidenceEvaluation)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<EvidenceEvaluation>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new EvidenceEvaluation());

        modelBuilder.Entity<CaseAnalysis>()
            .Property(e => e.TimelineAnalysis)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<TimelineAnalysis>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new TimelineAnalysis());

        // Configure DocumentText ExtractionMetadata property
        modelBuilder.Entity<DocumentText>()
            .Property(e => e.ExtractionMetadata)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        // Configure DocumentText Pages property
        modelBuilder.Entity<DocumentText>()
            .Property(e => e.Pages)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<TextPage>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<TextPage>());

        // Configure RegistrationCode
        modelBuilder.Entity<RegistrationCode>()
            .HasIndex(rc => rc.Code)
            .IsUnique();

        modelBuilder.Entity<RegistrationCode>()
            .HasOne(rc => rc.UsedByUser)
            .WithMany()
            .HasForeignKey(rc => rc.UsedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Soft delete query filter
        modelBuilder.Entity<Case>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Document>().HasQueryFilter(d => !d.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (IAuditableEntity)entityEntry.Entity;
            var now = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
            }

            entity.UpdatedAt = now;
        }
    }
}

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}