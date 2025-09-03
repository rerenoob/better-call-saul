# Task: Custom Login and Registration Pages

## Overview
- **Parent Feature**: Authentication System (AUTH-002 from 3_IMPLEMENTATION.md)
- **Complexity**: Medium
- **Estimated Time**: 5 hours
- **Status**: Completed

## Dependencies
### Required Tasks
- [ ] 001-identity-setup.md: Identity foundation must be in place

### External Dependencies
- Bootstrap 5 (already available in project)
- jQuery for client-side validation

## Implementation Details
### Files to Create/Modify
- `Components/Pages/Account/Login.razor`: Custom login page
- `Components/Pages/Account/Register.razor`: Custom registration page
- `Components/Pages/Account/Logout.razor`: Logout confirmation page
- `Models/ViewModels/LoginViewModel.cs`: Login form model
- `Models/ViewModels/RegisterViewModel.cs`: Registration form model
- `Components/Layout/NavMenu.razor`: Add login/logout links

### Code Patterns
- Follow existing Blazor component patterns in the project
- Use Bootstrap form styling consistent with app theme
- Implement client-side validation with data annotations

### API/Data Structures
```csharp
public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
}
```

## Acceptance Criteria
- [ ] Custom login page with email/password fields and "Remember Me"
- [ ] Custom registration page with first name, last name, email, password
- [ ] Client-side validation working on both forms
- [ ] Successful login redirects to home page
- [ ] Failed login shows appropriate error messages
- [ ] Registration creates new user and logs them in
- [ ] Navigation menu shows login/logout links appropriately
- [ ] Logout functionality works correctly

## Testing Strategy
- Manual validation: Test complete registration and login flow
- Error testing: Test invalid emails, weak passwords, mismatched passwords
- Navigation testing: Verify menu links work correctly
- Session testing: Verify "Remember Me" functionality

## System Stability
- How this task maintains operational state: Replaces scaffolded UI with custom implementation
- Rollback strategy if needed: Revert to scaffolded Identity pages
- Impact on existing functionality: Improves user experience, maintains same functionality