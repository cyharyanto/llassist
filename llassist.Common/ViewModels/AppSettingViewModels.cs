using llassist.Common.Models;

namespace llassist.Common.ViewModels;
public class AppSettingViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
} 

public class SearchAppSettingViewModel
{
    public IList<string> Keys { get; set; } = [];

    public static SearchAppSettingViewModel CreateForFrontEndSession()
    {
        return new SearchAppSettingViewModel 
        {
            Keys = [.. AppSettingKeys.FrontEndSessionSettings]
        };
    }
}
