namespace llassist.Common;

public interface ICRUDRepository<TId, TEntity> where TEntity : IEntity<TId>
{
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity?> ReadAsync(TId id);
    Task<IEnumerable<TEntity>> ReadAllAsync();
    Task<bool> DeleteAsync(TId id);
    Task<TEntity> UpdateAsync(TEntity entity);
}
