using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);
        builder.OwnsMany(p => p.Articles, builder =>
        {
            builder.ToJson();
        });
        builder.OwnsOne(p => p.ResearchQuestions, builder =>
        {
            builder.ToJson();
        });
    }
}