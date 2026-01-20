Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text.Json
Imports ClaudeConsole.Models

Namespace Services
    ''' <summary>
    ''' Service for managing favorites persistence.
    ''' </summary>
    Public Class FavoritesService
        Private Shared ReadOnly _jsonOptions As New JsonSerializerOptions With {
            .WriteIndented = True,
            .PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }

        Private ReadOnly _favoritesPath As String
        Private _favorites As List(Of Favorite)

        ''' <summary>
        ''' Gets the list of favorites.
        ''' </summary>
        Public ReadOnly Property Favorites As IReadOnlyList(Of Favorite)
            Get
                Return _favorites.AsReadOnly()
            End Get
        End Property

        Public Sub New()
            Dim appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClaudeConsole")
            Directory.CreateDirectory(appDataPath)
            _favoritesPath = Path.Combine(appDataPath, "favorites.json")
            _favorites = New List(Of Favorite)()
        End Sub

        ''' <summary>
        ''' Loads favorites from disk.
        ''' </summary>
        Public Sub Load()
            Try
                If File.Exists(_favoritesPath) Then
                    Dim json = File.ReadAllText(_favoritesPath)
                    Dim loaded = JsonSerializer.Deserialize(Of List(Of Favorite))(json, _jsonOptions)
                    If loaded IsNot Nothing Then
                        _favorites = loaded
                    End If
                End If
            Catch ex As Exception
                ' If load fails, start with empty list
                _favorites = New List(Of Favorite)()
            End Try
        End Sub

        ''' <summary>
        ''' Saves favorites to disk.
        ''' </summary>
        Public Sub Save()
            Try
                Dim json = JsonSerializer.Serialize(_favorites, _jsonOptions)
                File.WriteAllText(_favoritesPath, json)
            Catch ex As Exception
                ' Silently fail for now
            End Try
        End Sub

        ''' <summary>
        ''' Adds a new favorite.
        ''' </summary>
        Public Sub Add(favorite As Favorite)
            If favorite Is Nothing Then Return
            favorite.SortOrder = If(_favorites.Count > 0, _favorites.Max(Function(f) f.SortOrder) + 1, 0)
            _favorites.Add(favorite)
            Save()
        End Sub

        ''' <summary>
        ''' Updates an existing favorite.
        ''' </summary>
        Public Sub Update(favorite As Favorite)
            If favorite Is Nothing Then Return
            Dim index = _favorites.FindIndex(Function(f) f.Id = favorite.Id)
            If index >= 0 Then
                _favorites(index) = favorite
                Save()
            End If
        End Sub

        ''' <summary>
        ''' Removes a favorite.
        ''' </summary>
        Public Sub Remove(favorite As Favorite)
            If favorite Is Nothing Then Return
            _favorites.RemoveAll(Function(f) f.Id = favorite.Id)
            Save()
        End Sub

        ''' <summary>
        ''' Removes a favorite by ID.
        ''' </summary>
        Public Sub Remove(id As Guid)
            _favorites.RemoveAll(Function(f) f.Id = id)
            Save()
        End Sub

        ''' <summary>
        ''' Moves a favorite up in the list.
        ''' </summary>
        Public Sub MoveUp(favorite As Favorite)
            Dim index = _favorites.IndexOf(favorite)
            If index > 0 Then
                _favorites.RemoveAt(index)
                _favorites.Insert(index - 1, favorite)
                UpdateSortOrders()
                Save()
            End If
        End Sub

        ''' <summary>
        ''' Moves a favorite down in the list.
        ''' </summary>
        Public Sub MoveDown(favorite As Favorite)
            Dim index = _favorites.IndexOf(favorite)
            If index >= 0 AndAlso index < _favorites.Count - 1 Then
                _favorites.RemoveAt(index)
                _favorites.Insert(index + 1, favorite)
                UpdateSortOrders()
                Save()
            End If
        End Sub

        Private Sub UpdateSortOrders()
            For i = 0 To _favorites.Count - 1
                _favorites(i).SortOrder = i
            Next
        End Sub

        ''' <summary>
        ''' Gets the path to the favorites file.
        ''' </summary>
        Public ReadOnly Property FilePath As String
            Get
                Return _favoritesPath
            End Get
        End Property
    End Class
End Namespace
