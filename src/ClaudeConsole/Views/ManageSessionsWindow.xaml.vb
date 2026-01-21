Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports ClaudeConsole.Models
Imports ClaudeConsole.Services

Partial Class ManageSessionsWindow

    ' Windows API for dark title bar
    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function

    Private Const DWMWA_USE_IMMERSIVE_DARK_MODE As Integer = 20

    Private ReadOnly _sessionService As SessionService
    Private _sessionsToRestore As New List(Of SavedSession)()

    ''' <summary>
    ''' Gets the sessions selected for restore.
    ''' </summary>
    Public ReadOnly Property SessionsToRestore As IReadOnlyList(Of SavedSession)
        Get
            Return _sessionsToRestore.AsReadOnly()
        End Get
    End Property

    ''' <summary>
    ''' Creates a new ManageSessionsWindow.
    ''' </summary>
    Public Sub New(sessionService As SessionService)
        InitializeComponent()
        _sessionService = sessionService
        RefreshSessionsList()

        ' Enable/disable restore button based on selection
        AddHandler SessionsList.SelectionChanged, AddressOf OnSelectionChanged
    End Sub

    Private Sub RefreshSessionsList()
        SessionsList.ItemsSource = Nothing
        SessionsList.ItemsSource = _sessionService.Sessions.OrderByDescending(Function(s) s.LastAccessedAt).ToList()

        ' Show empty state if no sessions
        EmptyText.Visibility = If(_sessionService.Count = 0, Visibility.Visible, Visibility.Collapsed)

        ' Update button states
        ClearAllButton.IsEnabled = _sessionService.Count > 0
        RestoreButton.IsEnabled = False
    End Sub

    Private Sub OnSelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        RestoreButton.IsEnabled = SessionsList.SelectedItems.Count > 0
    End Sub

    Private Sub DeleteSession_Click(sender As Object, e As RoutedEventArgs)
        Dim button = TryCast(sender, Button)
        Dim session = TryCast(button?.Tag, SavedSession)
        If session Is Nothing Then Return

        Dim result = MessageBox.Show(
            $"Delete saved session '{session.Title}'?",
            "Delete Session",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question)

        If result = MessageBoxResult.Yes Then
            _sessionService.Delete(session.Id)
            RefreshSessionsList()
        End If
    End Sub

    Private Sub RestoreButton_Click(sender As Object, e As RoutedEventArgs)
        _sessionsToRestore.Clear()

        For Each item In SessionsList.SelectedItems
            Dim session = TryCast(item, SavedSession)
            If session IsNot Nothing Then
                _sessionsToRestore.Add(session)
            End If
        Next

        If _sessionsToRestore.Count > 0 Then
            DialogResult = True
            Close()
        End If
    End Sub

    Private Sub ClearAllButton_Click(sender As Object, e As RoutedEventArgs)
        Dim result = MessageBox.Show(
            $"Delete all {_sessionService.Count} saved sessions?{Environment.NewLine}{Environment.NewLine}This action cannot be undone.",
            "Clear All Sessions",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning)

        If result = MessageBoxResult.Yes Then
            _sessionService.Clear()
            RefreshSessionsList()
        End If
    End Sub

    Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
        Close()
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        ' Enable dark mode for title bar (Windows 10 1809+ / Windows 11)
        Try
            Dim hwnd As IntPtr = New WindowInteropHelper(Me).Handle
            Dim darkMode As Integer = 1
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, darkMode, Marshal.SizeOf(GetType(Integer)))
        Catch
            ' Ignore if not supported on this Windows version
        End Try
    End Sub
End Class
