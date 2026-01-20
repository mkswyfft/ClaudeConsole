Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Threading

Namespace Services
    ''' <summary>
    ''' Manages Claude CLI process lifecycle and I/O.
    ''' </summary>
    Public Class ClaudeCliService
        Implements IDisposable

        Private _process As Process
        Private _outputReader As Thread
        Private _errorReader As Thread
        Private _isDisposed As Boolean

        ''' <summary>
        ''' Fired when output is received from Claude CLI stdout.
        ''' </summary>
        Public Event OutputReceived As EventHandler(Of String)

        ''' <summary>
        ''' Fired when output is received from Claude CLI stderr.
        ''' </summary>
        Public Event ErrorReceived As EventHandler(Of String)

        ''' <summary>
        ''' Fired when the Claude CLI process exits.
        ''' </summary>
        Public Event SessionEnded As EventHandler(Of Integer)

        ''' <summary>
        ''' Gets whether the CLI process is currently running.
        ''' </summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _process IsNot Nothing AndAlso Not _process.HasExited
            End Get
        End Property

        ''' <summary>
        ''' Gets the underlying process.
        ''' </summary>
        Public ReadOnly Property Process As Process
            Get
                Return _process
            End Get
        End Property

        ''' <summary>
        ''' Starts a new Claude CLI session.
        ''' </summary>
        ''' <param name="workingDirectory">The working directory for the CLI.</param>
        ''' <returns>True if started successfully, False otherwise.</returns>
        Public Function StartSession(workingDirectory As String) As Boolean
            If IsRunning Then
                Return False
            End If

            Try
                Dim startInfo As New ProcessStartInfo() With {
                    .FileName = "claude",
                    .UseShellExecute = False,
                    .RedirectStandardInput = True,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True,
                    .WorkingDirectory = workingDirectory,
                    .StandardOutputEncoding = Encoding.UTF8,
                    .StandardErrorEncoding = Encoding.UTF8
                }

                _process = New Process() With {
                    .StartInfo = startInfo,
                    .EnableRaisingEvents = True
                }

                AddHandler _process.Exited, AddressOf OnProcessExited

                _process.Start()

                ' Start background threads to read output
                _outputReader = New Thread(AddressOf ReadOutputStream)
                _outputReader.IsBackground = True
                _outputReader.Start()

                _errorReader = New Thread(AddressOf ReadErrorStream)
                _errorReader.IsBackground = True
                _errorReader.Start()

                Return True
            Catch ex As Exception
                RaiseEvent ErrorReceived(Me, $"Failed to start Claude CLI: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Sends input to the Claude CLI process.
        ''' </summary>
        ''' <param name="input">The input text to send.</param>
        Public Sub SendInput(input As String)
            If Not IsRunning Then
                Return
            End If

            Try
                _process.StandardInput.WriteLine(input)
                _process.StandardInput.Flush()
            Catch ex As Exception
                RaiseEvent ErrorReceived(Me, $"Failed to send input: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Stops the Claude CLI session.
        ''' </summary>
        Public Sub StopSession()
            If Not IsRunning Then
                Return
            End If

            Try
                ' Try graceful shutdown first
                _process.StandardInput.WriteLine("/exit")
                _process.StandardInput.Flush()

                ' Wait a bit for graceful exit
                If Not _process.WaitForExit(2000) Then
                    ' Force kill if it doesn't exit gracefully
                    _process.Kill()
                End If
            Catch ex As Exception
                ' Process may have already exited
                Try
                    If Not _process.HasExited Then
                        _process.Kill()
                    End If
                Catch
                    ' Ignore
                End Try
            End Try
        End Sub

        Private Sub ReadOutputStream()
            Try
                Dim buffer(4095) As Char
                Dim reader As StreamReader = _process.StandardOutput

                While Not _process.HasExited OrElse Not reader.EndOfStream
                    Dim count As Integer = reader.Read(buffer, 0, buffer.Length)
                    If count > 0 Then
                        Dim text As String = New String(buffer, 0, count)
                        RaiseEvent OutputReceived(Me, text)
                    End If
                End While
            Catch ex As Exception
                ' Stream closed or process ended
            End Try
        End Sub

        Private Sub ReadErrorStream()
            Try
                Dim buffer(4095) As Char
                Dim reader As StreamReader = _process.StandardError

                While Not _process.HasExited OrElse Not reader.EndOfStream
                    Dim count As Integer = reader.Read(buffer, 0, buffer.Length)
                    If count > 0 Then
                        Dim text As String = New String(buffer, 0, count)
                        RaiseEvent ErrorReceived(Me, text)
                    End If
                End While
            Catch ex As Exception
                ' Stream closed or process ended
            End Try
        End Sub

        Private Sub OnProcessExited(sender As Object, e As EventArgs)
            Dim exitCode As Integer = 0
            Try
                exitCode = _process.ExitCode
            Catch
                ' Process may not have exit code available
            End Try
            RaiseEvent SessionEnded(Me, exitCode)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _isDisposed Then
                Return
            End If

            StopSession()

            If _process IsNot Nothing Then
                RemoveHandler _process.Exited, AddressOf OnProcessExited
                _process.Dispose()
                _process = Nothing
            End If

            _isDisposed = True
        End Sub
    End Class
End Namespace
