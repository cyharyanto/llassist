using llassist.Common.Models;
using llassist.Common.ViewModels;
using llassist.ApiService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace llassist.ApiService.Services;

public interface IAppSettingService
{
    Task<AppSettingViewModel?> GetSettingAsync(string key);
    Task<IEnumerable<AppSettingViewModel>> GetAllSettingsAsync();
    Task<AppSettingViewModel> CreateSettingAsync(AppSettingViewModel setting);
    Task<AppSettingViewModel?> UpdateSettingAsync(string key, AppSettingViewModel setting);
    Task<bool> DeleteSettingAsync(string key);
    Task<Dictionary<string, AppSettingViewModel>> SearchAsync(SearchAppSettingViewModel searchSpec);
} 

public class AppSettingService : IAppSettingService
{
    private readonly ApplicationDbContext _context;

    public AppSettingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppSettingViewModel?> GetSettingAsync(string key)
    {
        var setting = await _context.AppSettings.FindAsync(key);
        return setting == null ? null : ToDTO(setting);
    }

    public async Task<IEnumerable<AppSettingViewModel>> GetAllSettingsAsync()
    {
        var settings = await _context.AppSettings.ToListAsync();
        return settings.Select(ToDTO);
    }

    public async Task<AppSettingViewModel> CreateSettingAsync(AppSettingViewModel settingDto)
    {
        var setting = new AppSetting
        {
            Key = settingDto.Key,
            Value = settingDto.Value,
            Description = settingDto.Description ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.AppSettings.Add(setting);
        await _context.SaveChangesAsync();

        return ToDTO(setting);
    }

    public async Task<AppSettingViewModel?> UpdateSettingAsync(string key, AppSettingViewModel settingDto)
    {
        var setting = await _context.AppSettings.FindAsync(key);
        if (setting == null) return null;

        setting.Value = settingDto.Value;
        setting.Description = settingDto.Description ?? string.Empty;
        setting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ToDTO(setting);
    }

    public async Task<bool> DeleteSettingAsync(string key)
    {
        var setting = await _context.AppSettings.FindAsync(key);
        if (setting == null) return false;

        _context.AppSettings.Remove(setting);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Dictionary<string, AppSettingViewModel>> SearchAsync(SearchAppSettingViewModel searchSpec)
    {
        IQueryable<AppSetting> query = _context.AppSettings;
        bool hasSearchQuery = false;

        if (searchSpec.Keys != null && searchSpec.Keys.Any())
        {
            query = query.Where(s => searchSpec.Keys.Contains(s.Key));
            hasSearchQuery = true;
        }

        if (!hasSearchQuery)
        {
            return [];
        }

        return await query
            .ToDictionaryAsync(
                s => s.Key,
                s => ToDTO(s)
            );
    }

    private static AppSettingViewModel ToDTO(AppSetting setting) => new()
    {
        Key = setting.Key,
        Value = setting.Value,
        Description = setting.Description
    };
} 