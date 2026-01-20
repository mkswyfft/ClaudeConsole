Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports ClaudeConsole.ViewModels

Class MainWindow
    Private ReadOnly _viewModel As MainViewModel

    ' Windows API for dark title bar
    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function

    Private Const DWMWA_USE_IMMERSIVE_DARK_MODE As Integer = 20

    Public Sub New()
        InitializeComponent()

        _viewModel = New MainViewModel(Dispatcher)
        DataContext = _viewModel

        ' Apply dark title bar when window is loaded
        AddHandler Loaded, AddressOf OnWindowLoaded
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
    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        ' Clean up all sessions before closing
        _viewModel.Shutdown()
    End Sub

    Private Sub FavoritesButton_Click(sender As Object, e As RoutedEventArgs)
        MessageBox.Show("Favorites management will be available in a future update." & vbCrLf & vbCrLf &
                        "You'll be able to:" & vbCrLf &
                        "• Save favorite folders" & vbCrLf &
                        "• Save favorite commands" & vbCrLf &
                        "• Quickly launch projects", "Favorites", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub

    Private Sub SettingsButton_Click(sender As Object, e As RoutedEventArgs)
        MessageBox.Show("Settings will be available in a future update." & vbCrLf & vbCrLf &
                        "Planned settings:" & vbCrLf &
                        "• Theme customization" & vbCrLf &
                        "• Font settings" & vbCrLf &
                        "• Claude CLI configuration", "Settings", MessageBoxButton.OK, MessageBoxImage.Information)
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
