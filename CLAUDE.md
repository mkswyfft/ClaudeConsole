# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Project Overview

ClaudeConsole is a WPF desktop application that provides a terminal-like interface for interacting with Claude AI. It wraps the Claude CLI and adds project management, tabbed sessions, and favorites functionality.

**Tech Stack:**
- Framework: WPF on .NET 8
- Language: VB.NET
- IDE: Visual Studio 2022 Community
- Architecture: MVVM pattern with CommunityToolkit.Mvvm

## Build Commands

```bash
# Build the solution
dotnet build ClaudeConsole.sln

# Clean and rebuild
dotnet clean && dotnet build

# Run the application
dotnet run --project src/ClaudeConsole/ClaudeConsole.vbproj

# Run tests (when available)
dotnet test tests/ClaudeConsole.Tests/ClaudeConsole.Tests.vbproj
```

## Project Structure

```
ClaudeConsole/
├── ClaudeConsole.sln              # Solution file
├── CLAUDE.md                       # This file
├── PROJECT_NOTES.md               # Feature planning and architecture
├── .mcp.json                      # MCP server configuration
├── .gitignore                     # Git ignore rules
├── src/ClaudeConsole/             # Main application
│   ├── ClaudeConsole.vbproj       # Project file
│   ├── Application.xaml           # App entry point + resources
│   ├── MainWindow.xaml            # Main window with tabs
│   ├── TerminalView.xaml          # Terminal UserControl
│   ├── Models/                    # Data models
│   │   └── TabSession.vb
│   ├── ViewModels/                # MVVM view models
│   │   ├── MainViewModel.vb
│   │   └── TabViewModel.vb
│   ├── Services/                  # Business logic
│   │   └── ClaudeCliService.vb
│   ├── Controls/                  # Custom WPF controls
│   └── Utilities/                 # Helper classes
│       └── AnsiParser.vb
├── tests/                         # Unit tests (planned)
├── docs/                          # Documentation
└── assets/
    └── icons/                     # Application icons
```

## Key Files

| File | Purpose |
|------|---------|
| `MainWindow.xaml` | Main window with tab bar and content area |
| `TerminalView.xaml` | Terminal-like UI for Claude CLI interaction |
| `MainViewModel.vb` | Tab management, commands for new/close tabs |
| `TabViewModel.vb` | Individual session management, CLI interaction |
| `ClaudeCliService.vb` | Claude CLI process lifecycle and I/O |
| `AnsiParser.vb` | ANSI escape code parsing for colored output |
| `Application.xaml` | Dark theme styles and resources |

## MCP Configuration

GitHub MCP server is configured for issue management:
- Uses `@modelcontextprotocol/server-github`
- Token from environment variable `claude_github_environment_token`

## Development Guidelines

### VB.NET Conventions
- Use `Option Strict On` and `Option Explicit On` in all files
- Prefer explicit types over inference where clarity helps
- Use `Async/Await` for all I/O operations
- Use `Using` blocks for disposable resources

### MVVM Pattern
- Follow MVVM strictly - no code-behind logic except UI-specific behavior
- Use CommunityToolkit.Mvvm for commands and observable properties
- Keep views in XAML, logic in ViewModels
- Use dependency injection for services

### WPF/XAML Conventions
- Prefer XAML styling over code-behind for appearance
- Use StaticResource for theme colors and styles
- Use data binding over direct property manipulation

### Git Workflow
- Create feature branches for new work
- Use conventional commit messages
- Create GitHub issues for non-trivial changes
- Reference issues in commits with `Closes #XX`

## Planned Features

See `PROJECT_NOTES.md` for full feature list and development phases.

### Phase 1 (Complete)
- [x] Basic WPF application shell
- [x] Tab control with new/close
- [x] Terminal-like display
- [x] Dark theme
- [x] Menu structure (placeholder)

### Phase 2 (Current)
- [ ] Icon-based toolbar (favorites, settings, help)
- [ ] Favorites system (folder, command, or both)
- [ ] Settings/appearance customization
- [ ] Claude CLI integration improvements

### Future
- Session persistence
- Project templates
- Claude API integration
- Keyboard shortcuts
- Search functionality

## Configuration

User settings will be stored in: `%APPDATA%/ClaudeConsole/`
- `settings.json` - Application preferences
- `favorites.json` - Favorite projects
- `sessions/` - Saved session history

## Favorites Feature (Planned)

Favorites can define:
- **Name**: Display name for the favorite
- **Folder**: Working directory path (optional)
- **Command**: Command to run after navigation (optional)
- **Icon**: Custom icon (optional)
- **Color**: Accent color for identification (optional)

Example structure:
```json
{
  "favorites": [
    {
      "name": "Helix",
      "folder": "C:/Users/.../source/repos/Helix",
      "command": null,
      "icon": null
    },
    {
      "name": "Run Tests",
      "folder": "C:/Projects/MyApp",
      "command": "dotnet test",
      "icon": "test"
    }
  ]
}
```

## GitHub Repository

- **Organization**: mkswyfft
- **Repository**: ClaudeConsole
- **URL**: https://github.com/mkswyfft/ClaudeConsole

## Common Issues

### VB.NET WPF Gotchas
- `For Each` loops use `Next`, not `End For`
- `InputBox` is a reserved function name - don't use for control names
- Partial classes for UserControls must match XAML x:Class exactly
- `Handles Me.Loaded` doesn't work well in partial classes - use XAML `Loaded=` instead

### Claude CLI Integration
- Claude CLI may need PTY emulation for full interactive mode
- Consider using `--print` mode with streaming for better control
- Process redirection works but buffering may affect output timing
