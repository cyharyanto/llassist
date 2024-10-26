using llassist.ApiService.Repositories.Converters;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class ResearchQuestionConfiguration : IEntityTypeConfiguration<ResearchQuestion>
{
    public void Configure(EntityTypeBuilder<ResearchQuestion> builder)
    {
        builder.ToTable("ResearchQuestions");

        builder.HasKey(rq => rq.Id);
        builder.Property(rq => rq.Id)
            .HasConversion(new UlidToStringConverter());

        builder.Property(rq => rq.QuestionText)
            .IsRequired();

        builder.Property(rq => rq.ProjectId)
            .IsRequired()
            .HasConversion(new UlidToStringConverter());

        builder.HasOne(rq => rq.Project)
            .WithMany(p => p.ResearchQuestions)
            .HasForeignKey(rq => rq.ProjectId);

        builder.HasMany(rq => rq.QuestionDefinitions)
            .WithOne(qd => qd.ResearchQuestion)
            .HasForeignKey(qd => qd.ResearchQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
