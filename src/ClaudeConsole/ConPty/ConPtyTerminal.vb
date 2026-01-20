Option Strict On
Option Explicit On

Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports Microsoft.Win32.SafeHandles

Namespace ConPty
    ''' <summary>
    ''' Manages a ConPTY (Windows Pseudo Console) session for running interactive CLI applications.
    ''' </summary>
    Public Class ConPtyTerminal
        Implements IDisposable

        Private _inputPipeRead As SafeFileHandle
        Private _inputPipeWrite As SafeFileHandle
        Private _outputPipeRead As SafeFileHandle
        Private _outputPipeWrite As SafeFileHandle
        Private _pseudoConsoleHandle As IntPtr
        Private _processInfo As NativeApi.PROCESS_INFORMATION
        Private _startupInfo As NativeApi.STARTUPINFOEX
        Private _outputWriter As StreamWriter
        Private _outputReaderTask As Task
        Private _isRunning As Boolean
        Private _isDisposed As Boolean
        Private _cancellationSource As CancellationTokenSource

        Private Const DefaultWidth As Short = 120
        Private Const DefaultHeight As Short = 30

        ''' <summary>
        ''' Fired when output is received from the terminal.
        ''' </summary>
        Public Event OutputReceived As EventHandler(Of String)

        ''' <summary>
        ''' Fired when the process exits.
        ''' </summary>
        Public Event ProcessExited As EventHandler(Of Integer)

        ''' <summary>
        ''' Gets whether the terminal process is running.
        ''' </summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        ''' <summary>
        ''' Gets the last error message if Start() failed.
        ''' </summary>
        Public Property LastError As String

        ''' <summary>
        ''' Starts a new ConPTY session with the specified command.
        ''' </summary>
        ''' <param name="command">The command to run (e.g., "claude").</param>
        ''' <param name="workingDirectory">The working directory for the process.</param>
        ''' <param name="width">Terminal width in characters.</param>
        ''' <param name="height">Terminal height in characters.</param>
        ''' <returns>True if started successfully.</returns>
        Public Function Start(command As String, Optional workingDirectory As String = Nothing, Optional width As Short = DefaultWidth, Optional height As Short = DefaultHeight) As Boolean
            If _isRunning Then
                LastError = "Already running"
                Return False
            End If

            Try
                _cancellationSource = New CancellationTokenSource()

                ' Create pipes for input and output
                If Not CreatePipes() Then
                    LastError = $"Failed to create pipes. Error: {Marshal.GetLastWin32Error()}"
                    Return False
                End If

                ' Create the pseudo console
                Dim conPtyResult = CreatePseudoConsole(width, height)
                If Not conPtyResult.Success Then
                    LastError = conPtyResult.ErrorMessage
                    DisposePipes()
                    Return False
                End If

                ' Start the process
                Dim processResult = StartProcess(command, workingDirectory)
                If Not processResult.Success Then
                    LastError = processResult.ErrorMessage
                    ClosePseudoConsole()
                    DisposePipes()
                    Return False
                End If

                ' Set up the output writer for sending input
                _outputWriter = New StreamWriter(New FileStream(_inputPipeWrite, FileAccess.Write)) With {
                    .AutoFlush = True
                }

                ' Start reading output in background
                _outputReaderTask = Task.Run(Sub() ReadOutputLoop(_cancellationSource.Token))

                _isRunning = True
                Return True

            Catch ex As Exception
                LastError = $"Exception: {ex.Message}"
                Cleanup()
                Return False
            End Try
        End Function

        Private Structure OperationResult
            Public Success As Boolean
            Public ErrorMessage As String
        End Structure

        Private Function CreatePipes() As Boolean
            Dim inputRead As IntPtr = IntPtr.Zero
            Dim inputWrite As IntPtr = IntPtr.Zero
            Dim outputRead As IntPtr = IntPtr.Zero
            Dim outputWrite As IntPtr = IntPtr.Zero

            ' Create input pipe (we write to WriteSide, ConPTY reads from ReadSide)
            If Not NativeApi.CreatePipe(inputRead, inputWrite, IntPtr.Zero, 0) Then
                Return False
            End If

            ' Create output pipe (ConPTY writes to WriteSide, we read from ReadSide)
            If Not NativeApi.CreatePipe(outputRead, outputWrite, IntPtr.Zero, 0) Then
                NativeApi.CloseHandle(inputRead)
                NativeApi.CloseHandle(inputWrite)
                Return False
            End If

            ' Wrap in SafeFileHandle (ownsHandle:=True means they'll be closed on dispose)
            _inputPipeRead = New SafeFileHandle(inputRead, True)
            _inputPipeWrite = New SafeFileHandle(inputWrite, True)
            _outputPipeRead = New SafeFileHandle(outputRead, True)
            _outputPipeWrite = New SafeFileHandle(outputWrite, True)

            Return True
        End Function

        Private Function CreatePseudoConsole(width As Short, height As Short) As OperationResult
            Dim size As New NativeApi.COORD With {
                .X = width,
                .Y = height
            }

            Dim result As Integer = NativeApi.CreatePseudoConsole(
                size,
                _inputPipeRead,
                _outputPipeWrite,
                0,
                _pseudoConsoleHandle)

            If result <> 0 Then
                Return New OperationResult With {
                    .Success = False,
                    .ErrorMessage = $"CreatePseudoConsole failed with HRESULT: 0x{result:X8}"
                }
            End If

            Return New OperationResult With {.Success = True}
        End Function

        Private Function StartProcess(command As String, workingDirectory As String) As OperationResult
            ' Initialize the startup info
            _startupInfo = New NativeApi.STARTUPINFOEX()
            _startupInfo.StartupInfo.cb = Marshal.SizeOf(Of NativeApi.STARTUPINFOEX)()

            ' Get the size needed for the attribute list
            Dim lpSize As IntPtr = IntPtr.Zero
            NativeApi.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, lpSize)

            If lpSize = IntPtr.Zero Then
                Return New OperationResult With {
                    .Success = False,
                    .ErrorMessage = $"InitializeProcThreadAttributeList (size query) failed. Error: {Marshal.GetLastWin32Error()}"
                }
            End If

            ' Allocate the attribute list
            _startupInfo.lpAttributeList = Marshal.AllocHGlobal(lpSize)

            ' Initialize the attribute list
            If Not NativeApi.InitializeProcThreadAttributeList(_startupInfo.lpAttributeList, 1, 0, lpSize) Then
                Dim err = Marshal.GetLastWin32Error()
                Marshal.FreeHGlobal(_startupInfo.lpAttributeList)
                _startupInfo.lpAttributeList = IntPtr.Zero
                Return New OperationResult With {
                    .Success = False,
                    .ErrorMessage = $"InitializeProcThreadAttributeList failed. Error: {err}"
                }
            End If

            ' Set the pseudo console attribute
            If Not NativeApi.UpdateProcThreadAttribute(
                _startupInfo.lpAttributeList,
                0,
                CType(NativeApi.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, IntPtr),
                _pseudoConsoleHandle,
                CType(IntPtr.Size, IntPtr),
                IntPtr.Zero,
                IntPtr.Zero) Then
                Return New OperationResult With {
                    .Success = False,
                    .ErrorMessage = $"UpdateProcThreadAttribute failed. Error: {Marshal.GetLastWin32Error()}"
                }
            End If

            ' Create security attributes
            Dim pSec As New NativeApi.SECURITY_ATTRIBUTES With {
                .nLength = Marshal.SizeOf(Of NativeApi.SECURITY_ATTRIBUTES)()
            }
            Dim tSec As New NativeApi.SECURITY_ATTRIBUTES With {
                .nLength = Marshal.SizeOf(Of NativeApi.SECURITY_ATTRIBUTES)()
            }

            ' Create the process
            Dim success As Boolean = NativeApi.CreateProcess(
                Nothing,
                command,
                pSec,
                tSec,
                False,
                NativeApi.EXTENDED_STARTUPINFO_PRESENT,
                IntPtr.Zero,
                workingDirectory,
                _startupInfo,
                _processInfo)

            If Not success Then
                Return New OperationResult With {
                    .Success = False,
                    .ErrorMessage = $"CreateProcess failed for '{command}'. Error: {Marshal.GetLastWin32Error()}"
                }
            End If

            Return New OperationResult With {.Success = True}
        End Function

        Private Sub ReadOutputLoop(cancellationToken As CancellationToken)
            Try
                Using reader As New FileStream(_outputPipeRead, FileAccess.Read)
                    Dim buffer(4095) As Byte
                    Dim count As Integer

                    While Not cancellationToken.IsCancellationRequested
                        Try
                            count = reader.Read(buffer, 0, buffer.Length)
                            If count > 0 Then
                                Dim text As String = Encoding.UTF8.GetString(buffer, 0, count)
                                RaiseEvent OutputReceived(Me, text)
                            ElseIf count = 0 Then
                                ' Pipe closed, process likely exited
                                Exit While
                            End If
                        Catch ex As IOException
                            ' Pipe broken, process exited
                            Exit While
                        End Try
                    End While
                End Using
            Catch ex As Exception
                ' Stream closed or cancelled
            Finally
                ' Check if process has exited
                CheckProcessExit()
            End Try
        End Sub

        Private Sub CheckProcessExit()
            If _processInfo.hProcess <> IntPtr.Zero Then
                Dim exitCode As UInteger = 0
                NativeApi.GetExitCodeProcess(_processInfo.hProcess, exitCode)

                _isRunning = False
                RaiseEvent ProcessExited(Me, CInt(exitCode))
            End If
        End Sub

        ''' <summary>
        ''' Sends input text to the terminal.
        ''' </summary>
        ''' <param name="text">The text to send.</param>
        Public Sub SendInput(text As String)
            If Not _isRunning OrElse _outputWriter Is Nothing Then
                Return
            End If

            Try
                _outputWriter.Write(text)
            Catch ex As Exception
                ' Pipe may be broken
            End Try
        End Sub

        ''' <summary>
        ''' Sends a line of input to the terminal (appends newline).
        ''' </summary>
        ''' <param name="text">The text to send.</param>
        Public Sub SendLine(text As String)
            SendInput(text & vbCrLf)
        End Sub

        ''' <summary>
        ''' Sends Ctrl+C to the terminal.
        ''' </summary>
        Public Sub SendCtrlC()
            SendInput(ChrW(3))
        End Sub

        ''' <summary>
        ''' Resizes the terminal.
        ''' </summary>
        ''' <param name="width">New width in characters.</param>
        ''' <param name="height">New height in characters.</param>
        Public Sub Resize(width As Short, height As Short)
            If _pseudoConsoleHandle = IntPtr.Zero Then
                Return
            End If

            Dim size As New NativeApi.COORD With {
                .X = width,
                .Y = height
            }
            NativeApi.ResizePseudoConsole(_pseudoConsoleHandle, size)
        End Sub

        ''' <summary>
        ''' Stops the terminal session.
        ''' </summary>
        Public Sub [Stop]()
            If Not _isRunning Then
                Return
            End If

            _cancellationSource?.Cancel()

            ' Try graceful shutdown first
            If _processInfo.hProcess <> IntPtr.Zero Then
                ' Wait briefly for process to exit
                Dim waitResult = NativeApi.WaitForSingleObject(_processInfo.hProcess, 2000)
                If waitResult = NativeApi.WAIT_TIMEOUT Then
                    ' Force terminate
                    NativeApi.TerminateProcess(_processInfo.hProcess, 1)
                End If
            End If

            Cleanup()
            _isRunning = False
        End Sub

        Private Sub ClosePseudoConsole()
            If _pseudoConsoleHandle <> IntPtr.Zero Then
                NativeApi.ClosePseudoConsole(_pseudoConsoleHandle)
                _pseudoConsoleHandle = IntPtr.Zero
            End If
        End Sub

        Private Sub DisposePipes()
            _inputPipeRead?.Dispose()
            _inputPipeWrite?.Dispose()
            _outputPipeRead?.Dispose()
            _outputPipeWrite?.Dispose()
            _inputPipeRead = Nothing
            _inputPipeWrite = Nothing
            _outputPipeRead = Nothing
            _outputPipeWrite = Nothing
        End Sub

        Private Sub Cleanup()
            _outputWriter?.Dispose()
            _outputWriter = Nothing

            ' Close process handles
            If _processInfo.hProcess <> IntPtr.Zero Then
                NativeApi.CloseHandle(_processInfo.hProcess)
                _processInfo.hProcess = IntPtr.Zero
            End If
            If _processInfo.hThread <> IntPtr.Zero Then
                NativeApi.CloseHandle(_processInfo.hThread)
                _processInfo.hThread = IntPtr.Zero
            End If

            ' Free attribute list
            If _startupInfo.lpAttributeList <> IntPtr.Zero Then
                NativeApi.DeleteProcThreadAttributeList(_startupInfo.lpAttributeList)
                Marshal.FreeHGlobal(_startupInfo.lpAttributeList)
                _startupInfo.lpAttributeList = IntPtr.Zero
            End If

            ClosePseudoConsole()
            DisposePipes()

            _cancellationSource?.Dispose()
            _cancellationSource = Nothing
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _isDisposed Then
                Return
            End If

            [Stop]()
            _isDisposed = True
        End Sub
    End Class
End Namespace
