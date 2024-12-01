using llassist.Common.ViewModels;
using llassist.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace llassist.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppSettingsController : ControllerBase
{
    private readonly IAppSettingService _settingService;

    public AppSettingsController(IAppSettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppSettingViewModel>>> GetAll()
    {
        var settings = await _settingService.GetAllSettingsAsync();
        return Ok(settings);
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<AppSettingViewModel>> Get(string key)
    {
        var setting = await _settingService.GetSettingAsync(key);
        if (setting == null) return NotFound();
        return Ok(setting);
    }

    [HttpPost]
    public async Task<ActionResult<AppSettingViewModel>> Create(AppSettingViewModel setting)
    {
        var existingSetting = await _settingService.GetSettingAsync(setting.Key);
        if (existingSetting != null)
            return Conflict($"Setting with key '{setting.Key}' already exists");

        var createdSetting = await _settingService.CreateSettingAsync(setting);
        return CreatedAtAction(nameof(Get), new { key = setting.Key }, createdSetting);
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<AppSettingViewModel>> Update(string key, AppSettingViewModel setting)
    {
        var updatedSetting = await _settingService.UpdateSettingAsync(key, setting);
        if (updatedSetting == null) return NotFound();
        return Ok(updatedSetting);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        var result = await _settingService.DeleteSettingAsync(key);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("search")]
    public async Task<ActionResult<Dictionary<string, AppSettingViewModel>>> Search(SearchAppSettingViewModel searchSpec)
    {
        var settings = await _settingService.SearchAsync(searchSpec);
        return Ok(settings);
    }
} 