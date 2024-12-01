namespace llassist.Common.Models;

public class FileUploadSettings
{
    private const int DefaultMaxSizeMB = 1;
    private static readonly string[] DefaultAllowedExtensions = [".csv"];

    public int MaxSizeMB { get; set; }
    public string[] AllowedExtensions { get; set; }
    public int MaxSizeBytes => MaxSizeMB * 1024 * 1024;

    private FileUploadSettings(int maxSizeMB, string[] allowedExtensions)
    {
        MaxSizeMB = maxSizeMB;
        AllowedExtensions = allowedExtensions;
    }

    public static FileUploadSettings Create(string? maxSizeMBStr, string? allowedExtensions)
    {
        var maxSize = ParseMaxSizeWithDefault(maxSizeMBStr);
        var extensions = ParseExtensionsWithDefault(allowedExtensions);
        
        return new FileUploadSettings(maxSize, extensions);
    }

    private static int ParseMaxSizeWithDefault(string? maxSizeMBStr)
    {
        return !string.IsNullOrWhiteSpace(maxSizeMBStr) && 
               int.TryParse(maxSizeMBStr, out int parsedSize) && 
               parsedSize > 0
               ? parsedSize 
               : DefaultMaxSizeMB;
    }

    private static string[] ParseExtensionsWithDefault(string? allowedExtensions)
    {
        var parsed = (allowedExtensions ?? string.Empty)
            .Split(',')
            .Select(ext => ext.Trim())
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .ToArray();

        return parsed.Length > 0 ? parsed : DefaultAllowedExtensions;
    }
}