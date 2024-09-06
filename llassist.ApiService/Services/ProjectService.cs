using llassist.ApiService.Repositories.Specifications;
using llassist.Common;
using llassist.Common.Models;
using llassist.Common.ViewModels;

namespace llassist.ApiService.Services;

public class ProjectService
{
    private readonly ICRUDRepository<Ulid, Project, ProjectSearchSpec> _projectRepository;

    public ProjectService(ICRUDRepository<Ulid, Project, ProjectSearchSpec> projectRepository)
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

    public async Task<ResearchQuestionsViewModel?> AddResearchQuestionAsync(Ulid projectId, AddEditResearchQuestionViewModel questionViewModel)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null)
        {
            return null;
        }

        var question = new Question
        {
            Text = questionViewModel.Text,
            Definitions = questionViewModel.Definitions
        };

        project.ResearchQuestions.Questions.Add(question);
        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ToResearchQuestionsViewModel(updated.ResearchQuestions) : null;
    }

    public async Task<ResearchQuestionsViewModel?> UpdateResearchQuestionAsync(Ulid projectId, int questionIndex, AddEditResearchQuestionViewModel questionViewModel)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null || questionIndex < 0 || questionIndex >= project.ResearchQuestions.Questions.Count)
        {
            return null;
        }

        var question = project.ResearchQuestions.Questions[questionIndex];
        question.Text = questionViewModel.Text;
        question.Definitions = questionViewModel.Definitions;

        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ToResearchQuestionsViewModel(updated.ResearchQuestions) : null;
    }

    public async Task<bool> DeleteResearchQuestionAsync(Ulid projectId, int questionIndex)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null || questionIndex < 0 || questionIndex >= project.ResearchQuestions.Questions.Count)
        {
            return false;
        }

        project.ResearchQuestions.Questions.RemoveAt(questionIndex);
        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null;
    }

    public async Task<ResearchQuestionsViewModel?> AddDefinitionAsync(Ulid projectId, string definition)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null)
        {
            return null;
        }

        project.ResearchQuestions.Definitions.Add(definition);
        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ToResearchQuestionsViewModel(updated.ResearchQuestions) : null;
    }

    public async Task<ResearchQuestionsViewModel?> UpdateDefinitionAsync(Ulid projectId, int definitionIndex, string definition)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null || definitionIndex < 0 || definitionIndex >= project.ResearchQuestions.Definitions.Count)
        {
            return null;
        }

        project.ResearchQuestions.Definitions[definitionIndex] = definition;
        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ToResearchQuestionsViewModel(updated.ResearchQuestions) : null;
    }

    public async Task<bool> DeleteDefinitionAsync(Ulid projectId, int definitionIndex)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null || definitionIndex < 0 || definitionIndex >= project.ResearchQuestions.Definitions.Count)
        {
            return false;
        }

        project.ResearchQuestions.Definitions.RemoveAt(definitionIndex);
        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null;
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
            ResearchQuestions = ToResearchQuestionsViewModel(project.ResearchQuestions)
        };
    }

    private static ResearchQuestionsViewModel ToResearchQuestionsViewModel(ResearchQuestions researchQuestions)
    {
        return new ResearchQuestionsViewModel
        {
            Definitions = researchQuestions.Definitions,
            Questions = researchQuestions.Questions.Select(q => new QuestionViewModel
            {
                Text = q.Text,
                Definitions = q.Definitions
            }).ToList()
        };
    }
}
