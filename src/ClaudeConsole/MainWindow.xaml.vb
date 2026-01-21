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
    Private ReadOnly _sessionService As SessionService
    Private ReadOnly _settingsService As SettingsService
    Private _isFavoritesPanelOpen As Boolean = False
    Private _isSettingsPanelOpen As Boolean = False

    ' Windows API for dark title bar
    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function

    Private Const DWMWA_USE_IMMERSIVE_DARK_MODE As Integer = 20

    Public Sub New()
        InitializeComponent()

        ' Initialize settings service first
        _settingsService = New SettingsService()
        _settingsService.Load()

        ' Initialize session service
        _sessionService = New SessionService()
        _sessionService.Load()

        ' Create view model with restore preference from settings
        _viewModel = New MainViewModel(Dispatcher, _sessionService, _settingsService.Settings.RestoreSessionsOnStartup)
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

        ' Close popups when window is minimized or deactivated
        AddHandler StateChanged, AddressOf OnWindowStateChanged
        AddHandler Deactivated, AddressOf OnWindowDeactivated
    End Sub

    Private Sub OnWindowStateChanged(sender As Object, e As EventArgs)
        ' Close panels when window is minimized
        If WindowState = WindowState.Minimized Then
            CloseAllPanels()
        End If
    End Sub

    Private Sub OnWindowDeactivated(sender As Object, e As EventArgs)
        ' Close panels when window loses focus (e.g., when a dialog opens from outside)
        ' Note: We don't close when our own dialogs open because we handle that explicitly
    End Sub

    Private Sub CloseAllPanels()
        If _isFavoritesPanelOpen Then
            _isFavoritesPanelOpen = False
            FavoritesPopup.IsOpen = False
            FavoritesIcon.Fill = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#5A5A5A"), Color))
        End If
        If _isSettingsPanelOpen Then
            _isSettingsPanelOpen = False
            SettingsPopup.IsOpen = False
            SettingsIcon.Fill = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#5A5A5A"), Color))
        End If
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

        ' Initialize settings UI from saved settings
        InitializeSettingsUI()
    End Sub

    Private Sub InitializeSettingsUI()
        ' Set checkbox states from settings
        RestoreSessionsCheckBox.IsChecked = _settingsService.Settings.RestoreSessionsOnStartup
        AutoSaveSessionsCheckBox.IsChecked = _settingsService.Settings.AutoSaveSessionsOnClose

        ' Set font size dropdown
        For Each item As ComboBoxItem In FontSizeComboBox.Items
            If CStr(item.Tag) = _settingsService.Settings.TerminalFontSize.ToString() Then
                FontSizeComboBox.SelectedItem = item
                Exit For
            End If
        Next

        ' Set Claude CLI path
        ClaudePathTextBox.Text = _settingsService.Settings.ClaudeCliPath

        ' Wire up checkbox change events
        AddHandler RestoreSessionsCheckBox.Checked, AddressOf OnSettingsCheckBoxChanged
        AddHandler RestoreSessionsCheckBox.Unchecked, AddressOf OnSettingsCheckBoxChanged
        AddHandler AutoSaveSessionsCheckBox.Checked, AddressOf OnSettingsCheckBoxChanged
        AddHandler AutoSaveSessionsCheckBox.Unchecked, AddressOf OnSettingsCheckBoxChanged
    End Sub

    Private Sub OnSettingsCheckBoxChanged(sender As Object, e As RoutedEventArgs)
        ' Update settings from UI
        _settingsService.Settings.RestoreSessionsOnStartup = RestoreSessionsCheckBox.IsChecked.GetValueOrDefault(False)
        _settingsService.Settings.AutoSaveSessionsOnClose = AutoSaveSessionsCheckBox.IsChecked.GetValueOrDefault(True)
        _settingsService.Save()
    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        ' Save sessions if auto-save is enabled
        If AutoSaveSessionsCheckBox.IsChecked = True Then
            SaveCurrentSessions()
        End If

        ' Clean up all sessions before closing
        _viewModel.Shutdown()
    End Sub

    Private Sub SaveCurrentSessions()
        Dim sessionsToSave As New List(Of SavedSession)()

        For i = 0 To _viewModel.Tabs.Count - 1
            Dim tab = _viewModel.Tabs(i)
            Dim savedSession As New SavedSession() With {
                .Id = tab.SessionId,
                .Title = tab.Title,
                .WorkingDirectory = tab.WorkingDirectory,
                .CreatedAt = tab.CreatedAt,
                .LastAccessedAt = DateTime.Now,
                .TabIndex = i
            }
            sessionsToSave.Add(savedSession)
        Next

        _sessionService.SaveAll(sessionsToSave)
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
        _viewModel.CreateNewTabWithFavorite(favorite.Folder, favorite.Name, favorite.Command)

        ' Optionally close the panel after opening
        ' ToggleFavoritesPanel()
    End Sub

    Private Sub AddFavorite_Click(sender As Object, e As RoutedEventArgs)
        ' Close the panel before showing dialog to avoid z-order issues
        Dim wasOpen = _isFavoritesPanelOpen
        If wasOpen Then ToggleFavoritesPanel()

        Dim editWindow As New FavoriteEditWindow()
        editWindow.Owner = Me
        If editWindow.ShowDialog() = True Then
            _favoritesService.Add(editWindow.Favorite)
        End If

        ' Reopen panel and refresh
        If wasOpen Then
            ToggleFavoritesPanel()
        End If
    End Sub

    Private Sub DeleteFavorite_Click(sender As Object, e As RoutedEventArgs)
        Dim button = TryCast(sender, Button)
        Dim favorite = TryCast(button?.Tag, Favorite)
        If favorite Is Nothing Then Return

        DeleteFavoriteWithConfirmation(favorite)
    End Sub

    Private Sub DeleteFavoriteWithConfirmation(favorite As Favorite)
        ' Close the panel before showing dialog to avoid z-order issues
        Dim wasOpen = _isFavoritesPanelOpen
        If wasOpen Then ToggleFavoritesPanel()

        Dim result = MessageBox.Show(
            $"Delete '{favorite.Name}'?",
            "Delete Favorite",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question)

        If result = MessageBoxResult.Yes Then
            _favoritesService.Remove(favorite)
        End If

        ' Reopen panel and refresh
        If wasOpen Then
            ToggleFavoritesPanel()
        End If
    End Sub

    Private Sub FavoritesList_MouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)
        ' Find the favorite that was right-clicked
        Dim listBox = TryCast(sender, ListBox)
        If listBox Is Nothing Then Return

        ' Get the item under the mouse
        Dim element = TryCast(e.OriginalSource, DependencyObject)
        While element IsNot Nothing AndAlso Not TypeOf element Is ListBoxItem
            element = VisualTreeHelper.GetParent(element)
        End While

        Dim listBoxItem = TryCast(element, ListBoxItem)
        If listBoxItem Is Nothing Then Return

        Dim favorite = TryCast(listBoxItem.DataContext, Favorite)
        If favorite Is Nothing Then Return

        ' Select the item
        listBox.SelectedItem = favorite

        ' Create and show context menu
        Dim contextMenu As New ContextMenu()

        Dim openItem As New MenuItem() With {.Header = "Open"}
        AddHandler openItem.Click, Sub(s, args) OpenFavorite(favorite)
        contextMenu.Items.Add(openItem)

        Dim editItem As New MenuItem() With {.Header = "Edit"}
        AddHandler editItem.Click, Sub(s, args) EditFavorite(favorite)
        contextMenu.Items.Add(editItem)

        contextMenu.Items.Add(New Separator())

        Dim deleteItem As New MenuItem() With {.Header = "Delete"}
        AddHandler deleteItem.Click, Sub(s, args) DeleteFavoriteWithConfirmation(favorite)
        contextMenu.Items.Add(deleteItem)

        contextMenu.IsOpen = True
        e.Handled = True
    End Sub

    Private Sub EditFavorite(favorite As Favorite)
        ' Close the panel before showing dialog to avoid z-order issues
        Dim wasOpen = _isFavoritesPanelOpen
        If wasOpen Then ToggleFavoritesPanel()

        Dim editWindow As New FavoriteEditWindow(favorite)
        editWindow.Owner = Me
        If editWindow.ShowDialog() = True Then
            _favoritesService.Update(editWindow.Favorite)
        End If

        ' Reopen panel and refresh
        If wasOpen Then
            ToggleFavoritesPanel()
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
        ' Close the panel before showing dialog to avoid z-order issues
        Dim wasOpen = _isSettingsPanelOpen
        If wasOpen Then ToggleSettingsPanel()

        Dim dialog As New ManageSessionsWindow(_sessionService)
        dialog.Owner = Me

        Dim shouldRestore = dialog.ShowDialog() = True AndAlso dialog.SessionsToRestore.Count > 0

        ' Reopen panel
        If wasOpen Then
            ToggleSettingsPanel()
        End If

        ' Restore sessions after panel is reopened
        If shouldRestore Then
            For Each session In dialog.SessionsToRestore
                _viewModel.CreateNewTabWithFavorite(session.WorkingDirectory, session.Title)
            Next
        End If
    End Sub

    Private Sub HelpButton_Click(sender As Object, e As RoutedEventArgs)
        MessageBox.Show("ClaudeConsole" & vbCrLf & vbCrLf &
                        "A terminal-like application for Claude AI" & vbCrLf &
                        "Version 1.0.0" & vbCrLf & vbCrLf &
                        "Keyboard Shortcuts:" & vbCrLf &
                        "• Ctrl+T - New Tab" & vbCrLf &
                        "• Ctrl+W - Close Tab", "About ClaudeConsole", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub

    Private Sub FontSizeComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim comboBox = TryCast(sender, ComboBox)
        Dim selectedItem = TryCast(comboBox?.SelectedItem, ComboBoxItem)
        If selectedItem Is Nothing Then Return

        Dim fontSizeStr = TryCast(selectedItem.Tag, String)
        If String.IsNullOrEmpty(fontSizeStr) Then Return

        Dim fontSize As Integer
        If Not Integer.TryParse(fontSizeStr, fontSize) Then Return

        ' Apply font size to all open terminal views
        ApplyFontSizeToAllTerminals(fontSize)

        ' Save setting (check if settings service is initialized to avoid error during startup)
        If _settingsService IsNot Nothing Then
            _settingsService.Settings.TerminalFontSize = fontSize
            _settingsService.Save()
        End If
    End Sub

    Private Sub ApplyFontSizeToAllTerminals(fontSize As Integer)
        ' Find all TerminalView controls in the visual tree and update their font size
        Dim terminalViews = FindVisualChildren(Of TerminalView)(ContentArea)
        For Each terminalView In terminalViews
            terminalView.SetFontSize(fontSize)
        Next
    End Sub

    Private Shared Iterator Function FindVisualChildren(Of T As DependencyObject)(parent As DependencyObject) As IEnumerable(Of T)
        If parent Is Nothing Then Return

        Dim childrenCount = VisualTreeHelper.GetChildrenCount(parent)
        For i = 0 To childrenCount - 1
            Dim child = VisualTreeHelper.GetChild(parent, i)
            If TypeOf child Is T Then
                Yield DirectCast(child, T)
            End If
            For Each grandChild In FindVisualChildren(Of T)(child)
                Yield grandChild
            Next
        Next
    End Function
End Class
