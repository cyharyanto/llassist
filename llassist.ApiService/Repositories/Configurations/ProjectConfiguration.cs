using llassist.ApiService.Repositories.Converters;
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
        builder.Property(p => p.Id)
            .HasConversion(new UlidToStringConverter());

        builder.Property(p => p.Name)
            .IsRequired();

        builder.HasMany(p => p.Articles)
            .WithOne(a => a.Project)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ProjectDefinitions)
            .WithOne(pd => pd.Project)
            .HasForeignKey(pd => pd.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ResearchQuestions)
            .WithOne(rq => rq.Project)
            .HasForeignKey(rq => rq.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
