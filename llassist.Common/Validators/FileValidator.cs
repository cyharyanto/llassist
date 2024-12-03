using llassist.Common.Models;

namespace llassist.Common.Validators;

public class FileValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

public static class FileValidator
{
    public static FileValidationResult ValidateFile(string fileName, long fileSize, FileUploadSettings settings)
    {
        // Sanitize by removing any path components from the filename
        var sanitizedFileName = Path.GetFileName(fileName); 
        if (string.IsNullOrEmpty(sanitizedFileName))
            return new FileValidationResult { 
                IsValid = false, 
                ErrorMessage = "Invalid filename" 
            };

        if (fileSize == 0)
            return new FileValidationResult { 
                IsValid = false, 
                ErrorMessage = "File is empty" 
            };

        if (fileSize > settings.MaxSizeBytes)
            return new FileValidationResult { 
                IsValid = false, 
                ErrorMessage = $"File size exceeds the limit of {settings.MaxSizeMB}MB" 
            };

        var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
        if (!settings.AllowedExtensions.Contains(extension))
            return new FileValidationResult { 
                IsValid = false, 
                ErrorMessage = $"File type {extension} is not supported. Allowed types: {string.Join(", ", settings.AllowedExtensions)}" 
            };

        return new FileValidationResult { IsValid = true };
    }
}