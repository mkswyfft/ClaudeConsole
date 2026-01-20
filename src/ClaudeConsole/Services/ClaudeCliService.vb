Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Threading
Imports ClaudeConsole.ConPty

Namespace Services
    ''' <summary>
    ''' Manages Claude CLI interactions using ConPTY for full interactive mode,
    ''' with print mode as a fallback.
    ''' </summary>
    Public Class ClaudeCliService
        Implements IDisposable

        Private _workingDirectory As String
        Private _isSessionActive As Boolean
        Private _isDisposed As Boolean

        ' ConPTY mode (disabled for now - print mode is more reliable)
        Private _conPtyTerminal As ConPtyTerminal
        Private _useConPty As Boolean = False

        ' Fallback print mode
        Private _isFirstMessage As Boolean = True
        Private _currentProcess As Process
        Private _cancellationSource As CancellationTokenSource

        ''' <summary>
        ''' Fired when output is received from Claude CLI.
        ''' </summary>
        Public Event OutputReceived As EventHandler(Of String)

        ''' <summary>
        ''' Fired when error output is received.
        ''' </summary>
        Public Event ErrorReceived As EventHandler(Of String)

        ''' <summary>
        ''' Fired when a message response is complete (print mode only).
        ''' </summary>
        Public Event ResponseComplete As EventHandler(Of Integer)

        ''' <summary>
        ''' Fired when the session ends.
        ''' </summary>
        Public Event SessionEnded As EventHandler(Of Integer)

        ''' <summary>
        ''' Gets whether the session is active.
        ''' </summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isSessionActive
            End Get
        End Property

        ''' <summary>
        ''' Gets whether a command is currently being processed (print mode only).
        ''' </summary>
        Public ReadOnly Property IsProcessing As Boolean
            Get
                If _useConPty Then
                    Return False ' ConPTY mode is always ready for input
                End If
                Return _currentProcess IsNot Nothing AndAlso Not _currentProcess.HasExited
            End Get
        End Property

        ''' <summary>
        ''' Gets or sets whether to use ConPTY mode.
        ''' </summary>
        Public Property UseConPty As Boolean
            Get
                Return _useConPty
            End Get
            Set(value As Boolean)
                If _isSessionActive Then
                    Throw New InvalidOperationException("Cannot change mode while session is active.")
                End If
                _useConPty = value
            End Set
        End Property

        ''' <summary>
        ''' Starts a new Claude CLI session.
        ''' </summary>
        ''' <param name="workingDirectory">The working directory for the CLI.</param>
        ''' <returns>True if started successfully, False otherwise.</returns>
        Public Function StartSession(workingDirectory As String) As Boolean
            If _isSessionActive Then
                Return False
            End If

            _workingDirectory = workingDirectory

            If _useConPty Then
                Return StartConPtySession()
            Else
                Return StartPrintModeSession()
            End If
        End Function

        Private Function StartConPtySession() As Boolean
            Try
                _conPtyTerminal = New ConPtyTerminal()

                AddHandler _conPtyTerminal.OutputReceived, AddressOf OnConPtyOutput
                AddHandler _conPtyTerminal.ProcessExited, AddressOf OnConPtyExited

                ' Start claude in interactive mode
                If _conPtyTerminal.Start("claude", _workingDirectory) Then
                    _isSessionActive = True
                    Return True
                Else
                    ' ConPTY failed, try fallback
                    Dim errorMsg = If(_conPtyTerminal.LastError, "Unknown error")
                    RaiseEvent ErrorReceived(Me, $"ConPTY failed: {errorMsg}. Falling back to print mode...")
                    _useConPty = False
                    CleanupConPty()
                    Return StartPrintModeSession()
                End If
            Catch ex As Exception
                RaiseEvent ErrorReceived(Me, $"ConPTY error: {ex.Message}. Falling back to print mode...")
                _useConPty = False
                CleanupConPty()
                Return StartPrintModeSession()
            End Try
        End Function

        Private Function StartPrintModeSession() As Boolean
            _isFirstMessage = True
            _cancellationSource = New CancellationTokenSource()
            _isSessionActive = True
            Return True
        End Function

        Private Sub OnConPtyOutput(sender As Object, output As String)
            RaiseEvent OutputReceived(Me, output)
        End Sub

        Private Sub OnConPtyExited(sender As Object, exitCode As Integer)
            _isSessionActive = False
            RaiseEvent SessionEnded(Me, exitCode)
        End Sub

        ''' <summary>
        ''' Sends input to Claude.
        ''' </summary>
        ''' <param name="input">The input text to send.</param>
        Public Sub SendInput(input As String)
            If Not _isSessionActive Then
                RaiseEvent ErrorReceived(Me, "Session is not active. Click Start to begin.")
                Return
            End If

            If _useConPty AndAlso _conPtyTerminal IsNot Nothing Then
                ' ConPTY mode - send directly
                _conPtyTerminal.SendLine(input)
            Else
                ' Print mode - async
                SendInputAsync(input)
            End If
        End Sub

        ''' <summary>
        ''' Sends input asynchronously (print mode).
        ''' </summary>
        Private Async Sub SendInputAsync(input As String)
            If IsProcessing Then
                RaiseEvent ErrorReceived(Me, "Please wait for the current response to complete.")
                Return
            End If

            Try
                ' Build command arguments
                Dim args As String
                If _isFirstMessage Then
                    args = $"-p ""{EscapeArgument(input)}"""
                    _isFirstMessage = False
                Else
                    args = $"-p --continue ""{EscapeArgument(input)}"""
                End If

                Dim startInfo As New ProcessStartInfo() With {
                    .FileName = "claude",
                    .Arguments = args,
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True,
                    .WorkingDirectory = _workingDirectory,
                    .StandardOutputEncoding = Encoding.UTF8,
                    .StandardErrorEncoding = Encoding.UTF8
                }

                _currentProcess = New Process() With {
                    .StartInfo = startInfo,
                    .EnableRaisingEvents = True
                }

                _currentProcess.Start()

                Dim outputTask = ReadStreamAsync(_currentProcess.StandardOutput, False)
                Dim errorTask = ReadStreamAsync(_currentProcess.StandardError, True)

                Await Task.WhenAll(outputTask, errorTask)
                Await Task.Run(Sub() _currentProcess.WaitForExit())

                Dim exitCode As Integer = _currentProcess.ExitCode
                RaiseEvent ResponseComplete(Me, exitCode)

                _currentProcess.Dispose()
                _currentProcess = Nothing

            Catch ex As OperationCanceledException
                ' Cancelled
            Catch ex As Exception
                RaiseEvent ErrorReceived(Me, $"Error: {ex.Message}")
            End Try
        End Sub

        Private Async Function ReadStreamAsync(reader As StreamReader, isError As Boolean) As Task
            Try
                Dim buffer(4095) As Char
                Dim count As Integer

                Do
                    count = Await reader.ReadAsync(buffer, 0, buffer.Length)
                    If count > 0 Then
                        Dim text As String = New String(buffer, 0, count)
                        If isError Then
                            RaiseEvent ErrorReceived(Me, text)
                        Else
                            RaiseEvent OutputReceived(Me, text)
                        End If
                    End If
                Loop While count > 0
            Catch
                ' Stream closed
            End Try
        End Function

        Private Function EscapeArgument(input As String) As String
            Return input.Replace("\", "\\").Replace("""", "\""")
        End Function

        ''' <summary>
        ''' Sends Ctrl+C to interrupt the current operation.
        ''' </summary>
        Public Sub SendInterrupt()
            If _useConPty AndAlso _conPtyTerminal IsNot Nothing Then
                _conPtyTerminal.SendCtrlC()
            ElseIf _currentProcess IsNot Nothing AndAlso Not _currentProcess.HasExited Then
                Try
                    _currentProcess.Kill()
                Catch
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Stops the Claude CLI session.
        ''' </summary>
        Public Sub StopSession()
            If _useConPty Then
                _conPtyTerminal?.Stop()
                CleanupConPty()
            Else
                If _currentProcess IsNot Nothing AndAlso Not _currentProcess.HasExited Then
                    Try
                        _currentProcess.Kill()
                    Catch
                    End Try
                End If
            End If

            If _isSessionActive Then
                _isSessionActive = False
                _isFirstMessage = True
                RaiseEvent SessionEnded(Me, 0)
            End If
        End Sub

        Private Sub CleanupConPty()
            If _conPtyTerminal IsNot Nothing Then
                RemoveHandler _conPtyTerminal.OutputReceived, AddressOf OnConPtyOutput
                RemoveHandler _conPtyTerminal.ProcessExited, AddressOf OnConPtyExited
                _conPtyTerminal.Dispose()
                _conPtyTerminal = Nothing
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _isDisposed Then
                Return
            End If

            StopSession()
            CleanupConPty()

            _cancellationSource?.Cancel()
            _cancellationSource?.Dispose()
            _cancellationSource = Nothing

            _currentProcess?.Dispose()
            _currentProcess = Nothing

            _isDisposed = True
        End Sub
    End Class
End Namespace
