using llassist.ApiService.Repositories;
using llassist.ApiService.Repositories.Specifications;
using llassist.Common;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Assert = Xunit.Assert;

namespace llassist.Tests;

public class ArticleRepositoryTests(DatabaseFixture fixture) : BaseRepositoryTests<Ulid, Article, ArticleSearchSpec>(fixture)
{
    protected override ICRUDRepository<Ulid, Article, ArticleSearchSpec> CreateRepository(ApplicationDbContext context)
    {
        return new ArticleRepository(context);
    }

    protected override Article CreateTestEntity(Project project)
    {
        var articleId = Ulid.NewUlid();
        return new Article
        {
            Id = articleId,
            Authors = "John Doe",
            Year = 2022,
            Title = "Sample Article",
            DOI = "doi:10.1234/sample",
            Link = "https://sample.com",
            Abstract = "This is a sample article.",
            MustRead = true,
            ProjectId = project.Id,
            ArticleKeySemantics = [
                new() { ArticleId = articleId, KeySemanticIndex = 0, Type = ArticleKeySemantic.TypeTopic, Value = "Sample Topic" },
                new() { ArticleId = articleId, KeySemanticIndex = 1, Type = ArticleKeySemantic.TypeTopic, Value = "Another Topic" },
                new() { ArticleId = articleId, KeySemanticIndex = 2, Type = ArticleKeySemantic.TypeEntity, Value = "Sample Entity" },
                new() { ArticleId = articleId, KeySemanticIndex = 3, Type = ArticleKeySemantic.TypeKeyword, Value = "Sample Keyword" },
            ],
            ArticleRelevances =
            [
                new ArticleRelevance
                {
                    ArticleId = articleId,
                    EstimateRelevanceJobId = Ulid.NewUlid(),
                    Question = "Sample Question",
                    RelevanceScore = 0.2,
                    ContributionScore = 0.3,
                    IsRelevant = true,
                    IsContributing = true,
                    RelevanceReason = "Sample Reason",
                    ContributionReason = "Sample Reason",
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            ],
        };
    }

    protected override DbSet<Article> GetDbSet(ApplicationDbContext context)
    {
        return context.Articles;
    }

    protected override void VerifyEntity(Article expected, Article actual)
    {
        VerifyArticles(expected, actual);
    }

    public static void VerifyArticles(Article expected, Article result)
    {
        Assert.Equal(expected.Authors, result.Authors);
        Assert.Equal(expected.Year, result.Year);
        Assert.Equal(expected.Title, result.Title);
        Assert.Equal(expected.DOI, result.DOI);
        Assert.Equal(expected.Link, result.Link);
        Assert.Equal(expected.Abstract, result.Abstract);
        Assert.Equal(expected.ProjectId, result.ProjectId);
        Assert.Equal(expected.MustRead, result.MustRead);

        // Verify ArticleKeySemantics
        Assert.Equal(expected.ArticleKeySemantics.Count, result.ArticleKeySemantics.Count);
        var expectedKeySemantics = expected.ArticleKeySemantics.ToList();
        var resultKeySemantics = result.ArticleKeySemantics.ToList();
        for (int i = 0; i < expectedKeySemantics.Count; i++)
        {
            Assert.Equal(expectedKeySemantics[i].ArticleId, resultKeySemantics[i].ArticleId);
            Assert.Equal(expectedKeySemantics[i].KeySemanticIndex, resultKeySemantics[i].KeySemanticIndex);
            Assert.Equal(expectedKeySemantics[i].Type, resultKeySemantics[i].Type);
            Assert.Equal(expectedKeySemantics[i].Value, resultKeySemantics[i].Value);
        }

        // Verify ArticleRelevances
        Assert.Equal(expected.ArticleRelevances.Count, result.ArticleRelevances.Count);
        var expectedRelevances = expected.ArticleRelevances.ToList();
        var resultRelevances = result.ArticleRelevances.ToList();
        for (int i = 0; i < expectedRelevances.Count; i++)
        {
            Assert.Equal(expectedRelevances[i].ArticleId, resultRelevances[i].ArticleId);
            Assert.Equal(expectedRelevances[i].EstimateRelevanceJobId, resultRelevances[i].EstimateRelevanceJobId);
            VerifyDateTimeOffset(expectedRelevances[i].CreatedAt, resultRelevances[i].CreatedAt);
            Assert.Equal(expectedRelevances[i].Question, resultRelevances[i].Question);
            Assert.Equal(expectedRelevances[i].RelevanceScore, resultRelevances[i].RelevanceScore);
            Assert.Equal(expectedRelevances[i].ContributionScore, resultRelevances[i].ContributionScore);
            Assert.Equal(expectedRelevances[i].IsRelevant, resultRelevances[i].IsRelevant);
            Assert.Equal(expectedRelevances[i].IsContributing, resultRelevances[i].IsContributing);
            Assert.Equal(expectedRelevances[i].RelevanceReason, resultRelevances[i].RelevanceReason);
            Assert.Equal(expectedRelevances[i].ContributionReason, resultRelevances[i].ContributionReason);
        }
    }

    protected override async Task UpdateThenRead()
    {
        // Read for update
        var repository = CreateRepository();
        var updateEntity = await repository.ReadAsync(TestEntity.Id);
        Assert.NotNull(updateEntity);

        // Update
        updateEntity.Authors = "Jane Doe";
        updateEntity.MustRead = !updateEntity.MustRead;
        updateEntity.ArticleKeySemantics = [
            new() { ArticleId = updateEntity.Id, KeySemanticIndex = 0, Type = ArticleKeySemantic.TypeTopic, Value = "Sample Topic" },
            new() { ArticleId = updateEntity.Id, KeySemanticIndex = 1, Type = ArticleKeySemantic.TypeEntity, Value = "Sample Entity" },
            new() { ArticleId = updateEntity.Id, KeySemanticIndex = 2, Type = ArticleKeySemantic.TypeKeyword, Value = "Sample Keyword" },
        ];
        updateEntity.ArticleRelevances.Add(new ArticleRelevance
        {
            ArticleId = updateEntity.Id,
            EstimateRelevanceJobId = Ulid.NewUlid(),
            Question = "Sample Question 2",
            RelevanceScore = 0.9,
            ContributionScore = 0.8,
            IsRelevant = false,
            IsContributing = false,
            RelevanceReason = "Sample Reason 2",
            ContributionReason = "Sample Reason 2",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await repository.UpdateAsync(updateEntity);
        LogEntity(updateEntity);

        // Read
        Article? readEntity = null;
        var searchBySpecResult = await CreateRepository().ReadWithSearchSpecAsync(new ArticleSearchSpec { ProjectId = updateEntity.ProjectId });
        foreach (var entity in searchBySpecResult)
        {
            if (entity.Id == updateEntity.Id)
            {
                readEntity = entity;
                break;
            }
        }
        Assert.NotNull(readEntity);

        LogEntity(readEntity);
        VerifyEntity(updateEntity, readEntity);
    }
}
