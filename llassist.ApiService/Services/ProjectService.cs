using llassist.Common;
using llassist.Common.Models;
using llassist.Common.ViewModels;

namespace llassist.ApiService.Services;

public class ProjectService
{
    private readonly ICRUDRepository<Ulid, Project> _projectRepository;

    public ProjectService(ICRUDRepository<Ulid, Project> projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<ProjectViewModel> CreateProjectAsync(string name, string description)
    {
        var project = new Project
        {
            Name = name,
            Description = description
        };
        return ToViewModel(await _projectRepository.CreateAsync(project));
    }

    public async Task<ProjectViewModel?> GetProjectAsync(Ulid id)
    {
        return await _projectRepository.ReadAsync(id) is Project project ? ToViewModel(project) : null;
    }

    public async Task<IEnumerable<ProjectViewModel>> GetAllProjectsAsync()
    {
        return (await _projectRepository.ReadAllAsync()).Select(ToViewModel);
    }

    public async Task<ProjectViewModel?> AddArticlesToProjectAsync(Ulid projectId, IEnumerable<Article> articles)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null)
        {
            return null;
        }

        foreach (var article in articles)
        {
            project.Articles.Add(article);
        }

        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ToViewModel(updated) : null;
    }

    public async Task<bool> DeleteProjectAsync(Ulid id)
    {
        return await _projectRepository.DeleteAsync(id);
    }

    public static ProjectViewModel ToViewModel(Project project)
    {
        return new ProjectViewModel
        {
            Id = project.Id.ToString(),
            Name = project.Name,
            Description = project.Description,
            Articles = project.Articles.Select(article => new ArticleViewModel
            {
                Title = article.Title,
                Authors = article.Authors,
                Year = article.Year,
                Abstract = article.Abstract,
                MustRead = article.MustRead,
                Relevances = article.Relevances.Select(relevance => new RelevanceViewModel
                {
                    Question = relevance.Question,
                    RelevanceScore = relevance.RelevanceScore,
                    IsRelevant = relevance.IsRelevant
                }).ToList()
            }).ToList(),
        };
    }
    public async Task<ProjectViewModel?> UpdateProjectAsync(Ulid id, ProjectViewModel projectViewModel)
    {
        if (string.IsNullOrWhiteSpace(projectViewModel.Name) || string.IsNullOrWhiteSpace(projectViewModel.Description))
        {
            throw new ArgumentException("Project name and description cannot be empty.");
        }

        var project = await _projectRepository.ReadAsync(id);
        if (project == null)
        {
            return null;
        }

        project.Name = projectViewModel.Name;
        project.Description = projectViewModel.Description;

        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ToViewModel(updated) : null;
    }
}
