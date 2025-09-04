using BetterCallSaul.Core.Models;
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

        builder.Property(d => d.OriginalFileName)
            .HasMaxLength(500);

        builder.Property(d => d.FileType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.StoragePath)
            .HasMaxLength(500);

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

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
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.UploadedBy)
            .WithMany()
            .HasForeignKey(d => d.UploadedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.ExtractedText)
            .WithOne(dt => dt.Document)
            .HasForeignKey<DocumentText>(dt => dt.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(d => d.CaseId);
        builder.HasIndex(d => d.UploadedById);
        builder.HasIndex(d => d.Type);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.IsProcessed);
        builder.HasIndex(d => d.IsDeleted);
    }
}