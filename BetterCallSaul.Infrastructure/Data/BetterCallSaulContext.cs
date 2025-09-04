using BetterCallSaul.Core.Interfaces;
using BetterCallSaul.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BetterCallSaul.Infrastructure.Data;

public class BetterCallSaulContext : IdentityDbContext<User, Role, Guid>
{
    public BetterCallSaulContext(DbContextOptions<BetterCallSaulContext> options) : base(options)
    {
    }

    public DbSet<Case> Cases { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<CaseAnalysis> CaseAnalyses { get; set; } = null!;
    public DbSet<LegalCase> LegalCases { get; set; } = null!;
    public DbSet<CourtOpinion> CourtOpinions { get; set; } = null!;
    public DbSet<JustiaSearchResult> JustiaSearchResults { get; set; } = null!;
    public DbSet<LegalStatute> LegalStatutes { get; set; } = null!;
    public DbSet<CaseMatch> CaseMatches { get; set; } = null!;
    public DbSet<MatchingCriteria> MatchingCriteria { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entity relationships and constraints
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BetterCallSaulContext).Assembly);

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