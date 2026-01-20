# ClaudeConsole - Project Notes

## Overview
ClaudeConsole is a terminal-like Windows desktop application for interacting with Claude AI. It provides project management capabilities, allowing users to quickly access, switch between, and organize their Claude projects with a tabbed interface.

## Tech Stack
- **Framework**: WPF (.NET 8)
- **Language**: VB.NET
- **IDE**: Visual Studio 2022 Community
- **Claude Integration**: Claude CLI (primary), Claude API (future)
- **Platform**: Windows

## Core Features

### 1. Project Management
- **Project Definitions**: Define projects with name, working directory, and context files
- **Quick Access**: Fast switching between defined projects
- **Favorites System**: Mark projects as favorites for prominent placement
- **Recent Projects**: Track and display recently used projects
- **Project Groups/Folders**: Organize projects into categories
- **Project Templates**: Pre-configured setups for common use cases

### 2. Tab & Session Management
- **Multiple Tabs**: Open multiple Claude sessions simultaneously
- **Easy Tab Creation**: Quick way to open new tabs (keyboard shortcut, button)
- **Tab Naming/Renaming**: Custom names for easy identification
- **Tab Status Indicators**: Show active, waiting for response, has new content
- **Drag-and-Drop Reordering**: Rearrange tabs as needed
- **Split View**: View multiple sessions side-by-side

### 3. Session & History
- **Session Persistence**: Save and resume conversations per project
- **Auto-Save**: Automatically save session state with crash recovery
- **Conversation Search**: Search through past discussions
- **Export Functionality**: Export to markdown or other formats
- **Session History Browser**: View and restore previous sessions

### 4. User Interface
- **Terminal Aesthetic**: Clean, terminal-like appearance
- **Light/Dark Themes**: Support both with easy toggle
- **Customizable Colors/Fonts**: Personalize the terminal look
- **Command Palette**: Ctrl+P style quick access to all features
- **Keyboard Shortcuts**: All common actions accessible via keyboard

### 5. Configuration & Settings
- **Favorites Definition**: Dedicated interface to manage favorite projects
- **Global Settings**: App-wide preferences
- **Per-Project Settings**: Project-specific configurations
- **API Key Management**: Secure storage for Claude API keys (future)
- **Model Selection**: Choose Claude models per project/session (future)
- **Context File Management**: Attach files to conversation context

### 6. File System Integration
- **Working Directory**: Each project has its own working directory
- **Context Files**: Support for CLAUDE.md and other context files
- **File Browser**: Browse and attach files to sessions
- **Project-Aware Navigation**: Navigate file system relative to project root

## Architecture

### Project Structure
```
ClaudeConsole/
├── ClaudeConsole.sln
├── src/
│   └── ClaudeConsole/
│       ├── App.xaml
│       ├── MainWindow.xaml
│       ├── Models/
│       │   ├── Project.vb
│       │   ├── Session.vb
│       │   ├── Tab.vb
│       │   └── Settings.vb
│       ├── ViewModels/
│       │   ├── MainViewModel.vb
│       │   ├── TabViewModel.vb
│       │   ├── ProjectViewModel.vb
│       │   └── SettingsViewModel.vb
│       ├── Views/
│       │   ├── MainWindow.xaml
│       │   ├── TabView.xaml
│       │   ├── ProjectsPanel.xaml
│       │   ├── SettingsWindow.xaml
│       │   └── FavoritesManager.xaml
│       ├── Services/
│       │   ├── ClaudeCliService.vb
│       │   ├── ProjectService.vb
│       │   ├── SessionService.vb
│       │   ├── SettingsService.vb
│       │   └── FileSystemService.vb
│       ├── Controls/
│       │   ├── TerminalControl.xaml
│       │   └── TabControl.xaml
│       └── Utilities/
│           ├── KeyboardShortcuts.vb
│           └── ThemeManager.vb
├── tests/
│   └── ClaudeConsole.Tests/
├── docs/
└── assets/
    └── icons/
```

### Design Patterns
- **MVVM**: Model-View-ViewModel for clean separation
- **Dependency Injection**: For services and testability
- **Command Pattern**: For keyboard shortcuts and actions
- **Observer Pattern**: For UI updates and notifications

### Data Storage
- **Settings**: JSON file in AppData
- **Projects**: JSON file defining all projects
- **Sessions**: Individual JSON files per session
- **Favorites**: Stored within projects JSON with favorite flag

## Claude CLI Integration

### Why Claude CLI over API
1. Built-in file system access
2. Understands project context automatically
3. Can execute commands
4. Handles all tooling (code reading, editing, etc.)
5. No need to rebuild existing functionality

### Integration Approach
- Launch Claude CLI as a subprocess
- Capture stdin/stdout for terminal display
- Parse output for status indicators
- Support CLI flags for project context (--cwd, etc.)

## Keyboard Shortcuts (Planned)
| Shortcut | Action |
|----------|--------|
| Ctrl+T | New Tab |
| Ctrl+W | Close Tab |
| Ctrl+Tab | Next Tab |
| Ctrl+Shift+Tab | Previous Tab |
| Ctrl+1-9 | Switch to Tab N |
| Ctrl+P | Command Palette |
| Ctrl+, | Settings |
| Ctrl+S | Save Session |
| Ctrl+F | Search in Session |
| Ctrl+Shift+F | Search All Sessions |
| F11 | Toggle Fullscreen |

## Future Enhancements
- Claude API integration for simple queries
- Plugin system for extensions
- Cloud sync for settings/favorites
- Team sharing features
- Custom prompt templates
- Session branching (fork conversation)
- Notifications for long-running responses

## Development Phases

### Phase 1: Foundation
- [ ] Basic WPF application shell
- [ ] Tab control implementation
- [ ] Claude CLI process management
- [ ] Basic terminal display

### Phase 2: Project Management
- [ ] Project definition and storage
- [ ] Favorites system
- [ ] Recent projects
- [ ] Project switching

### Phase 3: Session Management
- [ ] Session persistence
- [ ] Auto-save
- [ ] Session history

### Phase 4: Polish
- [ ] Themes
- [ ] Keyboard shortcuts
- [ ] Command palette
- [ ] Search functionality

### Phase 5: Advanced Features
- [ ] Split view
- [ ] Export functionality
- [ ] Project templates
- [ ] API integration
