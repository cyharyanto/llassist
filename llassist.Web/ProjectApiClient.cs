using llassist.Common.Models;
using llassist.Common.Validators;
using llassist.Common.ViewModels;
using Microsoft.AspNetCore.Components.Forms;

namespace llassist.Web;

public class ProjectApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AppSettingApiClient _appSettingApiClient;

    public ProjectApiClient(
        HttpClient httpClient,
        AppSettingApiClient appSettingApiClient)
    {
        _httpClient = httpClient;
        _appSettingApiClient = appSettingApiClient;
    }

    public async Task<ProjectViewModel?> CreateProjectAsync(CreateEditProjectViewModel createProject)
    {
        var response = await _httpClient.PostAsJsonAsync("api/project/create", createProject);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectViewModel>();
    }

    public async Task<ProjectViewModel?> GetProjectAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<ProjectViewModel>($"api/project/{id}");
    }

    public async Task<IEnumerable<ProjectViewModel>> GetAllProjectsAsync()
    {
        var projects = await _httpClient.GetFromJsonAsync<IEnumerable<ProjectViewModel>>("api/project");
        return projects ?? Enumerable.Empty<ProjectViewModel>();
    }

    public async Task<ProjectViewModel?> UpdateProjectAsync(string id, CreateEditProjectViewModel updateProject)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/project/{id}", updateProject);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectViewModel>();
    }

    public async Task DeleteProjectAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/project/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ProjectViewModel?> UploadCSVAsync(string projectId, IBrowserFile file)
    {
        var uploadSettings = await _appSettingApiClient.GetFileUploadSettingsAsync();
        var validationResult = FileValidator.ValidateFile(
            file.Name,
            file.Size,
            uploadSettings
        );

        if (!validationResult.IsValid)
            throw new InvalidOperationException(validationResult.ErrorMessage);

        using var content = new MultipartFormDataContent();
        using var fileStream = file.OpenReadStream(maxAllowedSize: uploadSettings.MaxSizeBytes);
        var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", file.Name);

        var response = await _httpClient.PostAsync($"api/project/upload/{projectId}", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectViewModel>();
    }

    public async Task<ProjectViewModel?> ProcessArticlesAsync(string projectId)
    {
        return await _httpClient.GetFromJsonAsync<ProjectViewModel>($"api/project/process/{projectId}");
    }

    public async Task<byte[]> DownloadResultsAsync(string projectId)
    {
        var response = await _httpClient.GetAsync($"api/project/download/{projectId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<ResearchQuestionsViewModel?> AddDefinitionAsync(string projectId, AddEditDefinitionViewModel definitionViewModel)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/project/{projectId}/definitions", definitionViewModel);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ResearchQuestionsViewModel>();
    }

    public async Task<ResearchQuestionsViewModel?> UpdateDefinitionAsync(string projectId, int definitionIndex, AddEditDefinitionViewModel definitionViewModel)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/project/{projectId}/definitions/{definitionIndex}", definitionViewModel);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ResearchQuestionsViewModel>();
    }

    public async Task DeleteDefinitionAsync(string projectId, int definitionIndex)
    {
        var response = await _httpClient.DeleteAsync($"api/project/{projectId}/definitions/{definitionIndex}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ResearchQuestionsViewModel?> AddResearchQuestionAsync(string projectId, AddEditResearchQuestionViewModel questionViewModel)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/project/{projectId}/research-questions", questionViewModel);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ResearchQuestionsViewModel>();
    }

    public async Task<ResearchQuestionsViewModel?> UpdateResearchQuestionAsync(string projectId, int questionIndex, AddEditResearchQuestionViewModel questionViewModel)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/project/{projectId}/research-questions/{questionIndex}", questionViewModel);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ResearchQuestionsViewModel>();
    }

    public async Task DeleteResearchQuestionAsync(string projectId, int questionIndex)
    {
        var response = await _httpClient.DeleteAsync($"api/project/{projectId}/research-questions/{questionIndex}");
        response.EnsureSuccessStatusCode();
    }
}
