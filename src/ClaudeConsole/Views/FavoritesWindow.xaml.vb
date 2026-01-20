Option Strict On
Option Explicit On

Imports ClaudeConsole.Models
Imports ClaudeConsole.Services

Partial Class FavoritesWindow

    Private ReadOnly _favoritesService As FavoritesService
    Private _selectedFavorite As Favorite

    ''' <summary>
    ''' Gets the favorite that was selected to open, if any.
    ''' </summary>
    Public Property SelectedFavoriteToOpen As Favorite

    Public Sub New(favoritesService As FavoritesService)
        InitializeComponent()
        _favoritesService = favoritesService
        RefreshList()
    End Sub

    Private Sub RefreshList()
        FavoritesList.ItemsSource = Nothing
        FavoritesList.ItemsSource = _favoritesService.Favorites.ToList()
        UpdateButtonStates()
    End Sub

    Private Sub UpdateButtonStates()
        Dim hasSelection = FavoritesList.SelectedItem IsNot Nothing
        EditButton.IsEnabled = hasSelection
        DeleteButton.IsEnabled = hasSelection
        OpenButton.IsEnabled = hasSelection
    End Sub

    Private Sub FavoritesList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        _selectedFavorite = TryCast(FavoritesList.SelectedItem, Favorite)
        UpdateButtonStates()
    End Sub

    Private Sub FavoritesList_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        If FavoritesList.SelectedItem IsNot Nothing Then
            OpenSelectedFavorite()
        End If
    End Sub

    Private Sub AddButton_Click(sender As Object, e As RoutedEventArgs)
        Dim editWindow As New FavoriteEditWindow()
        editWindow.Owner = Me
        If editWindow.ShowDialog() = True Then
            _favoritesService.Add(editWindow.Favorite)
            RefreshList()
        End If
    End Sub

    Private Sub EditButton_Click(sender As Object, e As RoutedEventArgs)
        If _selectedFavorite Is Nothing Then Return

        Dim editWindow As New FavoriteEditWindow(_selectedFavorite)
        editWindow.Owner = Me
        If editWindow.ShowDialog() = True Then
            _favoritesService.Update(editWindow.Favorite)
            RefreshList()
        End If
    End Sub

    Private Sub DeleteButton_Click(sender As Object, e As RoutedEventArgs)
        If _selectedFavorite Is Nothing Then Return

        Dim result = MessageBox.Show(
            $"Are you sure you want to delete '{_selectedFavorite.Name}'?",
            "Delete Favorite",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question)

        If result = MessageBoxResult.Yes Then
            _favoritesService.Remove(_selectedFavorite)
            RefreshList()
        End If
    End Sub

    Private Sub OpenButton_Click(sender As Object, e As RoutedEventArgs)
        OpenSelectedFavorite()
    End Sub

    Private Sub OpenSelectedFavorite()
        If _selectedFavorite Is Nothing Then Return
        SelectedFavoriteToOpen = _selectedFavorite
        DialogResult = True
        Close()
    End Sub

    Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
        Close()
    End Sub
End Class
