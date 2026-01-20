Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports ClaudeConsole.Models

Partial Class FavoriteEditWindow

    ' Windows API for dark title bar
    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function

    Private Const DWMWA_USE_IMMERSIVE_DARK_MODE As Integer = 20

    Private _favorite As Favorite
    Private _isNew As Boolean

    ''' <summary>
    ''' Gets the edited favorite.
    ''' </summary>
    Public ReadOnly Property Favorite As Favorite
        Get
            Return _favorite
        End Get
    End Property

    ''' <summary>
    ''' Creates a new FavoriteEditWindow for adding a new favorite.
    ''' </summary>
    Public Sub New()
        InitializeComponent()
        _favorite = New Favorite()
        _isNew = True
        Title = "Add Favorite"
    End Sub

    ''' <summary>
    ''' Creates a new FavoriteEditWindow for editing an existing favorite.
    ''' </summary>
    Public Sub New(favorite As Favorite)
        InitializeComponent()
        _favorite = New Favorite() With {
            .Id = favorite.Id,
            .Name = favorite.Name,
            .Folder = favorite.Folder,
            .Command = favorite.Command,
            .Icon = favorite.Icon,
            .Color = favorite.Color,
            .SortOrder = favorite.SortOrder
        }
        _isNew = False
        Title = "Edit Favorite"
        LoadFavorite()
    End Sub

    Private Sub LoadFavorite()
        NameTextBox.Text = _favorite.Name
        FolderTextBox.Text = _favorite.Folder
        CommandTextBox.Text = _favorite.Command
    End Sub

    Private Sub BrowseButton_Click(sender As Object, e As RoutedEventArgs)
        Dim dialog As New System.Windows.Forms.FolderBrowserDialog()
        dialog.Description = "Select folder"
        dialog.ShowNewFolderButton = False

        If Not String.IsNullOrEmpty(FolderTextBox.Text) AndAlso IO.Directory.Exists(FolderTextBox.Text) Then
            dialog.SelectedPath = FolderTextBox.Text
        End If

        If dialog.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            FolderTextBox.Text = dialog.SelectedPath
        End If
    End Sub

    Private Sub SaveButton_Click(sender As Object, e As RoutedEventArgs)
        ' Validate
        If String.IsNullOrWhiteSpace(NameTextBox.Text) Then
            MessageBox.Show("Please enter a name for the favorite.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning)
            NameTextBox.Focus()
            Return
        End If

        ' Validate folder path if provided
        If Not String.IsNullOrWhiteSpace(FolderTextBox.Text) AndAlso Not IO.Directory.Exists(FolderTextBox.Text) Then
            Dim result = MessageBox.Show(
                "The specified folder does not exist. Save anyway?",
                "Folder Not Found",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question)
            If result = MessageBoxResult.No Then
                FolderTextBox.Focus()
                Return
            End If
        End If

        ' Update favorite
        _favorite.Name = NameTextBox.Text.Trim()
        _favorite.Folder = FolderTextBox.Text.Trim()
        _favorite.Command = CommandTextBox.Text.Trim()

        DialogResult = True
        Close()
    End Sub

    Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
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
