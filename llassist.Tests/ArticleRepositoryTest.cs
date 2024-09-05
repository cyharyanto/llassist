using llassist.ApiService.Repositories;
using llassist.Common.Models;
using Xunit;
using Assert = Xunit.Assert;

namespace llassist.Tests;

public class ArticleRepositoryTest : IClassFixture<DatabaseFixture>, IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ArticleRepository _repository;

    private readonly Article Article;

    public ArticleRepositoryTest(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _repository = new ArticleRepository(_context);

        var project = fixture.Project;
        Assert.NotNull(project);

        Article = new Article
        {
            Title = "Sample Article",
            Authors = "John Doe",
            Year = 2022,
            DOI = "doi:10.1234/sample",
            Link = "https://sample.com",
            Abstract = "This is a sample article.",
            KeySemantics = new KeySemantics(),
            MustRead = true,
            ProjectId = project.Id
        };
    }

    [Fact]
    public async Task CRUDArticle()
    {
        // Create then read
        await _repository.CreateAsync(Article);

        var readArticle = await _repository.ReadAsync(Article.Id);
        VerifyArticle(readArticle);

        // Update then find from read-all
        Article.Title = "Updated Article";
        await _repository.UpdateAsync(Article);

        var allArticles = await _repository.ReadAllAsync();
        Article? foundArticle = null;
        foreach (var article in allArticles)
        {
            if (article.Id == Article.Id)
            {
                foundArticle = article;
            }
        }
        VerifyArticle(foundArticle);

        // Delete then read
        await _repository.DeleteAsync(Article.Id);
        readArticle = await _repository.ReadAsync(Article.Id);
        Assert.Null(readArticle);
    }

    private void VerifyArticle(Article? article)
    {
        Assert.NotNull(article);
        Assert.Equal(article.Title, Article.Title);
        Assert.Equal(article.Authors, Article.Authors);
        Assert.Equal(article.Year, Article.Year);
        Assert.Equal(article.DOI, Article.DOI);
        Assert.Equal(article.Link, Article.Link);
        Assert.Equal(article.Abstract, Article.Abstract);
        Assert.Equal(article.KeySemantics, Article.KeySemantics);
        Assert.Equal(article.MustRead, Article.MustRead);
    }

    public void Dispose()
    {
        try
        {
            _context.Articles.Remove(Article);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            // Ignore the error if the row is already removed
        }
    }
}
