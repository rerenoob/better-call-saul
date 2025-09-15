using BetterCallSaul.UserService.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetterCallSaul.UserService.Data.Configurations;

public class RegistrationCodeConfiguration : IEntityTypeConfiguration<RegistrationCode>
{
    public void Configure(EntityTypeBuilder<RegistrationCode> builder)
    {
        builder.ToTable("RegistrationCodes");

        builder.Property(rc => rc.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(rc => rc.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(rc => rc.Notes)
            .HasMaxLength(500);

        builder.Property(rc => rc.CreatedAt)
            .IsRequired();

        builder.Property(rc => rc.UpdatedAt);

        builder.Property(rc => rc.ExpiresAt)
            .IsRequired();

        builder.Property(rc => rc.UsedAt);

        // Indexes
        builder.HasIndex(rc => rc.Code).IsUnique();
        builder.HasIndex(rc => rc.IsUsed);
        builder.HasIndex(rc => rc.ExpiresAt);
        builder.HasIndex(rc => rc.UsedByUserId);
    }
}