namespace llassist.Common.Models;

public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
} 

public static class AppSettingKeys
{
    #region LLM Model Settings
    public const string OpenAIApiKey = "OpenAI:ApiKey";
    #endregion

    #region File Upload Settings
    public const string FileUploadMaxFileMB = "FileUpload:MaxSizeMB";
    public const string FileUploadAllowedExtensions = "FileUpload:AllowedExtensions";
    #endregion

    #region Frontend Session Settings
    public static readonly string[] FrontEndSessionSettings = [
        FileUploadMaxFileMB,
        FileUploadAllowedExtensions
    ];
    #endregion
}
