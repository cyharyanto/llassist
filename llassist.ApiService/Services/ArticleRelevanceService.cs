using llassist.ApiService.Repositories;
using llassist.Common.Mappers;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace llassist.ApiService.Services;

public interface IArticleRelevanceService
{
    public Task<bool> InsertRelevancesAndUpdateJobProgress(Ulid articleId, Ulid jobId, IList<Relevance> relevances);
}

public class ArticleRelevanceService : IArticleRelevanceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ArticleRelevanceService> _logger;

    public ArticleRelevanceService(ApplicationDbContext dbContext, ILogger<ArticleRelevanceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> InsertRelevancesAndUpdateJobProgress(Ulid articleId, Ulid jobId, IList<Relevance> relevances)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var articleRelevanceInserted = await InsertArticleRelevances(articleId, jobId, relevances, transaction);

        if (articleRelevanceInserted)
        {
            var jobUpdated = await IncrementCompletedArticles(jobId, transaction);

            if (jobUpdated)
            {
                await transaction.CommitAsync();
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to update job progress for Job: {jobId}", jobId);

                await transaction.RollbackAsync();
                return false;
            }
        }
        else
        {
            _logger.LogWarning("Relevance not inserted, possibly already exists for Article: {articleId} and Job: {jobId}", articleId, jobId);

            await transaction.RollbackAsync();
            return false;
        }
    }

    private async Task<bool> InsertArticleRelevances(Ulid articleId, Ulid jobId, IList<Relevance> relevances, IDbContextTransaction transaction)
    {
        try
        {
            var articleRelevances = ModelMappers.ToArticleRelevances(articleId, jobId, relevances);
            await _dbContext.ArticleRelevances.AddRangeAsync(articleRelevances);
            var insertedRow = await _dbContext.SaveChangesAsync();

            if (insertedRow != relevances.Count)
            {
                _logger.LogWarning("Failed to insert all Relevances for Article: {articleId}, Job: {jobId}", articleId, jobId);
                await transaction.RollbackAsync();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            if (ex is DbUpdateException && ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                // Handle unique constraint violation
                return false;
            }
            else
            {
                _logger.LogError(ex, "Error occurred while inserting Relevance for Article: {articleId} and Job: {jobId}",
                    articleId, jobId);

                await transaction.RollbackAsync();
                throw new InvalidDataException("Failed to insert relevance", ex);
            }
        }
    }

    private async Task<bool> IncrementCompletedArticles(Ulid jobId, IDbContextTransaction transaction)
    {
        try
        {
            var updatedRow = await _dbContext.EstimateRelevanceJobs
                .Where(j => j.Id == jobId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    j => j.CompletedArticles,
                    j => j.CompletedArticles + 1));

            return updatedRow > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job progress for Job: {jobId}", jobId);

            await transaction.RollbackAsync();
            throw new InvalidDataException("Failed to update job", ex);
        }
    }

}
