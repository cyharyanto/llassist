using llassist.ApiService.Repositories.Converters;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class QuestionDefinitionConfiguration : IEntityTypeConfiguration<QuestionDefinition>
{
    public void Configure(EntityTypeBuilder<QuestionDefinition> builder)
    {
        builder.ToTable("QuestionDefinitions");

        builder.HasKey(qd => qd.Id);
        builder.Property(qd => qd.Id)
            .HasConversion(new UlidToStringConverter());

        builder.Property(qd => qd.Definition)
            .IsRequired();

        builder.Property(qd => qd.ResearchQuestionId)
            .IsRequired()
            .HasConversion(new UlidToStringConverter());

        builder.HasOne(qd => qd.ResearchQuestion)
            .WithMany(rq => rq.QuestionDefinitions)
            .HasForeignKey(qd => qd.ResearchQuestionId)
            .IsRequired();
    }
}
