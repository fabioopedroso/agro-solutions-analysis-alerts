using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReading>
{
    public void Configure(EntityTypeBuilder<SensorReading> builder)
    {
        builder.ToTable("SensorReadings");

        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.Id)
            .ValueGeneratedOnAdd();

        builder.Property(sr => sr.FieldId)
            .IsRequired();

        builder.Property(sr => sr.SensorType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(sr => sr.Value)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(sr => sr.Timestamp)
            .IsRequired();

        builder.Property(sr => sr.ProcessedAt)
            .IsRequired();

        builder.HasIndex(sr => sr.FieldId);
        builder.HasIndex(sr => new { sr.FieldId, sr.SensorType, sr.Timestamp });
    }
}
