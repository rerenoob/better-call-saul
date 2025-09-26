using BetterCallSaul.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BetterCallSaul.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.FileType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedAt);

        builder.Property(d => d.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(d => d.Case)
            .WithMany(c => c.Documents)
            .HasForeignKey(d => d.CaseId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(d => d.UploadedBy)
            .WithMany()
            .HasForeignKey(d => d.UploadedById)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(d => d.CaseId);
        builder.HasIndex(d => d.UploadedById);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.IsDeleted);
    }
}