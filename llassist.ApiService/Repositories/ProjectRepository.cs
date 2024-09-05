using llassist.Common;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace llassist.ApiService.Repositories;

public class ProjectRepository : ICRUDRepository<Ulid, Project>
{
    private readonly ApplicationDbContext _context;

    public ProjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Project> CreateAsync(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<bool> DeleteAsync(Ulid id)
    {
        var project = _context.Projects.Find(id);
        if (project == null)
        {
            return false;
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Project>> ReadAllAsync()
    {
        return await _context.Projects.ToListAsync(); // Eager loading is disabled to avoid heavy load on large datasets
    }

    public async Task<Project?> ReadAsync(Ulid id)
    {
        return await _context.Projects
                             .Include(p => p.Articles)
                             .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        _context.Entry(project).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return project;
    }
}
