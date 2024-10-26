using llassist.ApiService.Repositories;
using llassist.ApiService.Repositories.Specifications;
using llassist.Common;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Assert = Xunit.Assert;

namespace llassist.Tests;

public class EstimateRelevanceJobRepositoryTests(DatabaseFixture fixture) : BaseRepositoryTests<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec>(fixture)
{
    protected override ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec> CreateRepository(ApplicationDbContext context)
    {
        return new EstimateRelevanceJobRepository(context);
    }

    protected override EstimateRelevanceJob CreateTestEntity(Project project)
    {
        return new EstimateRelevanceJob
        {
            Id = Ulid.NewUlid(),
            ModelName = "Sample Model Name",
            CreatedAt = DateTimeOffset.UtcNow,
            TotalArticles = 11,
            ProjectId = project.Id,
            Snapshots = [],
        };
    }

    protected override DbSet<EstimateRelevanceJob> GetDbSet(ApplicationDbContext context)
    {
        return context.EstimateRelevanceJobs;
    }

    protected override async Task UpdateThenRead()
    {
        // Read for update
        var repository = CreateRepository();
        var updateEntity = await repository.ReadAsync(TestEntity.Id);
        Assert.NotNull(updateEntity);

        // Update
        updateEntity.TotalArticles = 20;
        updateEntity.Snapshots.Add(new Snapshot
        {
            Id = Ulid.NewUlid(),
            EntityType = "Sample Entity Type",
            EntityId = Ulid.NewUlid(),
            SerializedEntity = "Sample Serialized Entity",
            CreatedAt = DateTimeOffset.UtcNow,
            EstimateRelevanceJobId = updateEntity.Id,
        });
        await repository.UpdateAsync(updateEntity);

        // Read
        var readEntity = await CreateRepository().ReadAsync(updateEntity.Id);
        Assert.NotNull(readEntity);

        LogEntity(readEntity);
        VerifyEntity(updateEntity, readEntity);
    }

    protected override void VerifyEntity(EstimateRelevanceJob expected, EstimateRelevanceJob actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.ModelName, actual.ModelName);
        VerifyDateTimeOffset(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.TotalArticles, actual.TotalArticles);
        Assert.Equal(expected.ProjectId, actual.ProjectId);

        // Verify Snapshots
        Assert.Equal(expected.Snapshots.Count, actual.Snapshots.Count);
        var expectedSnapshots = expected.Snapshots.ToList();
        var resultSnapshots = actual.Snapshots.ToList();
        for (int i = 0; i < expected.Snapshots.Count; i++)
        {
            Assert.Equal(expectedSnapshots[i].Id, resultSnapshots[i].Id);
            Assert.Equal(expectedSnapshots[i].EntityType, resultSnapshots[i].EntityType);
            Assert.Equal(expectedSnapshots[i].EntityId, resultSnapshots[i].EntityId);
            Assert.Equal(expectedSnapshots[i].SerializedEntity, resultSnapshots[i].SerializedEntity);
            VerifyDateTimeOffset(expectedSnapshots[i].CreatedAt, resultSnapshots[i].CreatedAt);
            Assert.Equal(expectedSnapshots[i].EstimateRelevanceJobId, resultSnapshots[i].EstimateRelevanceJobId);
        }
    }
}
