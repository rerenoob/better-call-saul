using BetterCallSaul.UserService.Models.Entities;
using Xunit;

namespace BetterCallSaul.UserService.Tests;

public class UserServiceStructureTests
{
    [Fact]
    public void User_Entity_Has_Required_Properties()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            BarNumber = "12345",
            LawFirm = "Doe & Associates"
        };

        // Act & Assert
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("John Doe", user.FullName);
        Assert.Equal("12345", user.BarNumber);
        Assert.Equal("Doe & Associates", user.LawFirm);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void RegistrationCode_Entity_Has_Required_Properties()
    {
        // Arrange
        var code = new RegistrationCode
        {
            Code = "ABC123DEF456",
            CreatedBy = "System",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act & Assert
        Assert.Equal("ABC123DEF456", code.Code);
        Assert.Equal("System", code.CreatedBy);
        Assert.False(code.IsUsed);
        Assert.True(code.IsValid);
    }

    [Fact]
    public void RegistrationCode_IsValid_Returns_False_When_Used()
    {
        // Arrange
        var code = new RegistrationCode
        {
            Code = "USEDCODE123",
            CreatedBy = "System",
            IsUsed = true,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act & Assert
        Assert.False(code.IsValid);
    }

    [Fact]
    public void RegistrationCode_IsValid_Returns_False_When_Expired()
    {
        // Arrange
        var code = new RegistrationCode
        {
            Code = "EXPIREDCODE",
            CreatedBy = "System",
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        Assert.False(code.IsValid);
    }

    [Fact]
    public void AuditLog_Entity_Has_Required_Properties()
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Action = "UserLogin",
            Description = "User logged in successfully",
            Level = Models.Enums.AuditLogLevel.Info,
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0"
        };

        // Act & Assert
        Assert.Equal("UserLogin", auditLog.Action);
        Assert.Equal("User logged in successfully", auditLog.Description);
        Assert.Equal(Models.Enums.AuditLogLevel.Info, auditLog.Level);
        Assert.Equal("192.168.1.1", auditLog.IpAddress);
        Assert.Equal("Mozilla/5.0", auditLog.UserAgent);
    }
}