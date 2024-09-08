using llassist.Common;

namespace llassist.ApiService.Repositories;

public class InMemoryRepository<TEntity, TSearchSpec> : ICRUDRepository<Ulid, TEntity, TSearchSpec> where TEntity : IEntity<Ulid>
{
    private readonly Dictionary<Ulid, TEntity> _entities = new();

    public Task<TEntity> CreateAsync(TEntity entity)
    {
        _entities[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<TEntity?> ReadAsync(Ulid id)
    {
        _entities.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<TEntity>> ReadAllAsync()
    {
        return Task.FromResult(_entities.Values.AsEnumerable());
    }

    public Task<bool> DeleteAsync(Ulid id)
    {
        var result = _entities.Remove(id);
        return Task.FromResult(result);
    }

    public Task<TEntity> UpdateAsync(TEntity entity)
    {
        if (!_entities.ContainsKey(entity.Id))
        {
            throw new KeyNotFoundException($"Entity with ID {entity.Id} not found.");
        }

        _entities[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<TEntity>> ReadWithSearchSpecAsync(TSearchSpec searchSpec)
    {
        throw new NotImplementedException();
    }
}
