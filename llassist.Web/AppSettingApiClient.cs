using llassist.Common.Models;
using llassist.Common.ViewModels;

namespace llassist.Web;

public class AppSettingApiClient
{
    private readonly HttpClient _httpClient;

    public AppSettingApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<AppSettingViewModel>> GetAllSettingsAsync()
    {
        var settings = await _httpClient.GetFromJsonAsync<IEnumerable<AppSettingViewModel>>("api/appsettings");
        return settings ?? Enumerable.Empty<AppSettingViewModel>();
    }

    public async Task<AppSettingViewModel?> GetSettingAsync(string key)
    {
        return await _httpClient.GetFromJsonAsync<AppSettingViewModel>($"api/appsettings/{key}");
    }

    public async Task<AppSettingViewModel?> CreateSettingAsync(AppSettingViewModel setting)
    {
        var response = await _httpClient.PostAsJsonAsync("api/appsettings", setting);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppSettingViewModel>();
    }

    public async Task<AppSettingViewModel?> UpdateSettingAsync(string key, AppSettingViewModel setting)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/appsettings/{key}", setting);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppSettingViewModel>();
    }

    public async Task DeleteSettingAsync(string key)
    {
        var response = await _httpClient.DeleteAsync($"api/appsettings/{key}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<Dictionary<string, AppSettingViewModel>> GetSessionSettingsAsync()
    {
        var searchSpec = SearchAppSettingViewModel.CreateForFrontEndSession();
        var response = await _httpClient.PostAsJsonAsync("api/appsettings/search", searchSpec);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, AppSettingViewModel>>();
        return result ?? new Dictionary<string, AppSettingViewModel>();
    }

    public async Task<FileUploadSettings> GetFileUploadSettingsAsync()
    {
        var sessionSettings = await GetSessionSettingsAsync();

        return FileUploadSettings.Create(
            sessionSettings.GetValueOrDefault(AppSettingKeys.FileUploadMaxFileMB)?.Value,
            sessionSettings.GetValueOrDefault(AppSettingKeys.FileUploadAllowedExtensions)?.Value
        );
    }
} 