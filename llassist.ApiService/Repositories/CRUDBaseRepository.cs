using llassist.Common;
using Microsoft.EntityFrameworkCore;

namespace llassist.ApiService.Repositories;

public abstract class CRUDBaseRepository<TId, TEntity, TSearchSpec>(ApplicationDbContext context) : ICRUDRepository<TId, TEntity, TSearchSpec>
    where TEntity : class, IEntity<TId>
{
    protected readonly ApplicationDbContext _context = context;

    public abstract DbSet<TEntity> GetDbSet();

    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        GetDbSet().Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(TId id)
    {
        var dbSet = GetDbSet();
        var entity = dbSet.Find(id);
        if (entity == null)
        {
            return false;
        }

        dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public virtual Task<IEnumerable<TEntity>> ReadAllAsync()
    {
        throw new NotImplementedException("Not implemented by default");
    }

    public virtual async Task<TEntity?> ReadAsync(TId id)
    {
        return await GetDbSet().FindAsync(id);
    }

    public virtual Task<IEnumerable<TEntity>> ReadWithSearchSpecAsync(TSearchSpec searchSpec)
    {
        throw new NotImplementedException("Not implemented by default");
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return entity;
    }
}
