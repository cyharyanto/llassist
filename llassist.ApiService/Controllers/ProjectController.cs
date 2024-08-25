using CsvHelper;
using llassist.ApiService.Services;
using llassist.Common.Models;
using llassist.Common.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace llassist.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectController : ControllerBase
{
    private readonly ProjectService _projectService;
    private readonly NLPService _nlpService;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(ProjectService projectService, NLPService nlpService, ILogger<ProjectController> logger)
    {
        _projectService = projectService;
        _nlpService = nlpService;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateProject([FromBody] CreateEditProjectViewModel createProject)
    {
        var project = await _projectService.CreateProjectAsync(createProject.Name, createProject.Description);
        return Ok(project);
    }

    [HttpGet("{projectId}")]
    public async Task<IActionResult> GetProject(string projectId)
    {
        var project = await _projectService.GetProjectAsync(Ulid.Parse(projectId));
        if (project == null)
        {
            return NotFound();
        }
        return Ok(project);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProjects()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        return Ok(projects);
    }

    [HttpPut("{projectId}")]
    public async Task<IActionResult> UpdateProject(string projectId, [FromBody] CreateEditProjectViewModel updateProject)
    {
        var project = await _projectService.GetProjectAsync(Ulid.Parse(projectId));
        if (project == null)
        {
            return NotFound();
        }

        project.Name = updateProject.Name;
        project.Description = updateProject.Description;

        var updatedProject = await _projectService.UpdateProjectAsync(Ulid.Parse(projectId), project);
        return Ok(updatedProject);
    }

    [HttpDelete("{projectId}")]
    public async Task<IActionResult> DeleteProject(string projectId)
    {
        var success = await _projectService.DeleteProjectAsync(Ulid.Parse(projectId));
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("upload/{projectId}")]
    public async Task<IActionResult> UploadCSV(string projectId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        var reader = new StreamReader(file.OpenReadStream());
        var articles = ArticleService.ReadArticlesFromCsv(reader);
        var project = await _projectService.AddArticlesToProjectAsync(Ulid.Parse(projectId), articles);

        return Ok(project);
    }

    [HttpGet("process/{projectId}")]
    public async Task<IActionResult> ProcessArticles(string projectId)
    {
        // Retrieve articles based on the id
        // Process articles using NLPService
        // Update progress and results
        var project = await _projectService.GetProjectAsync(Ulid.Parse(projectId));

        return Ok(project);
    }

    [HttpGet("download/{projectId}")]
    public async Task<IActionResult> DownloadResults(string projectId)
    {
        // Retrieve processed results
        // Generate CSV file
        // Return file for download

        return File(new byte[0], "text/csv", "results.csv");
    }

    [HttpPost("{projectId}/definitions")]
    public async Task<IActionResult> AddDefinition(string projectId, [FromBody] AddEditDefinitionViewModel definitionViewModel)
    {
        var researchQuestions = await _projectService.AddDefinitionAsync(Ulid.Parse(projectId), definitionViewModel.Definition);
        if (researchQuestions == null)
        {
            return NotFound();
        }
        return Ok(researchQuestions);
    }

    [HttpPut("{projectId}/definitions/{definitionIndex}")]
    public async Task<IActionResult> UpdateDefinition(string projectId, int definitionIndex, [FromBody] AddEditDefinitionViewModel definitionViewModel)
    {
        var researchQuestions = await _projectService.UpdateDefinitionAsync(Ulid.Parse(projectId), definitionIndex, definitionViewModel.Definition);
        if (researchQuestions == null)
        {
            return NotFound();
        }
        return Ok(researchQuestions);
    }

    [HttpDelete("{projectId}/definitions/{definitionIndex}")]
    public async Task<IActionResult> DeleteDefinition(string projectId, int definitionIndex)
    {
        var success = await _projectService.DeleteDefinitionAsync(Ulid.Parse(projectId), definitionIndex);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("{projectId}/research-questions")]
    public async Task<IActionResult> AddResearchQuestion(string projectId, [FromBody] AddEditResearchQuestionViewModel questionViewModel)
    {
        var researchQuestions = await _projectService.AddResearchQuestionAsync(Ulid.Parse(projectId), questionViewModel);
        if (researchQuestions == null)
        {
            return NotFound();
        }
        return Ok(researchQuestions);
    }

    [HttpPut("{projectId}/research-questions/{questionIndex}")]
    public async Task<IActionResult> UpdateResearchQuestion(string projectId, int questionIndex, [FromBody] AddEditResearchQuestionViewModel questionViewModel)
    {
        var researchQuestions = await _projectService.UpdateResearchQuestionAsync(Ulid.Parse(projectId), questionIndex, questionViewModel);
        if (researchQuestions == null)
        {
            return NotFound();
        }
        return Ok(researchQuestions);
    }

    [HttpDelete("{projectId}/research-questions/{questionIndex}")]
    public async Task<IActionResult> DeleteResearchQuestion(string projectId, int questionIndex)
    {
        var success = await _projectService.DeleteResearchQuestionAsync(Ulid.Parse(projectId), questionIndex);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }
}
