using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace llassist.ApiService.Repositories;
public class ApplicationDbContext : DbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Article> Articles { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Common Ulid to string conversion
        var ulidToStringConverter = new ValueConverter<Ulid, string>(
            ulid => ulid.ToString(),
            str => Ulid.Parse(str));

        modelBuilder.Entity<Project>().Property(e => e.Id).HasConversion(ulidToStringConverter);
        modelBuilder.Entity<Article>().Property(e => e.Id).HasConversion(ulidToStringConverter);
        modelBuilder.Entity<Article>().Property(e => e.ProjectId).HasConversion(ulidToStringConverter);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
