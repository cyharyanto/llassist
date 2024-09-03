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
        builder.OwnsOne(a => a.KeySemantics, builder =>
        {
            builder.ToJson();
        });
        builder.OwnsMany(a => a.Relevances, builder =>
        {
            builder.ToJson();
        });
        builder.HasOne(a => a.Project)
            .WithMany(p => p.Articles)
            .HasForeignKey(a => a.ProjectId);
    }
}
