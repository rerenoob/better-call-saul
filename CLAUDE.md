# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Blazor Server application using .NET 8 called "better-call-saul" - an AI Lawyer application for the masses. The project uses interactive server-side rendering with Blazor components.

## Common Commands

**Build and Run:**
```bash
dotnet build              # Build the project
dotnet run                # Run the application (http://localhost:5173, https://localhost:7191)
dotnet watch              # Run with hot reload during development
```

**Testing:**
```bash
dotnet test               # Run tests (if test projects exist)
```

**Package Management:**
```bash
dotnet restore            # Restore NuGet packages
```

## Architecture

- **Entry Point:** `Program.cs` - Standard ASP.NET Core startup with Blazor Server configuration
- **Components Structure:**
  - `Components/App.razor` - Root HTML document template
  - `Components/Routes.razor` - Routing configuration
  - `Components/Pages/` - Page components (Home, Counter, Weather, Error)
  - `Components/Layout/` - Layout components (MainLayout, NavMenu)
  - `Components/_Imports.razor` - Global using statements for components

- **Configuration:**
  - Uses interactive server components (`AddInteractiveServerComponents()`)
  - Antiforgery protection enabled
  - HTTPS redirection and HSTS for production
  - Static files served from `wwwroot/`

- **Styling:** Bootstrap 5 + custom CSS (`app.css`)

- **Namespace:** Uses `better_call_saul` namespace (dashes converted to underscores)

## Development Notes

- Target Framework: .NET 8.0
- Nullable reference types enabled
- Implicit usings enabled
- Default launch URLs: http://localhost:5173, https://localhost:7191
- Development environment configured for hot reload and browser launch