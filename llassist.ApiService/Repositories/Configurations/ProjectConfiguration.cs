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
        builder.HasMany(p => p.Articles)
               .WithOne(a => a.Project)
               .HasForeignKey(a => a.ProjectId);
        builder.OwnsOne(p => p.ResearchQuestions, builder =>
        {
            builder.ToJson();
            builder.OwnsMany(rq => rq.Questions, builder =>
            {
                builder.ToJson();
            });
        });
    }
}