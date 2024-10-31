using llassist.ApiService.Repositories.Converters;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class ArticleKeySemanticConfiguration : IEntityTypeConfiguration<ArticleKeySemantic>
{
    public void Configure(EntityTypeBuilder<ArticleKeySemantic> builder)
    {
        builder.ToTable("ArticleKeySemantics");

        builder.HasKey(aks => new { aks.ArticleId, aks.KeySemanticIndex });
        builder.Property(aks => aks.ArticleId)
            .HasConversion(new UlidToStringConverter());

        builder.Property(aks => aks.KeySemanticIndex)
            .IsRequired();
    }
}
