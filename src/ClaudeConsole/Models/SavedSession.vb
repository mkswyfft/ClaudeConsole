Option Strict On
Option Explicit On

Imports System.Text.Json.Serialization

Namespace Models
    ''' <summary>
    ''' Represents a saved session for persistence and restore.
    ''' </summary>
    Public Class SavedSession
        ''' <summary>
        ''' Unique identifier for the session.
        ''' </summary>
        Public Property Id As Guid = Guid.NewGuid()

        ''' <summary>
        ''' Display name for the session.
        ''' </summary>
        Public Property Title As String = String.Empty

        ''' <summary>
        ''' Working directory path where Claude CLI runs.
        ''' </summary>
        Public Property WorkingDirectory As String = String.Empty

        ''' <summary>
        ''' When the session was originally created.
        ''' </summary>
        Public Property CreatedAt As DateTime = DateTime.Now

        ''' <summary>
        ''' When the session was last accessed/saved.
        ''' </summary>
        Public Property LastAccessedAt As DateTime = DateTime.Now

        ''' <summary>
        ''' Position in tab order for restore.
        ''' </summary>
        Public Property TabIndex As Integer = 0

        ''' <summary>
        ''' Gets a display-friendly description showing the working directory.
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property Description As String
            Get
                If Not String.IsNullOrWhiteSpace(WorkingDirectory) Then
                    Return WorkingDirectory
                Else
                    Return "(No working directory)"
                End If
            End Get
        End Property

        ''' <summary>
        ''' Gets a formatted string of when the session was last accessed.
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property LastAccessedDisplay As String
            Get
                Dim diff = DateTime.Now - LastAccessedAt
                If diff.TotalMinutes < 1 Then
                    Return "Just now"
                ElseIf diff.TotalHours < 1 Then
                    Return $"{CInt(diff.TotalMinutes)} min ago"
                ElseIf diff.TotalDays < 1 Then
                    Return $"{CInt(diff.TotalHours)} hours ago"
                ElseIf diff.TotalDays < 7 Then
                    Return $"{CInt(diff.TotalDays)} days ago"
                Else
                    Return LastAccessedAt.ToString("MMM d, yyyy")
                End If
            End Get
        End Property
    End Class
End Namespace
