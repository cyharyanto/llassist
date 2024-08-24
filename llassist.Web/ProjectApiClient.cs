using llassist.Common.ViewModels;

namespace llassist.Web;

public class ProjectApiClient
{
    private readonly HttpClient _httpClient;

    public ProjectApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProjectViewModel?> CreateProjectAsync(CreateProjectViewModel createProject)
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

    public async Task<ProjectViewModel?> UpdateProjectAsync(string id, CreateProjectViewModel updateProject)
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

    public async Task<ProjectViewModel?> UploadCSVAsync(string projectId, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        using var fileStream = file.OpenReadStream();
        var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", file.FileName);

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
}
