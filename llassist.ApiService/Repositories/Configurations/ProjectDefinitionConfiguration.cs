using llassist.ApiService.Repositories.Converters;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class ProjectDefinitionConfiguration : IEntityTypeConfiguration<ProjectDefinition>
{
    public void Configure(EntityTypeBuilder<ProjectDefinition> builder)
    {
        builder.ToTable("ProjectDefinitions");

        builder.HasKey(pd => pd.Id);
        builder.Property(pd => pd.Id)
            .HasConversion(new UlidToStringConverter());

        builder.Property(pd => pd.Definition)
            .IsRequired();

        builder.Property(pd => pd.ProjectId)
            .IsRequired()
            .HasConversion(new UlidToStringConverter());

        builder.HasOne(pd => pd.Project)
            .WithMany(p => p.ProjectDefinitions)
            .HasForeignKey(pd => pd.ProjectId);
    }
}
