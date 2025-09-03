# Task: Password Reset Functionality

## Overview
- **Parent Feature**: Authentication System (AUTH-002 from 3_IMPLEMENTATION.md)
- **Complexity**: Low
- **Estimated Time**: 4 hours
- **Status**: Completed

## Dependencies
### Required Tasks
- [ ] 001-identity-setup.md: Identity foundation required
- [ ] 002-login-registration-ui.md: Login page needs forgot password link

### External Dependencies
- Email service (can use fake/console provider for MVP)

## Implementation Details
### Files to Create/Modify
- `Components/Pages/Account/ForgotPassword.razor`: Password reset request page
- `Components/Pages/Account/ResetPassword.razor`: Password reset form page
- `Services/IEmailService.cs`: Email service interface
- `Services/ConsoleEmailService.cs`: Console-based email service for development
- `Models/ViewModels/ForgotPasswordViewModel.cs`: Forgot password form model
- `Models/ViewModels/ResetPasswordViewModel.cs`: Reset password form model

### Code Patterns
- Use ASP.NET Core Identity password reset tokens
- Implement email service abstraction for future SMTP integration
- Follow same UI patterns as login/registration pages

### API/Data Structures
```csharp
public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink);
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

## Acceptance Criteria
- [ ] Forgot password page with email input
- [ ] Password reset token generation and validation
- [ ] Reset password page with secure token validation
- [ ] Email service sends reset instructions (console output for MVP)
- [ ] Successful password reset logs user out of other sessions
- [ ] Reset links expire after reasonable time (24 hours)
- [ ] Used tokens cannot be reused
- [ ] Appropriate error messages for invalid/expired tokens

## Testing Strategy
- Manual validation: Complete password reset flow from request to new password
- Security testing: Verify tokens expire and cannot be reused
- Error testing: Test invalid tokens, expired tokens, mismatched passwords

## System Stability
- How this task maintains operational state: Adds password recovery without breaking existing auth
- Rollback strategy if needed: Remove reset pages and email service
- Impact on existing functionality: None (adds new functionality)