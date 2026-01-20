Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports Microsoft.Win32.SafeHandles

Namespace ConPty
    ''' <summary>
    ''' Native Windows API declarations for ConPTY (Pseudo Console).
    ''' </summary>
    Friend Module NativeApi

#Region "Constants"
        Friend Const PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE As Integer = &H20016
        Friend Const EXTENDED_STARTUPINFO_PRESENT As UInteger = &H80000UI
        Friend Const STD_OUTPUT_HANDLE As Integer = -11
        Friend Const ENABLE_VIRTUAL_TERMINAL_PROCESSING As UInteger = &H4UI
        Friend Const DISABLE_NEWLINE_AUTO_RETURN As UInteger = &H8UI
#End Region

#Region "Structures"
        <StructLayout(LayoutKind.Sequential)>
        Friend Structure COORD
            Public X As Short
            Public Y As Short
        End Structure

        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
        Friend Structure STARTUPINFO
            Public cb As Integer
            Public lpReserved As String
            Public lpDesktop As String
            Public lpTitle As String
            Public dwX As Integer
            Public dwY As Integer
            Public dwXSize As Integer
            Public dwYSize As Integer
            Public dwXCountChars As Integer
            Public dwYCountChars As Integer
            Public dwFillAttribute As Integer
            Public dwFlags As Integer
            Public wShowWindow As Short
            Public cbReserved2 As Short
            Public lpReserved2 As IntPtr
            Public hStdInput As IntPtr
            Public hStdOutput As IntPtr
            Public hStdError As IntPtr
        End Structure

        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
        Friend Structure STARTUPINFOEX
            Public StartupInfo As STARTUPINFO
            Public lpAttributeList As IntPtr
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Friend Structure PROCESS_INFORMATION
            Public hProcess As IntPtr
            Public hThread As IntPtr
            Public dwProcessId As Integer
            Public dwThreadId As Integer
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Friend Structure SECURITY_ATTRIBUTES
            Public nLength As Integer
            Public lpSecurityDescriptor As IntPtr
            Public bInheritHandle As Integer
        End Structure
#End Region

#Region "Pseudo Console API"
        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function CreatePseudoConsole(size As COORD, hInput As SafeFileHandle, hOutput As SafeFileHandle, dwFlags As UInteger, ByRef phPC As IntPtr) As Integer
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function ResizePseudoConsole(hPC As IntPtr, size As COORD) As Integer
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Sub ClosePseudoConsole(hPC As IntPtr)
        End Sub

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function CreatePipe(ByRef hReadPipe As IntPtr, ByRef hWritePipe As IntPtr, lpPipeAttributes As IntPtr, nSize As Integer) As Boolean
        End Function
#End Region

#Region "Process API"
        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function InitializeProcThreadAttributeList(lpAttributeList As IntPtr, dwAttributeCount As Integer, dwFlags As Integer, ByRef lpSize As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function UpdateProcThreadAttribute(lpAttributeList As IntPtr, dwFlags As UInteger, attribute As IntPtr, lpValue As IntPtr, cbSize As IntPtr, lpPreviousValue As IntPtr, lpReturnSize As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Sub DeleteProcThreadAttributeList(lpAttributeList As IntPtr)
        End Sub

        <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
        Friend Function CreateProcess(lpApplicationName As String, lpCommandLine As String, ByRef lpProcessAttributes As SECURITY_ATTRIBUTES, ByRef lpThreadAttributes As SECURITY_ATTRIBUTES, bInheritHandles As Boolean, dwCreationFlags As UInteger, lpEnvironment As IntPtr, lpCurrentDirectory As String, ByRef lpStartupInfo As STARTUPINFOEX, ByRef lpProcessInformation As PROCESS_INFORMATION) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function CloseHandle(hObject As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function WaitForSingleObject(hHandle As IntPtr, dwMilliseconds As UInteger) As UInteger
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function TerminateProcess(hProcess As IntPtr, uExitCode As UInteger) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function GetExitCodeProcess(hProcess As IntPtr, ByRef lpExitCode As UInteger) As Boolean
        End Function

        Friend Const WAIT_OBJECT_0 As UInteger = 0
        Friend Const WAIT_TIMEOUT As UInteger = &H102UI
        Friend Const INFINITE As UInteger = &HFFFFFFFFUI
#End Region

#Region "Console API"
        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function GetStdHandle(nStdHandle As Integer) As SafeFileHandle
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function SetConsoleMode(hConsoleHandle As SafeFileHandle, mode As UInteger) As Boolean
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Friend Function GetConsoleMode(handle As SafeFileHandle, ByRef mode As UInteger) As Boolean
        End Function
#End Region

    End Module
End Namespace
