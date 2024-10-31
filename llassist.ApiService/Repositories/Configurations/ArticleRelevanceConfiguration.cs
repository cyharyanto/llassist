using llassist.ApiService.Repositories.Converters;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class ArticleRelevanceConfiguration : IEntityTypeConfiguration<ArticleRelevance>
{
    public void Configure(EntityTypeBuilder<ArticleRelevance> builder)
    {
        builder.ToTable("ArticleRelevances");

        builder.HasKey(ar => new { ar.ArticleId, ar.EstimateRelevanceJobId, ar.RelevanceIndex });
        builder.Property(ar => ar.ArticleId)
            .IsRequired()
            .HasConversion(new UlidToStringConverter());
        builder.Property(ar => ar.EstimateRelevanceJobId)
            .IsRequired()
            .HasConversion(new UlidToStringConverter());

        builder.Property(ar => ar.RelevanceIndex)
            .IsRequired();

        builder.Property(ar => ar.Question)
            .IsRequired();

        builder.Property(ar => ar.CreatedAt)
            .HasConversion(new UtcDateTimeOffsetConverter());
    }
}
