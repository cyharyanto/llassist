using llassist.Common;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace llassist.ApiService.Repositories;

public class ArticleRepository : ICRUDRepository<Ulid, Article>
{
    private readonly ApplicationDbContext _context;

    public ArticleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Article> CreateAsync(Article article)
    {
        _context.Articles.Add(article);
        await _context.SaveChangesAsync();
        return article;
    }

    public async Task<bool> DeleteAsync(Ulid id)
    {
        var article = _context.Articles.Find(id);
        if (article == null)
        {
            return false;
        }

        _context.Articles.Remove(article);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Article>> ReadAllAsync()
    {
        return await _context.Articles.ToListAsync();
    }

    public async Task<Article?> ReadAsync(Ulid id)
    {
        return await _context.Articles.FindAsync(id);
    }

    public async Task<Article> UpdateAsync(Article article)
    {
        _context.Entry(article).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return article;
    }
}