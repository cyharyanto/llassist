using llassist.ApiService.Repositories.Converters;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class EstimateRelevanceJobConfiguration : IEntityTypeConfiguration<EstimateRelevanceJob>
{
    public void Configure(EntityTypeBuilder<EstimateRelevanceJob> builder)
    {
        builder.ToTable("EstimateRelevanceJobs");

        builder.HasKey(erj => erj.Id);
        builder.Property(erj => erj.Id)
            .HasConversion(new UlidToStringConverter());

        builder.Property(erj => erj.CreatedAt)
            .HasConversion(new UtcDateTimeOffsetConverter());

        builder.HasIndex(erj => erj.ProjectId);
        builder.Property(erj => erj.ProjectId)
            .IsRequired()
            .HasConversion(new UlidToStringConverter());

        builder.HasMany(erj => erj.Snapshots)
            .WithOne(s => s.EstimateRelevanceJob)
            .HasForeignKey(s => s.EstimateRelevanceJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
