Option Strict On
Option Explicit On

Imports System.Diagnostics

Namespace Models
    ''' <summary>
    ''' Represents a single Claude CLI session within a tab.
    ''' </summary>
    Public Class TabSession
        ''' <summary>
        ''' Unique identifier for this session.
        ''' </summary>
        Public Property Id As Guid = Guid.NewGuid()

        ''' <summary>
        ''' Display title for the tab.
        ''' </summary>
        Public Property Title As String = "New Session"

        ''' <summary>
        ''' Working directory for the Claude CLI process.
        ''' </summary>
        Public Property WorkingDirectory As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

        ''' <summary>
        ''' The Claude CLI process (if running).
        ''' </summary>
        Public Property Process As Process

        ''' <summary>
        ''' Timestamp when the session was created.
        ''' </summary>
        Public Property CreatedAt As DateTime = DateTime.Now

        ''' <summary>
        ''' Indicates if the CLI process is currently running.
        ''' </summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return Process IsNot Nothing AndAlso Not Process.HasExited
            End Get
        End Property
    End Class
End Namespace
