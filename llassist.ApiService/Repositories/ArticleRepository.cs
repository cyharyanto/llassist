using llassist.ApiService.Repositories.Specifications;
using llassist.Common;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace llassist.ApiService.Repositories;

public class ArticleRepository : ICRUDRepository<Ulid, Article, ArticleSearchSpec>
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

    public Task<IEnumerable<Article>> ReadAllAsync()
    {
        throw new NotImplementedException("This method is not supported for Articles");
    }

    public async Task<Article?> ReadAsync(Ulid id)
    {
        return await _context.Articles.FindAsync(id);
    }

    public async Task<IEnumerable<Article>> ReadWithSearchSpecAsync(ArticleSearchSpec searchSpec)
    {
        return await _context.Articles
            .Where(a => a.ProjectId == searchSpec.ProjectId)
            .ToListAsync();
    }

    public async Task<Article> UpdateAsync(Article article)
    {
        _context.Entry(article).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return article;
    }
}