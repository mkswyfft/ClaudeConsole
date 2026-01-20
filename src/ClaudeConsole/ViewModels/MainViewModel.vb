Option Strict On
Option Explicit On

Imports System.Collections.ObjectModel
Imports System.Windows.Threading
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input

Namespace ViewModels
    ''' <summary>
    ''' Main ViewModel for the application window.
    ''' </summary>
    Public Class MainViewModel
        Inherits ObservableObject

        Private ReadOnly _dispatcher As Dispatcher
        Private _selectedTab As TabViewModel
        Private _selectedTabIndex As Integer

        ''' <summary>
        ''' Collection of open tabs.
        ''' </summary>
        Public ReadOnly Property Tabs As New ObservableCollection(Of TabViewModel)

        ''' <summary>
        ''' Gets or sets the currently selected tab.
        ''' </summary>
        Public Property SelectedTab As TabViewModel
            Get
                Return _selectedTab
            End Get
            Set(value As TabViewModel)
                SetProperty(_selectedTab, value)
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the selected tab index.
        ''' </summary>
        Public Property SelectedTabIndex As Integer
            Get
                Return _selectedTabIndex
            End Get
            Set(value As Integer)
                SetProperty(_selectedTabIndex, value)
            End Set
        End Property

        ''' <summary>
        ''' Command to create a new tab.
        ''' </summary>
        Public ReadOnly Property NewTabCommand As IRelayCommand

        ''' <summary>
        ''' Command to close a specific tab.
        ''' </summary>
        Public ReadOnly Property CloseTabCommand As IRelayCommand(Of TabViewModel)

        ''' <summary>
        ''' Command to close all tabs.
        ''' </summary>
        Public ReadOnly Property CloseAllTabsCommand As IRelayCommand

        Public Sub New()
            Me.New(Nothing)
        End Sub

        Public Sub New(dispatcher As Dispatcher)
            _dispatcher = If(dispatcher, Dispatcher.CurrentDispatcher)

            ' Initialize commands
            NewTabCommand = New RelayCommand(AddressOf ExecuteNewTab)
            CloseTabCommand = New RelayCommand(Of TabViewModel)(AddressOf ExecuteCloseTab, AddressOf CanCloseTab)
            CloseAllTabsCommand = New RelayCommand(AddressOf ExecuteCloseAllTabs, AddressOf CanCloseAllTabs)

            ' Create initial tab
            CreateNewTab()
        End Sub

        Private Sub ExecuteNewTab()
            CreateNewTab()
        End Sub

        Private Function CanCloseTab(tab As TabViewModel) As Boolean
            Return tab IsNot Nothing AndAlso Tabs.Contains(tab)
        End Function

        Private Sub ExecuteCloseTab(tab As TabViewModel)
            If tab Is Nothing Then
                Return
            End If

            CloseTab(tab)
        End Sub

        Private Function CanCloseAllTabs() As Boolean
            Return Tabs.Count > 0
        End Function

        Private Sub ExecuteCloseAllTabs()
            While Tabs.Count > 0
                CloseTab(Tabs(0))
            End While
        End Sub

        Private Sub CreateNewTab()
            Dim tab As New TabViewModel(_dispatcher)
            tab.Title = $"Session {Tabs.Count + 1}"

            AddHandler tab.CloseRequested, AddressOf OnTabCloseRequested

            Tabs.Add(tab)
            SelectedTab = tab
            SelectedTabIndex = Tabs.Count - 1

            UpdateCommandStates()
        End Sub

        Private Sub CloseTab(tab As TabViewModel)
            If tab Is Nothing Then
                Return
            End If

            Dim index = Tabs.IndexOf(tab)

            RemoveHandler tab.CloseRequested, AddressOf OnTabCloseRequested

            ' Stop the CLI session if running
            tab.Dispose()
            Tabs.Remove(tab)

            ' Select another tab if available
            If Tabs.Count > 0 Then
                If index >= Tabs.Count Then
                    index = Tabs.Count - 1
                End If
                SelectedTabIndex = index
                SelectedTab = Tabs(index)
            Else
                SelectedTab = Nothing
                SelectedTabIndex = -1
            End If

            UpdateCommandStates()
        End Sub

        Private Sub OnTabCloseRequested(sender As Object, e As EventArgs)
            Dim tab = TryCast(sender, TabViewModel)
            If tab IsNot Nothing Then
                CloseTab(tab)
            End If
        End Sub

        Private Sub UpdateCommandStates()
            DirectCast(CloseAllTabsCommand, RelayCommand).NotifyCanExecuteChanged()
        End Sub

        ''' <summary>
        ''' Closes all tabs and cleans up resources.
        ''' </summary>
        Public Sub Shutdown()
            For Each t As TabViewModel In Tabs.ToList()
                t.Dispose()
            Next
            Tabs.Clear()
        End Sub
    End Class
End Namespace
