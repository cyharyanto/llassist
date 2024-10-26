using llassist.ApiService.Repositories.Converters;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class SnapshotConfiguration : IEntityTypeConfiguration<Snapshot>
{
    public void Configure(EntityTypeBuilder<Snapshot> builder)
    {
        builder.ToTable("Snapshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(new UlidToStringConverter());

        builder.Property(s => s.EntityType)
            .IsRequired();

        builder.Property(s => s.EntityId)
            .IsRequired()
            .HasConversion(new UlidToStringConverter());

        builder.Property(s => s.SerializedEntity)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasConversion(new UtcDateTimeOffsetConverter());

        builder.Property(s => s.EstimateRelevanceJobId)
            .IsRequired()
            .HasConversion(new UlidToStringConverter());

        builder.HasOne(s => s.EstimateRelevanceJob)
            .WithMany(j => j.Snapshots)
            .HasForeignKey(s => s.EstimateRelevanceJobId);
    }
}
