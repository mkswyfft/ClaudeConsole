Option Strict On
Option Explicit On

Imports System.Text.Json.Serialization

Namespace Models
    ''' <summary>
    ''' Represents a favorite project or command.
    ''' </summary>
    Public Class Favorite
        ''' <summary>
        ''' Unique identifier for the favorite.
        ''' </summary>
        Public Property Id As Guid = Guid.NewGuid()

        ''' <summary>
        ''' Display name for the favorite.
        ''' </summary>
        Public Property Name As String = String.Empty

        ''' <summary>
        ''' Working directory path (optional).
        ''' </summary>
        Public Property Folder As String = String.Empty

        ''' <summary>
        ''' Command to run after navigation (optional).
        ''' </summary>
        Public Property Command As String = String.Empty

        ''' <summary>
        ''' Icon identifier (optional).
        ''' </summary>
        Public Property Icon As String = String.Empty

        ''' <summary>
        ''' Accent color hex code (optional).
        ''' </summary>
        Public Property Color As String = String.Empty

        ''' <summary>
        ''' Order index for sorting.
        ''' </summary>
        Public Property SortOrder As Integer = 0

        ''' <summary>
        ''' Gets whether this favorite has a folder path.
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property HasFolder As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(Folder)
            End Get
        End Property

        ''' <summary>
        ''' Gets whether this favorite has a command.
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property HasCommand As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(Command)
            End Get
        End Property

        ''' <summary>
        ''' Gets a display-friendly description of the favorite.
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property Description As String
            Get
                If HasFolder AndAlso HasCommand Then
                    Return $"{Folder} → {Command}"
                ElseIf HasFolder Then
                    Return Folder
                ElseIf HasCommand Then
                    Return Command
                Else
                    Return "(No folder or command)"
                End If
            End Get
        End Property
    End Class
End Namespace
