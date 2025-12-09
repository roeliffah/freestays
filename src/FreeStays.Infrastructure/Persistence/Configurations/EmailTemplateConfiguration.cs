using FreeStays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreeStays.Infrastructure.Persistence.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.Body)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.Variables)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.HasIndex(e => e.Code)
            .IsUnique();
    }
}
