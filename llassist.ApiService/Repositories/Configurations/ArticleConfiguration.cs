using llassist.ApiService.Repositories.Converters;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(new UlidToStringConverter());

        builder.Property(a => a.Title)
            .IsRequired();

        builder.Property(a => a.ProjectId)
            .IsRequired()
            .HasConversion(new UlidToStringConverter());

        builder.HasOne(a => a.Project)
            .WithMany(p => p.Articles)
            .HasForeignKey(a => a.ProjectId);

        builder.HasMany(a => a.ArticleKeySemantics)
            .WithOne(aks => aks.Article)
            .HasForeignKey(aks => aks.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.ArticleRelevances)
            .WithOne(ar => ar.Article)
            .HasForeignKey(ar => ar.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
