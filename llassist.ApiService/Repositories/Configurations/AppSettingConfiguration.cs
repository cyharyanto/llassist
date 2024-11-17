using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace llassist.ApiService.Repositories.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings");

        builder.HasKey(ac => ac.Key);
        
        builder.Property(ac => ac.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ac => ac.Value)
            .IsRequired();

        builder.Property(ac => ac.Description)
            .IsRequired(false);

        builder.Property(ac => ac.CreatedAt)
            .IsRequired();

        builder.Property(ac => ac.UpdatedAt)
            .IsRequired(false);
    }
} 