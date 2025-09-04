using BetterCallSaul.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetterCallSaul.Infrastructure.Data.Configurations;

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.ToTable("Cases");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CaseNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Court)
            .HasMaxLength(100);

        builder.Property(c => c.Judge)
            .HasMaxLength(100);

        builder.Property(c => c.SuccessProbability)
            .HasPrecision(5, 2);

        builder.Property(c => c.EstimatedValue)
            .HasPrecision(18, 2);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(c => c.User)
            .WithMany(u => u.Cases)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(c => c.CaseNumber)
            .IsUnique();

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.IsDeleted);
        builder.HasIndex(c => c.FiledDate);
        builder.HasIndex(c => c.HearingDate);
    }
}