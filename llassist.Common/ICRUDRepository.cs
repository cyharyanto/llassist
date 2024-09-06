namespace llassist.Common;

public interface ICRUDRepository<TId, TEntity, TSearchSpec> where TEntity : IEntity<TId>
{
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity?> ReadAsync(TId id);
    Task<IEnumerable<TEntity>> ReadAllAsync();
    Task<IEnumerable<TEntity>> ReadWithSearchSpecAsync(TSearchSpec searchSpec);
    Task<bool> DeleteAsync(TId id);
    Task<TEntity> UpdateAsync(TEntity entity);
}
