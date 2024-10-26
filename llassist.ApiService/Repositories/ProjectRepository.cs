using llassist.ApiService.Repositories.Specifications;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace llassist.ApiService.Repositories;

public class ProjectRepository(ApplicationDbContext context) : CRUDBaseRepository<Ulid, Project, BaseSearchSpec>(context)
{
    public override DbSet<Project> GetDbSet()
    {
        return _context.Projects;
    }

    public override async Task<Project?> ReadAsync(Ulid id)
    {
        return await GetDbSet()
            .Include(p => p.Articles.OrderBy(a => a.Id))
                .ThenInclude(a => a.ArticleKeySemantics.OrderBy(aks => aks.KeySemanticIndex))
            .Include(p => p.Articles.OrderBy(a => a.Id))
                .ThenInclude(a => a.ArticleRelevances.OrderBy(ar => ar.EstimateRelevanceJobId))
            .Include(p => p.ProjectDefinitions.OrderBy(pd => pd.Id))
            .Include(p => p.ResearchQuestions.OrderBy(rq => rq.Id))
                .ThenInclude(rq => rq.QuestionDefinitions.OrderBy(qd => qd.Id))
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public override async Task<IEnumerable<Project>> ReadAllAsync()
    {
        // Eager loading is disabled to avoid heavy load on large datasets
        return await GetDbSet().ToListAsync();
    }
}
