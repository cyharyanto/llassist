using llassist.Common.Models;
using llassist.Common.Validators;
using Xunit;
using Assert = Xunit.Assert;

namespace llassist.Tests;

public class FileValidatorTests
{
    private readonly FileUploadSettings _defaultSettings;

    public FileValidatorTests()
    {
        _defaultSettings = FileUploadSettings.Create("5", ".jpg,.png,.pdf");
    }

    [Fact]
    public void ValidateFile_ValidFile_ReturnsValid()
    {
        // Arrange
        var fileName = "test.jpg";
        var fileSize = 1024 * 1024; // 1MB

        // Act
        var result = FileValidator.ValidateFile(fileName, fileSize, _defaultSettings);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_EmptyFileName_ReturnsInvalid()
    {
        // Arrange
        var fileName = "";
        var fileSize = 1024;

        // Act
        var result = FileValidator.ValidateFile(fileName, fileSize, _defaultSettings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid filename", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_ZeroFileSize_ReturnsInvalid()
    {
        // Arrange
        var fileName = "test.jpg";
        var fileSize = 0L;

        // Act
        var result = FileValidator.ValidateFile(fileName, fileSize, _defaultSettings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("File is empty", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_ExceedingSizeLimit_ReturnsInvalid()
    {
        // Arrange
        var fileName = "test.jpg";
        var fileSize = 6 * 1024 * 1024L; // 6MB

        // Act
        var result = FileValidator.ValidateFile(fileName, fileSize, _defaultSettings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal($"File size exceeds the limit of {_defaultSettings.MaxSizeMB}MB", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_InvalidExtension_ReturnsInvalid()
    {
        // Arrange
        var fileName = "test.txt";
        var fileSize = 1024;

        // Act
        var result = FileValidator.ValidateFile(fileName, fileSize, _defaultSettings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("File type .txt is not supported", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_FileNameWithoutExtension_ReturnsInvalid()
    {
        // Arrange
        var fileName = "testfile";
        var fileSize = 1024;

        // Act
        var result = FileValidator.ValidateFile(fileName, fileSize, _defaultSettings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("File type  is not supported", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFile_FileNameWithPath_ValidatesFileName()
    {
        // Arrange
        var fileName = "/path/to/test.jpg";
        var fileSize = 1024;

        // Act
        var result = FileValidator.ValidateFile(fileName, fileSize, _defaultSettings);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateFile_UppercaseExtension_ValidatesCorrectly()
    {
        // Arrange
        var fileName = "test.JPG";
        var fileSize = 1024;

        // Act
        var result = FileValidator.ValidateFile(fileName, fileSize, _defaultSettings);

        // Assert
        Assert.True(result.IsValid);
    }
}