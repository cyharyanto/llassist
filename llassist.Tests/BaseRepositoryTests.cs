using llassist.ApiService.Repositories;
using llassist.Common;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;
using Assert = Xunit.Assert;

namespace llassist.Tests;

public abstract class BaseRepositoryTests<TId, TEntity, TSearchSpec> : IClassFixture<DatabaseFixture>, IDisposable
    where TEntity : class, IEntity<TId>
{
    protected readonly TEntity TestEntity;

    public BaseRepositoryTests(DatabaseFixture fixture)
    {
        var project = fixture.Project;
        Assert.NotNull(project);

        TestEntity = CreateTestEntity(project);
    }

    [Fact]
    public async Task CRUDEntity()
    {
        await CreateThenRead();
        await UpdateThenRead();
        await DeleteThenRead();
    }

    protected abstract ICRUDRepository<TId, TEntity, TSearchSpec> CreateRepository(ApplicationDbContext context);
    protected abstract TEntity CreateTestEntity(Project project);
    protected abstract DbSet<TEntity> GetDbSet(ApplicationDbContext context);
    protected abstract void VerifyEntity(TEntity expected, TEntity actual);
    protected abstract Task UpdateThenRead();

    protected ICRUDRepository<TId, TEntity, TSearchSpec> CreateRepository()
    {
        var context = DatabaseFixture.CreateContext();
        return CreateRepository(context);
    }

    protected async Task CreateThenRead()
    {
        await CreateRepository().CreateAsync(TestEntity);

        var readEntity = await CreateRepository().ReadAsync(TestEntity.Id);
        Assert.NotNull(readEntity);

        LogEntity(readEntity);
        VerifyEntity(TestEntity, readEntity);
    }

    protected async Task DeleteThenRead()
    {
        await CreateRepository().DeleteAsync(TestEntity.Id);

        var readEntity = await CreateRepository().ReadAsync(TestEntity.Id);
        Assert.Null(readEntity);
    }

    protected static void LogEntity(TEntity entity)
    {
        Console.WriteLine($"Read entity: {JsonSerializer.Serialize(entity)}");
    }

    protected static void VerifyDateTimeOffset(DateTimeOffset expected, DateTimeOffset result)
    {
        Assert.True(RoundToMillisecond(expected) == RoundToMillisecond(result));
    }

    private static DateTimeOffset RoundToMillisecond(DateTimeOffset dto)
    {
        // To handle different precision with DB
        long ticks = dto.Ticks;
        long roundedTicks = (long)Math.Round(ticks / 10000.0) * 10000;
        return new DateTimeOffset(roundedTicks, dto.Offset);
    }

    public void Dispose()
    {
        try
        {
            var context = DatabaseFixture.CreateContext();
            GetDbSet(context).Remove(TestEntity);
            context.SaveChanges();
        }
        catch (Exception)
        {
            // Ignore error if the row is already removed
        }
    }
}
