Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports System.Windows.Media.Animation
Imports System.Windows.Controls.Primitives
Imports ClaudeConsole.Models
Imports ClaudeConsole.Services
Imports ClaudeConsole.ViewModels

Class MainWindow
    Private ReadOnly _viewModel As MainViewModel
    Private ReadOnly _favoritesService As FavoritesService
    Private _isFavoritesPanelOpen As Boolean = False
    Private _isSettingsPanelOpen As Boolean = False

    ' Windows API for dark title bar
    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function

    Private Const DWMWA_USE_IMMERSIVE_DARK_MODE As Integer = 20

    Public Sub New()
        InitializeComponent()

        _viewModel = New MainViewModel(Dispatcher)
        DataContext = _viewModel

        ' Initialize favorites service
        _favoritesService = New FavoritesService()
        _favoritesService.Load()

        ' Set custom placement callbacks for popups
        FavoritesPopup.CustomPopupPlacementCallback = AddressOf PlaceRightSidePopup
        SettingsPopup.CustomPopupPlacementCallback = AddressOf PlaceRightSidePopup

        ' Apply dark title bar when window is loaded
        AddHandler Loaded, AddressOf OnWindowLoaded

        ' Reposition popup when window moves or resizes
        AddHandler LocationChanged, AddressOf OnWindowPositionChanged
        AddHandler SizeChanged, AddressOf OnWindowPositionChanged
    End Sub

    Private Function PlaceRightSidePopup(popupSize As Size, targetSize As Size, offset As Point) As CustomPopupPlacement()
        ' Position popup at the right edge of the target (ContentArea), overlapping it
        ' targetSize is the size of ContentArea
        ' We want the popup's right edge to align with ContentArea's right edge
        Dim x = targetSize.Width - popupSize.Width
        Dim y = 0.0

        Return New CustomPopupPlacement() {
            New CustomPopupPlacement(New Point(x, y), PopupPrimaryAxis.Horizontal)
        }
    End Function

    Private Sub OnWindowPositionChanged(sender As Object, e As EventArgs)
        If _isFavoritesPanelOpen Then
            PositionFavoritesPopup()
        End If
        If _isSettingsPanelOpen Then
            PositionSettingsPopup()
        End If
    End Sub

    Private Sub OnWindowLoaded(sender As Object, e As RoutedEventArgs)
        ' Enable dark mode for title bar (Windows 10 1809+ / Windows 11)
        Try
            Dim hwnd As IntPtr = New WindowInteropHelper(Me).Handle
            Dim darkMode As Integer = 1
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, darkMode, Marshal.SizeOf(GetType(Integer)))
        Catch
            ' Ignore if not supported on this Windows version
        End Try

        ' Set version text from assembly info
        Try
            Dim version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
            VersionText.Text = $"ClaudeConsole v{version.Major}.{version.Minor}.{version.Build}"
        Catch
            VersionText.Text = "ClaudeConsole"
        End Try
    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        ' Clean up all sessions before closing
        _viewModel.Shutdown()
    End Sub

    Private Sub FavoritesButton_Click(sender As Object, e As RoutedEventArgs)
        ' Close settings panel if open
        If _isSettingsPanelOpen Then
            ToggleSettingsPanel()
        End If
        ToggleFavoritesPanel()
    End Sub

    Private Sub ToggleFavoritesPanel()
        _isFavoritesPanelOpen = Not _isFavoritesPanelOpen

        ' Update star icon color (gold when active, dark grey when inactive)
        FavoritesIcon.Fill = If(_isFavoritesPanelOpen,
            New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFD700"), Color)),
            New SolidColorBrush(CType(ColorConverter.ConvertFromString("#5A5A5A"), Color)))

        If _isFavoritesPanelOpen Then
            ' Position the popup at the right edge of the content area
            PositionFavoritesPopup()
            FavoritesPopup.IsOpen = True
            RefreshFavoritesList()

            ' Animate slide in from right (280 = hidden right, 0 = visible)
            Dim slideIn As New DoubleAnimation() With {
                .From = 280,
                .To = 0,
                .Duration = TimeSpan.FromMilliseconds(200),
                .EasingFunction = New QuadraticEase() With {.EasingMode = EasingMode.EaseOut}
            }
            FavoritesPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideIn)
        Else
            ' Animate slide out to right (0 = visible, 280 = hidden right)
            Dim slideOut As New DoubleAnimation() With {
                .From = 0,
                .To = 280,
                .Duration = TimeSpan.FromMilliseconds(200),
                .EasingFunction = New QuadraticEase() With {.EasingMode = EasingMode.EaseIn}
            }
            AddHandler slideOut.Completed, Sub(s, args)
                                               FavoritesPopup.IsOpen = False
                                           End Sub
            FavoritesPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideOut)
        End If
    End Sub

    Private Sub PositionFavoritesPopup()
        ' Set the panel height to match content area
        ' Position is handled by Popup's PlacementTarget and Placement properties
        FavoritesPanel.Height = ContentArea.ActualHeight
    End Sub

    Private Sub RefreshFavoritesList()
        FavoritesList.ItemsSource = Nothing
        FavoritesList.ItemsSource = _favoritesService.Favorites.ToList()

        ' Show empty state if no favorites
        EmptyFavoritesText.Visibility = If(_favoritesService.Favorites.Count = 0,
            Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Sub FavoritesList_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        Dim favorite = TryCast(FavoritesList.SelectedItem, Favorite)
        If favorite IsNot Nothing Then
            OpenFavorite(favorite)
        End If
    End Sub

    Private Sub OpenFavorite(favorite As Favorite)
        _viewModel.CreateNewTabWithFavorite(favorite.Folder, favorite.Name)

        ' Optionally close the panel after opening
        ' ToggleFavoritesPanel()
    End Sub

    Private Sub AddFavorite_Click(sender As Object, e As RoutedEventArgs)
        Dim editWindow As New FavoriteEditWindow()
        editWindow.Owner = Me
        If editWindow.ShowDialog() = True Then
            _favoritesService.Add(editWindow.Favorite)
            RefreshFavoritesList()
        End If
    End Sub

    Private Sub DeleteFavorite_Click(sender As Object, e As RoutedEventArgs)
        Dim button = TryCast(sender, Button)
        Dim favorite = TryCast(button?.Tag, Favorite)
        If favorite Is Nothing Then Return

        Dim result = MessageBox.Show(
            $"Delete '{favorite.Name}'?",
            "Delete Favorite",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question)

        If result = MessageBoxResult.Yes Then
            _favoritesService.Remove(favorite)
            RefreshFavoritesList()
        End If
    End Sub

    Private Sub SettingsButton_Click(sender As Object, e As RoutedEventArgs)
        ' Close favorites panel if open
        If _isFavoritesPanelOpen Then
            ToggleFavoritesPanel()
        End If
        ToggleSettingsPanel()
    End Sub

    Private Sub ToggleSettingsPanel()
        _isSettingsPanelOpen = Not _isSettingsPanelOpen

        ' Update settings icon color (silver when active, dark grey when inactive)
        SettingsIcon.Fill = If(_isSettingsPanelOpen,
            New SolidColorBrush(CType(ColorConverter.ConvertFromString("#C0C0C0"), Color)),
            New SolidColorBrush(CType(ColorConverter.ConvertFromString("#5A5A5A"), Color)))

        If _isSettingsPanelOpen Then
            PositionSettingsPopup()
            SettingsPopup.IsOpen = True

            ' Animate slide in from right
            Dim slideIn As New DoubleAnimation() With {
                .From = 320,
                .To = 0,
                .Duration = TimeSpan.FromMilliseconds(200),
                .EasingFunction = New QuadraticEase() With {.EasingMode = EasingMode.EaseOut}
            }
            SettingsPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideIn)
        Else
            ' Animate slide out to right
            Dim slideOut As New DoubleAnimation() With {
                .From = 0,
                .To = 320,
                .Duration = TimeSpan.FromMilliseconds(200),
                .EasingFunction = New QuadraticEase() With {.EasingMode = EasingMode.EaseIn}
            }
            AddHandler slideOut.Completed, Sub(s, args)
                                               SettingsPopup.IsOpen = False
                                           End Sub
            SettingsPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideOut)
        End If
    End Sub

    Private Sub PositionSettingsPopup()
        ' Set the panel height to match content area
        SettingsPanel.Height = ContentArea.ActualHeight
    End Sub

    Private Sub ManageSessions_Click(sender As Object, e As RoutedEventArgs)
        MessageBox.Show("Session management will be available in a future update." & vbCrLf & vbCrLf &
                        "This feature will allow you to:" & vbCrLf &
                        "• View saved sessions" & vbCrLf &
                        "• Restore previous sessions" & vbCrLf &
                        "• Delete old sessions", "Manage Sessions", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub

    Private Sub HelpButton_Click(sender As Object, e As RoutedEventArgs)
        MessageBox.Show("ClaudeConsole" & vbCrLf & vbCrLf &
                        "A terminal-like application for Claude AI" & vbCrLf &
                        "Version 1.0.0" & vbCrLf & vbCrLf &
                        "Keyboard Shortcuts:" & vbCrLf &
                        "• Ctrl+T - New Tab" & vbCrLf &
                        "• Ctrl+W - Close Tab", "About ClaudeConsole", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub
End Class
