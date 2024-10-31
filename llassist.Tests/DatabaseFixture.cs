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
    public Article? Article { get; }

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
                        Articles = [],
                        ProjectDefinitions = [],
                        ResearchQuestions = [],
                    };
                    context.Projects.Add(Project);

                    Article = new Article
                    {
                        Id = Ulid.NewUlid(),
                        Authors = "Test Author",
                        Year = 2021,
                        Title = "Test Title",
                        DOI = "Test DOI",
                        Link = "Test Link",
                        Abstract = "Test Abstract",
                        ProjectId = Project.Id,
                        Project = Project,
                        ArticleKeySemantics = [],
                        ArticleRelevances = [],
                    };
                    context.Articles.Add(Article);

                    context.SaveChanges();
                }

                _databaseInitialized = true;
            }
        }
    }

    public static ApplicationDbContext CreateContext()
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options);
    }

    public void CleanUp()
    {
        using var context = CreateContext();
        if (Project != null)
        {
            context.Projects.Remove(Project);
        }
        if (Article != null)
        {
            context.Articles.Remove(Article);
        }

        context.SaveChanges();
    }

    public void Dispose()
    {
        CleanUp();
    }
}
