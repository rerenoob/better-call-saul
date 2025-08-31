# CRUSH.md - Agentic Coding Assistant Guidelines

## Build & Development Commands
```bash
dotnet build              # Build project
dotnet run                # Run app (http://localhost:5173, https://localhost:7191)
dotnet watch              # Run with hot reload
dotnet restore            # Restore packages
dotnet test               # Run tests (when available)
```

## Code Style Guidelines
- **Namespace**: `better_call_saul` (dashes converted to underscores)
- **Framework**: .NET 8.0 with Blazor Server
- **Nullable**: Reference types enabled
- **Implicit Usings**: Enabled globally
- **File Organization**: Components in `/Components/` with Pages/Layout subdirectories
- **Naming**: PascalCase for classes/methods, camelCase for parameters/variables
- **Error Handling**: Use built-in ASP.NET Core exception handling

## Architecture Patterns
- Interactive Server Components (`AddInteractiveServerComponents()`)
- Razor Components with `@page` routing
- Bootstrap 5 + custom CSS styling
- Static files served from `wwwroot/`
- Antiforgery protection enabled

## Development Environment
- Default ports: 5173 (HTTP), 7191 (HTTPS)
- Hot reload supported via `dotnet watch`
- HSTS and HTTPS redirection in production
- Development exception handling configured

## Component Structure
- Root: `App.razor`, `Routes.razor`
- Pages: `/Components/Pages/*.razor`
- Layout: `/Components/Layout/*.razor`
- Global imports: `_Imports.razor`

💘 Generated with Crush
Co-Authored-By: Crush <crush@charm.land>