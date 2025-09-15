using BetterCallSaul.UserService.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BetterCallSaul.UserService.Data;

public class UserServiceContext : IdentityDbContext<User, Role, Guid>
{
    public UserServiceContext(DbContextOptions<UserServiceContext> options) : base(options)
    {
    }

    // Parameterless constructor for testing
    protected UserServiceContext() : base()
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public virtual DbSet<RegistrationCode> RegistrationCodes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entity relationships and constraints
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserServiceContext).Assembly);

        // Configure RegistrationCode relationships
        modelBuilder.Entity<RegistrationCode>()
            .HasOne(rc => rc.UsedByUser)
            .WithMany(u => u.RegistrationCodes)
            .HasForeignKey(rc => rc.UsedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure AuditLog relationships
        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}