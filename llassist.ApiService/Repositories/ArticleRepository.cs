using llassist.ApiService.Repositories.Specifications;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace llassist.ApiService.Repositories;

public class ArticleRepository(ApplicationDbContext context) : CRUDBaseRepository<Ulid, Article, ArticleSearchSpec>(context)
{
    public override DbSet<Article> GetDbSet()
    {
        return _context.Articles;
    }
    public override async Task<Article?> ReadAsync(Ulid id)
    {
        return await GetDbSet()
            .Include(a => a.ArticleKeySemantics.OrderBy(aks => aks.KeySemanticIndex))
            .Include(a => a.ArticleRelevances.OrderBy(ar => ar.EstimateRelevanceJobId))
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public override async Task<IEnumerable<Article>> ReadWithSearchSpecAsync(ArticleSearchSpec searchSpec)
    {
        return await GetDbSet()
            .Where(a => a.ProjectId == searchSpec.ProjectId)
            .Include(a => a.ArticleKeySemantics.OrderBy(aks => aks.KeySemanticIndex))
            .Include(a => a.ArticleRelevances.OrderBy(ar => ar.EstimateRelevanceJobId))
            .ToListAsync();
    }
}
