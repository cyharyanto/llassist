using llassist.ApiService.Repositories.Specifications;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace llassist.ApiService.Repositories;

public class EstimateRelevanceJobRepository(ApplicationDbContext context) : CRUDBaseRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec>(context)
{
    public override DbSet<EstimateRelevanceJob> GetDbSet()
    {
        return _context.EstimateRelevanceJobs;
    }

    public override async Task<EstimateRelevanceJob?> ReadAsync(Ulid id)
    {
        return await GetDbSet()
            .Include(erj => erj.Snapshots.OrderBy(s => s.Id))
            .FirstOrDefaultAsync(erj => erj.Id == id);
    }

    public override async Task<IEnumerable<EstimateRelevanceJob>> ReadWithSearchSpecAsync(EstimateRelevanceJobSearchSpec searchSpec)
    {
        return await GetDbSet()
            .Where(a => a.ProjectId == searchSpec.ProjectId)
            .Include(erj => erj.Snapshots.OrderBy(s => s.Id))
            .ToListAsync();
    }
}
