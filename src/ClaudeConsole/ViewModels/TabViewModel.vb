Option Strict On
Option Explicit On

Imports System.Windows.Documents
Imports System.Windows.Threading
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports ClaudeConsole.Models
Imports ClaudeConsole.Services
Imports ClaudeConsole.Utilities

Namespace ViewModels
    ''' <summary>
    ''' ViewModel for a single tab containing a Claude CLI session.
    ''' </summary>
    Public Class TabViewModel
        Inherits ObservableObject
        Implements IDisposable

        Private ReadOnly _session As TabSession
        Private ReadOnly _cliService As ClaudeCliService
        Private ReadOnly _dispatcher As Dispatcher
        Private _isDisposed As Boolean

        Private _title As String = "New Session"
        Private _inputText As String = String.Empty
        Private _isRunning As Boolean
        Private _flowDocument As FlowDocument

        ''' <summary>
        ''' Gets or sets the tab title.
        ''' </summary>
        Public Property Title As String
            Get
                Return _title
            End Get
            Set(value As String)
                SetProperty(_title, value)
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the current input text.
        ''' </summary>
        Public Property InputText As String
            Get
                Return _inputText
            End Get
            Set(value As String)
                SetProperty(_inputText, value)
            End Set
        End Property

        ''' <summary>
        ''' Gets whether the CLI is running.
        ''' </summary>
        Public Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
            Private Set(value As Boolean)
                SetProperty(_isRunning, value)
            End Set
        End Property

        ''' <summary>
        ''' Gets the FlowDocument for the terminal output.
        ''' </summary>
        Public Property OutputDocument As FlowDocument
            Get
                Return _flowDocument
            End Get
            Private Set(value As FlowDocument)
                SetProperty(_flowDocument, value)
            End Set
        End Property

        ''' <summary>
        ''' Gets the session ID.
        ''' </summary>
        Public ReadOnly Property SessionId As Guid
            Get
                Return _session.Id
            End Get
        End Property

        ''' <summary>
        ''' Gets the working directory.
        ''' </summary>
        Public Property WorkingDirectory As String
            Get
                Return _session.WorkingDirectory
            End Get
            Set(value As String)
                _session.WorkingDirectory = value
                OnPropertyChanged(NameOf(WorkingDirectory))
            End Set
        End Property

        ''' <summary>
        ''' Command to send input to the CLI.
        ''' </summary>
        Public ReadOnly Property SendCommand As IRelayCommand

        ''' <summary>
        ''' Command to clear the output.
        ''' </summary>
        Public ReadOnly Property ClearCommand As IRelayCommand

        ''' <summary>
        ''' Command to start the CLI session.
        ''' </summary>
        Public ReadOnly Property StartCommand As IRelayCommand

        ''' <summary>
        ''' Command to stop the CLI session.
        ''' </summary>
        Public ReadOnly Property StopCommand As IRelayCommand

        ''' <summary>
        ''' Event raised when this tab requests to be closed.
        ''' </summary>
        Public Event CloseRequested As EventHandler

        Public Sub New()
            Me.New(Nothing)
        End Sub

        Public Sub New(dispatcher As Dispatcher)
            _dispatcher = If(dispatcher, Dispatcher.CurrentDispatcher)
            _session = New TabSession()
            _cliService = New ClaudeCliService()

            ' Initialize FlowDocument
            _flowDocument = New FlowDocument()
            _flowDocument.PagePadding = New Thickness(10)
            _flowDocument.FontFamily = New FontFamily("Consolas")
            _flowDocument.FontSize = 14
            _flowDocument.Background = Brushes.Transparent
            _flowDocument.Foreground = Brushes.LightGray

            ' Initialize commands
            SendCommand = New RelayCommand(AddressOf ExecuteSend, AddressOf CanSend)
            ClearCommand = New RelayCommand(AddressOf ExecuteClear)
            StartCommand = New RelayCommand(AddressOf ExecuteStart, AddressOf CanStart)
            StopCommand = New RelayCommand(AddressOf ExecuteStop, AddressOf CanStop)

            ' Subscribe to CLI events
            AddHandler _cliService.OutputReceived, AddressOf OnOutputReceived
            AddHandler _cliService.ErrorReceived, AddressOf OnErrorReceived
            AddHandler _cliService.SessionEnded, AddressOf OnSessionEnded
        End Sub

        Private Function CanSend() As Boolean
            Return IsRunning AndAlso Not String.IsNullOrWhiteSpace(InputText)
        End Function

        Private Sub ExecuteSend()
            If String.IsNullOrWhiteSpace(InputText) Then
                Return
            End If

            ' Show user input in output
            AppendOutput($"> {InputText}{Environment.NewLine}", Brushes.Cyan)

            _cliService.SendInput(InputText)
            InputText = String.Empty
        End Sub

        Private Sub ExecuteClear()
            _dispatcher.Invoke(Sub()
                                   OutputDocument.Blocks.Clear()
                               End Sub)
        End Sub

        Private Function CanStart() As Boolean
            Return Not IsRunning
        End Function

        Private Sub ExecuteStart()
            If _cliService.StartSession(WorkingDirectory) Then
                IsRunning = True
                Title = $"Claude - {IO.Path.GetFileName(WorkingDirectory)}"
                AppendOutput($"Started Claude CLI session in {WorkingDirectory}{Environment.NewLine}", Brushes.Green)
            Else
                AppendOutput("Failed to start Claude CLI session." & Environment.NewLine, Brushes.Red)
            End If
            UpdateCommandStates()
        End Sub

        Private Function CanStop() As Boolean
            Return IsRunning
        End Function

        Private Sub ExecuteStop()
            _cliService.StopSession()
        End Sub

        Private Sub OnOutputReceived(sender As Object, output As String)
            AppendFormattedOutput(output)
        End Sub

        Private Sub OnErrorReceived(sender As Object, errorText As String)
            AppendOutput(errorText, Brushes.Red)
        End Sub

        Private Sub OnSessionEnded(sender As Object, exitCode As Integer)
            _dispatcher.Invoke(Sub()
                                   IsRunning = False
                                   AppendOutput($"{Environment.NewLine}Session ended (exit code: {exitCode}){Environment.NewLine}", Brushes.Yellow)
                                   UpdateCommandStates()
                               End Sub)
        End Sub

        Private Sub AppendOutput(text As String, foreground As Brush)
            _dispatcher.Invoke(Sub()
                                   Dim paragraph As Paragraph
                                   If OutputDocument.Blocks.Count > 0 AndAlso TypeOf OutputDocument.Blocks.LastBlock Is Paragraph Then
                                       paragraph = DirectCast(OutputDocument.Blocks.LastBlock, Paragraph)
                                   Else
                                       paragraph = New Paragraph()
                                       paragraph.Margin = New Thickness(0)
                                       OutputDocument.Blocks.Add(paragraph)
                                   End If

                                   Dim run As New Run(text) With {
                                       .Foreground = foreground
                                   }
                                   paragraph.Inlines.Add(run)
                               End Sub)
        End Sub

        Private Sub AppendFormattedOutput(text As String)
            _dispatcher.Invoke(Sub()
                                   Dim segments = AnsiParser.Parse(text)
                                   Dim runs = AnsiParser.CreateRuns(segments)

                                   Dim paragraph As Paragraph
                                   If OutputDocument.Blocks.Count > 0 AndAlso TypeOf OutputDocument.Blocks.LastBlock Is Paragraph Then
                                       paragraph = DirectCast(OutputDocument.Blocks.LastBlock, Paragraph)
                                   Else
                                       paragraph = New Paragraph()
                                       paragraph.Margin = New Thickness(0)
                                       OutputDocument.Blocks.Add(paragraph)
                                   End If

                                   For Each run In runs
                                       ' Check for newlines and create new paragraphs
                                       If run.Text.Contains(Environment.NewLine) OrElse run.Text.Contains(vbLf) Then
                                           Dim parts = run.Text.Split({Environment.NewLine, vbLf}, StringSplitOptions.None)
                                           For i = 0 To parts.Length - 1
                                               If Not String.IsNullOrEmpty(parts(i)) Then
                                                   Dim partRun As New Run(parts(i)) With {
                                                       .Foreground = run.Foreground,
                                                       .FontWeight = run.FontWeight
                                                   }
                                                   paragraph.Inlines.Add(partRun)
                                               End If
                                               If i < parts.Length - 1 Then
                                                   paragraph = New Paragraph()
                                                   paragraph.Margin = New Thickness(0)
                                                   OutputDocument.Blocks.Add(paragraph)
                                               End If
                                           Next
                                       Else
                                           paragraph.Inlines.Add(run)
                                       End If
                                   Next
                               End Sub)
        End Sub

        Private Sub UpdateCommandStates()
            DirectCast(SendCommand, RelayCommand).NotifyCanExecuteChanged()
            DirectCast(StartCommand, RelayCommand).NotifyCanExecuteChanged()
            DirectCast(StopCommand, RelayCommand).NotifyCanExecuteChanged()
        End Sub

        Public Sub RequestClose()
            RaiseEvent CloseRequested(Me, EventArgs.Empty)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _isDisposed Then
                Return
            End If

            RemoveHandler _cliService.OutputReceived, AddressOf OnOutputReceived
            RemoveHandler _cliService.ErrorReceived, AddressOf OnErrorReceived
            RemoveHandler _cliService.SessionEnded, AddressOf OnSessionEnded

            _cliService.Dispose()
            _isDisposed = True
        End Sub
    End Class
End Namespace
