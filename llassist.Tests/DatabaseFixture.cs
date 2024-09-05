using llassist.ApiService.Repositories;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace llassist.Tests;

public class DatabaseFixture : IDisposable
{
    private const string ConnectionString = "Host=localhost;Database=llassistdb;Username=postgres;Password=NotSoGoodPassword;";

    private static readonly object _lock = new();
    private static bool _databaseInitialized;

    public Project? Project { get; }

    public DatabaseFixture()
    {
        lock (_lock)
        {
            if (!_databaseInitialized)
            {
                using (var context = CreateContext())
                {
                    context.Database.EnsureCreated();

                    Project = new Project
                    {
                        Id = Ulid.NewUlid(),
                        Name = "Random Project",
                        Description = "This is a randomly generated project",
                        Articles = new List<Article>(),
                        ResearchQuestions = new ResearchQuestions
                        {
                            Definitions = new List<string>(),
                            Questions = new List<Question>()
                        }
                    };
                    context.Projects.Add(Project);
                    context.SaveChanges();
                }

                _databaseInitialized = true;
            }
        }
    }

    public ApplicationDbContext CreateContext()
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options);
    }

    public void CleanUp()
    {
        using (var context = CreateContext())
        {
            if (Project != null)
            {
                context.Projects.Remove(Project);
                context.SaveChanges();
            }
        }
    }

    public void Dispose()
    {
        CleanUp();
    }
}
