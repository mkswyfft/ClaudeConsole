Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text.Json

Namespace Services
    ''' <summary>
    ''' Service for managing application settings persistence.
    ''' </summary>
    Public Class SettingsService
        Private Shared ReadOnly _jsonOptions As New JsonSerializerOptions With {
            .WriteIndented = True,
            .PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }

        Private ReadOnly _settingsPath As String
        Private _settings As AppSettings

        ''' <summary>
        ''' Gets the current settings.
        ''' </summary>
        Public ReadOnly Property Settings As AppSettings
            Get
                Return _settings
            End Get
        End Property

        Public Sub New()
            Dim appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClaudeConsole")
            Directory.CreateDirectory(appDataPath)
            _settingsPath = Path.Combine(appDataPath, "settings.json")
            _settings = New AppSettings()
        End Sub

        ''' <summary>
        ''' Loads settings from disk.
        ''' </summary>
        Public Sub Load()
            Try
                If File.Exists(_settingsPath) Then
                    Dim json = File.ReadAllText(_settingsPath)
                    Dim loaded = JsonSerializer.Deserialize(Of AppSettings)(json, _jsonOptions)
                    If loaded IsNot Nothing Then
                        _settings = loaded
                    End If
                End If
            Catch ex As Exception
                ' If load fails, use defaults
                _settings = New AppSettings()
            End Try
        End Sub

        ''' <summary>
        ''' Saves settings to disk.
        ''' </summary>
        Public Sub Save()
            Try
                Dim json = JsonSerializer.Serialize(_settings, _jsonOptions)
                File.WriteAllText(_settingsPath, json)
            Catch ex As Exception
                ' Silently fail for now
            End Try
        End Sub
    End Class

    ''' <summary>
    ''' Application settings model.
    ''' </summary>
    Public Class AppSettings
        ''' <summary>
        ''' Whether to restore sessions on startup.
        ''' </summary>
        Public Property RestoreSessionsOnStartup As Boolean = False

        ''' <summary>
        ''' Whether to auto-save sessions on close.
        ''' </summary>
        Public Property AutoSaveSessionsOnClose As Boolean = True

        ''' <summary>
        ''' Terminal font size.
        ''' </summary>
        Public Property TerminalFontSize As Integer = 14

        ''' <summary>
        ''' Custom Claude CLI path (empty for default).
        ''' </summary>
        Public Property ClaudeCliPath As String = String.Empty
    End Class
End Namespace
