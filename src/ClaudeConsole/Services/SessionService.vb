Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text.Json
Imports ClaudeConsole.Models

Namespace Services
    ''' <summary>
    ''' Service for managing session persistence.
    ''' </summary>
    Public Class SessionService
        Private Shared ReadOnly _jsonOptions As New JsonSerializerOptions With {
            .WriteIndented = True,
            .PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }

        Private ReadOnly _sessionsPath As String
        Private _sessions As List(Of SavedSession)

        ''' <summary>
        ''' Gets the list of saved sessions.
        ''' </summary>
        Public ReadOnly Property Sessions As IReadOnlyList(Of SavedSession)
            Get
                Return _sessions.AsReadOnly()
            End Get
        End Property

        Public Sub New()
            Dim appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClaudeConsole")
            Directory.CreateDirectory(appDataPath)
            _sessionsPath = Path.Combine(appDataPath, "sessions.json")
            _sessions = New List(Of SavedSession)()
        End Sub

        ''' <summary>
        ''' Loads saved sessions from disk.
        ''' </summary>
        Public Sub Load()
            Try
                If File.Exists(_sessionsPath) Then
                    Dim json = File.ReadAllText(_sessionsPath)
                    Dim loaded = JsonSerializer.Deserialize(Of List(Of SavedSession))(json, _jsonOptions)
                    If loaded IsNot Nothing Then
                        _sessions = loaded
                    End If
                End If
            Catch ex As Exception
                ' If load fails, start with empty list
                _sessions = New List(Of SavedSession)()
            End Try
        End Sub

        ''' <summary>
        ''' Saves all sessions to disk.
        ''' </summary>
        Public Sub Save()
            Try
                Dim json = JsonSerializer.Serialize(_sessions, _jsonOptions)
                File.WriteAllText(_sessionsPath, json)
            Catch ex As Exception
                ' Silently fail for now
            End Try
        End Sub

        ''' <summary>
        ''' Adds or updates a session.
        ''' </summary>
        Public Sub SaveSession(session As SavedSession)
            If session Is Nothing Then Return

            session.LastAccessedAt = DateTime.Now

            Dim index = _sessions.FindIndex(Function(s) s.Id = session.Id)
            If index >= 0 Then
                _sessions(index) = session
            Else
                _sessions.Add(session)
            End If
            Save()
        End Sub

        ''' <summary>
        ''' Saves multiple sessions at once, replacing all existing sessions.
        ''' </summary>
        Public Sub SaveAll(sessions As IEnumerable(Of SavedSession))
            If sessions Is Nothing Then Return
            _sessions = sessions.ToList()
            Save()
        End Sub

        ''' <summary>
        ''' Gets a session by ID.
        ''' </summary>
        Public Function GetSession(id As Guid) As SavedSession
            Return _sessions.FirstOrDefault(Function(s) s.Id = id)
        End Function

        ''' <summary>
        ''' Removes a session by ID.
        ''' </summary>
        Public Sub Delete(id As Guid)
            _sessions.RemoveAll(Function(s) s.Id = id)
            Save()
        End Sub

        ''' <summary>
        ''' Removes a session.
        ''' </summary>
        Public Sub Delete(session As SavedSession)
            If session Is Nothing Then Return
            _sessions.RemoveAll(Function(s) s.Id = session.Id)
            Save()
        End Sub

        ''' <summary>
        ''' Clears all saved sessions.
        ''' </summary>
        Public Sub Clear()
            _sessions.Clear()
            Save()
        End Sub

        ''' <summary>
        ''' Gets sessions ordered by TabIndex for restore.
        ''' </summary>
        Public Function GetSessionsForRestore() As IReadOnlyList(Of SavedSession)
            Return _sessions.OrderBy(Function(s) s.TabIndex).ToList().AsReadOnly()
        End Function

        ''' <summary>
        ''' Gets the path to the sessions file.
        ''' </summary>
        Public ReadOnly Property FilePath As String
            Get
                Return _sessionsPath
            End Get
        End Property

        ''' <summary>
        ''' Gets the count of saved sessions.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                Return _sessions.Count
            End Get
        End Property
    End Class
End Namespace
