using llassist.ApiService.Repositories;
using llassist.ApiService.Services;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;
using TheoryAttribute = Xunit.TheoryAttribute;

namespace llassist.Tests;

public class ArticleRelevanceServiceTests : IClassFixture<DatabaseFixture>, IDisposable
{
    private readonly IList<ArticleRelevance> TestRelevances = new List<ArticleRelevance>();
    private readonly IList<EstimateRelevanceJob> TestJobs = new List<EstimateRelevanceJob>();
    private readonly Article TestArticle;

    public ArticleRelevanceServiceTests(DatabaseFixture fixture)
    {
        TestArticle = fixture.Article;
        Assert.NotNull(TestArticle);
    }

    private static ArticleRelevanceService CreateService()
    {
        return new ArticleRelevanceService(
            DatabaseFixture.CreateContext(),
            new Mock<ILogger<ArticleRelevanceService>>().Object);
    }

    [Fact]
    public async Task InsertRelevancesAndUpdateJobProgress_Success_ReturnsTrue()
    {
        // Arrange
        var (job, relevances) = await SetupJobAndRelevances(initialCompletedArticles: 0);

        // Act
        var result = await CreateService().InsertRelevancesAndUpdateJobProgress(TestArticle.Id, job.Id, relevances);

        // Assert
        Assert.True(result);
        await AssertJobAndRelevances(job.Id, TestArticle.Id, relevances, expectedCompletedArticles: 1, shouldExist: true);
    }

    [Theory]
    [InlineData(true, 0)] // Insert fails
    [InlineData(false, int.MaxValue)] // Update fails
    public async Task InsertRelevancesAndUpdateJobProgress_OperationFails_ReturnsFalse(bool invalidArticleId, int initialCompletedArticles)
    {
        // Arrange
        var (job, relevances) = await SetupJobAndRelevances(initialCompletedArticles);
        var articleId = invalidArticleId ? Ulid.NewUlid() : TestArticle.Id;

        // Act
        await Assert.ThrowsAsync<InvalidDataException>(() => CreateService().InsertRelevancesAndUpdateJobProgress(articleId, job.Id, relevances));
        await AssertJobAndRelevances(job.Id, TestArticle.Id, relevances, expectedCompletedArticles: initialCompletedArticles, shouldExist: false);
    }

    [Fact]
    public async Task InsertRelevancesAndUpdateJobProgress_InsertFailsOnUniqueConstraint_ReturnsFalse()
    {
        // Arrange
        var (job, relevances) = await SetupJobAndRelevances();
        await InsertRelevances(TestArticle.Id, job.Id, relevances);

        // Act
        var result = await CreateService().InsertRelevancesAndUpdateJobProgress(TestArticle.Id, job.Id, relevances);

        // Assert
        Assert.False(result);
        await AssertJobAndRelevances(job.Id, TestArticle.Id, relevances, expectedCompletedArticles: 0, shouldExist: true);
    }

    [Fact]
    public async Task InsertRelevancesAndUpdateJobProgress_JobNotFound_ReturnsFalse()
    {
        // Arrange
        var jobId = Ulid.NewUlid();
        var relevances = CreateRelevances();

        // Act
        var result = await CreateService().InsertRelevancesAndUpdateJobProgress(TestArticle.Id, jobId, relevances);

        // Assert
        Assert.False(result);
        var insertedRelevances = await GetRelevances(GetDbContext(), TestArticle.Id, jobId);
        Assert.Empty(insertedRelevances);
    }

    private async Task<(EstimateRelevanceJob, IList<Relevance>)> SetupJobAndRelevances(int initialCompletedArticles = 0)
    {
        var context = GetDbContext();

        var job = new EstimateRelevanceJob
        {
            ProjectId = TestArticle.ProjectId,
            CompletedArticles = initialCompletedArticles
        };
        TestJobs.Add(job);

        context.EstimateRelevanceJobs.Add(job);
        await context.SaveChangesAsync();

        var relevances = CreateRelevances();

        return (job, relevances);
    }

    private IList<Relevance> CreateRelevances()
    {
        return new List<Relevance>
        {
            new Relevance
            {
                Question = "Is this relevant?",
                RelevanceScore = 0.8,
                ContributionScore = 0.7,
                IsRelevant = true,
                IsContributing = true,
                RelevanceReason = "It's highly relevant.",
                ContributionReason = "It contributes significantly."
            },
            new Relevance
            {
                Question = "Does this add value?",
                RelevanceScore = 0.6,
                ContributionScore = 0.5,
                IsRelevant = true,
                IsContributing = false,
                RelevanceReason = "It's somewhat relevant.",
                ContributionReason = "It doesn't contribute much."
            }
        };
    }

    private async Task InsertRelevances(Ulid articleId, Ulid jobId, IList<Relevance> relevances)
    {
        var context = GetDbContext();

        var articleRelevances = relevances.Select((r, index) => new ArticleRelevance
        {
            ArticleId = articleId,
            EstimateRelevanceJobId = jobId,
            RelevanceIndex = index,
            Question = r.Question,
            RelevanceScore = r.RelevanceScore,
            ContributionScore = r.ContributionScore,
            IsRelevant = r.IsRelevant,
            IsContributing = r.IsContributing,
            RelevanceReason = r.RelevanceReason,
            ContributionReason = r.ContributionReason
        }).ToList();

        context.ArticleRelevances.AddRange(articleRelevances);
        await context.SaveChangesAsync();

        foreach (var articleRelevance in articleRelevances)
        {
            TestRelevances.Add(articleRelevance);
        }
    }

    private async Task AssertJobAndRelevances(Ulid jobId, Ulid articleId, IList<Relevance> relevances, int expectedCompletedArticles, bool shouldExist)
    {
        var context = GetDbContext();
        var updatedJob = await context.EstimateRelevanceJobs.FindAsync(jobId);

        Assert.NotNull(updatedJob);
        Assert.Equal(expectedCompletedArticles, updatedJob.CompletedArticles);

        var insertedRelevances = await GetRelevances(context, articleId, jobId);

        if (shouldExist)
        {
            Assert.Equal(relevances.Count, insertedRelevances.Count);
            foreach (var relevance in relevances)
            {
                Assert.Contains(insertedRelevances, r =>
                    r.Question == relevance.Question &&
                    r.RelevanceScore == relevance.RelevanceScore &&
                    r.ContributionScore == relevance.ContributionScore &&
                    r.IsRelevant == relevance.IsRelevant &&
                    r.IsContributing == relevance.IsContributing &&
                    r.RelevanceReason == relevance.RelevanceReason &&
                    r.ContributionReason == relevance.ContributionReason);
            }
            foreach (var insertedRelevance in insertedRelevances)
            {
                TestRelevances.Add(insertedRelevance);
            }
        }
        else
        {
            Assert.Empty(insertedRelevances);
        }
    }

    private async Task<List<ArticleRelevance>> GetRelevances(ApplicationDbContext context, Ulid articleId, Ulid jobId)
    {
        return await context.ArticleRelevances
            .Where(r => r.ArticleId == articleId && r.EstimateRelevanceJobId == jobId)
            .ToListAsync();
    }

    private ApplicationDbContext GetDbContext()
    {
        return DatabaseFixture.CreateContext();
    }

    public void Dispose()
    {
        try
        {
            var context = DatabaseFixture.CreateContext();
            context.ArticleRelevances.RemoveRange(TestRelevances);
            context.EstimateRelevanceJobs.RemoveRange(TestJobs);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clean up test data: {ex.Message}");
            // Ignore error if the row is already removed
        }
    }
}