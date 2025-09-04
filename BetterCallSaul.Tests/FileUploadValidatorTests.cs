using BetterCallSaul.Infrastructure.Validators;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BetterCallSaul.Tests;

public class FileUploadValidatorTests
{
    [Fact]
    public void ValidateFile_ValidTextFile_ReturnsValid()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.txt");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        // Act
        var (isValid, errors) = FileUploadValidator.ValidateFile(fileMock.Object);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateFile_EmptyFile_ReturnsInvalid()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.txt");
        fileMock.Setup(f => f.Length).Returns(0);

        // Act
        var (isValid, errors) = FileUploadValidator.ValidateFile(fileMock.Object);

        // Assert
        Assert.False(isValid);
        Assert.Contains("file", errors.Keys);
    }

    [Fact]
    public void ValidateFile_FileTooLarge_ReturnsInvalid()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.Length).Returns(60 * 1024 * 1024); // 60MB
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        // Act
        var (isValid, errors) = FileUploadValidator.ValidateFile(fileMock.Object);

        // Assert
        Assert.False(isValid);
        Assert.Contains("file", errors.Keys);
    }

    [Fact]
    public void ValidateFile_InvalidExtension_ReturnsInvalid()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.exe");
        fileMock.Setup(f => f.Length).Returns(1024);

        // Act
        var (isValid, errors) = FileUploadValidator.ValidateFile(fileMock.Object);

        // Assert
        Assert.False(isValid);
        Assert.Contains("extension", errors.Keys);
    }

    [Fact]
    public void ValidateFile_NoExtension_ReturnsInvalid()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test");
        fileMock.Setup(f => f.Length).Returns(1024);

        // Act
        var (isValid, errors) = FileUploadValidator.ValidateFile(fileMock.Object);

        // Assert
        Assert.False(isValid);
        Assert.Contains("extension", errors.Keys);
    }
}