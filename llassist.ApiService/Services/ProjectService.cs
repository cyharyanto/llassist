using llassist.ApiService.Repositories.Specifications;
using llassist.Common;
using llassist.Common.Mappers;
using llassist.Common.Models;
using llassist.Common.ViewModels;

namespace llassist.ApiService.Services;

public class ProjectService
{
    private readonly ICRUDRepository<Ulid, Project, BaseSearchSpec> _projectRepository;

    public ProjectService(ICRUDRepository<Ulid, Project, BaseSearchSpec> projectRepository)
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
        return ModelMappers.ToProjectViewModel(await _projectRepository.CreateAsync(project));
    }

    public async Task<ProjectViewModel?> GetProjectAsync(Ulid id)
    {
        return await _projectRepository.ReadAsync(id) is Project project ? 
            ModelMappers.ToProjectViewModel(project) : null;
    }

    public async Task<IEnumerable<ProjectViewModel>> GetAllProjectsAsync()
    {
        return (await _projectRepository.ReadAllAsync()).Select(ModelMappers.ToProjectViewModel);
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
        return updated != null ? ModelMappers.ToProjectViewModel(updated) : null;
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
        return updated != null ? ModelMappers.ToProjectViewModel(updated) : null;
    }

    public async Task<ResearchQuestionsViewModel?> AddResearchQuestionAsync(Ulid projectId, AddEditResearchQuestionViewModel questionViewModel)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null)
        {
            return null;
        }

        var researchQuestion = new ResearchQuestion
        {
            QuestionText = questionViewModel.Text,
            ProjectId = projectId,
        };

        researchQuestion.QuestionDefinitions = questionViewModel.Definitions.Select(d => new QuestionDefinition
        {
            Definition = d,
            ResearchQuestionId = researchQuestion.Id
        }).ToList();

        project.ResearchQuestions.Add(researchQuestion);
        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ModelMappers.ToResearchQuestionsViewModel(updated.ProjectDefinitions, updated.ResearchQuestions) : null;
    }

    public async Task<ResearchQuestionsViewModel?> UpdateResearchQuestionAsync(Ulid projectId, int questionIndex, AddEditResearchQuestionViewModel questionViewModel)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null || questionIndex < 0 || questionIndex >= project.ResearchQuestions.Count)
        {
            return null;
        }

        var question = project.ResearchQuestions.ElementAt(questionIndex);
        question.QuestionText = questionViewModel.Text;
        question.QuestionDefinitions = questionViewModel.Definitions.Select(d => new QuestionDefinition
        {
            Definition = d,
            ResearchQuestionId = question.Id
        }).ToList();

        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ModelMappers.ToResearchQuestionsViewModel(updated.ProjectDefinitions, updated.ResearchQuestions) : null;
    }

    public async Task<bool> DeleteResearchQuestionAsync(Ulid projectId, int questionIndex)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null || questionIndex < 0 || questionIndex >= project.ResearchQuestions.Count)
        {
            return false;
        }

        var questionToRemove = project.ResearchQuestions.ElementAt(questionIndex);
        project.ResearchQuestions.Remove(questionToRemove);
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

        project.ProjectDefinitions.Add(new ProjectDefinition
        {
            Definition = definition,
            ProjectId = projectId,
        });

        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ModelMappers.ToResearchQuestionsViewModel(updated.ProjectDefinitions, updated.ResearchQuestions) : null;
    }

    public async Task<ResearchQuestionsViewModel?> UpdateDefinitionAsync(Ulid projectId, int definitionIndex, string definition)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null || definitionIndex < 0 || definitionIndex >= project.ProjectDefinitions.Count)
        {
            return null;
        }

        project.ProjectDefinitions.ElementAt(definitionIndex).Definition = definition;
        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null ? ModelMappers.ToResearchQuestionsViewModel(updated.ProjectDefinitions, updated.ResearchQuestions) : null;
    }

    public async Task<bool> DeleteDefinitionAsync(Ulid projectId, int definitionIndex)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null || definitionIndex < 0 || definitionIndex >= project.ProjectDefinitions.Count)
        {
            return false;
        }

        var projectDefinition = project.ProjectDefinitions.ElementAt(definitionIndex);
        project.ProjectDefinitions.Remove(projectDefinition);
        var updated = await _projectRepository.UpdateAsync(project);
        return updated != null;
    }
}
