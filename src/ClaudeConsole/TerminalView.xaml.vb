Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text.Json
Imports ClaudeConsole.ViewModels
Imports Microsoft.Web.WebView2.Core

Partial Class TerminalView

    Private _isWebViewReady As Boolean = False
    Private _pendingOutput As New List(Of String)
    Private _currentVm As TabViewModel

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Async Sub TerminalView_Loaded(sender As Object, e As RoutedEventArgs)
        Try
            ' Initialize WebView2 if not already done
            If WebView.CoreWebView2 Is Nothing Then
                Await WebView.EnsureCoreWebView2Async()
                AddHandler WebView.CoreWebView2.WebMessageReceived, AddressOf OnWebMessageReceived

                ' Load the terminal HTML
                Dim htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "terminal.html")
                If File.Exists(htmlPath) Then
                    Dim fileUri = New Uri(htmlPath).AbsoluteUri
                    WebView.CoreWebView2.Navigate(fileUri)
                Else
                    WebView.CoreWebView2.NavigateToString(GetEmbeddedTerminalHtml())
                End If
            End If

            ' Connect to ViewModel events
            _currentVm = TryCast(DataContext, TabViewModel)
            If _currentVm IsNot Nothing Then
                AddHandler _currentVm.TerminalOutputReceived, AddressOf OnTerminalOutput
                AddHandler _currentVm.TerminalExited, AddressOf OnTerminalExited
            End If

        Catch ex As Exception
            System.Windows.MessageBox.Show($"Failed to initialize terminal: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub TerminalView_Unloaded(sender As Object, e As RoutedEventArgs)
        ' Disconnect from ViewModel events (but don't stop the terminal!)
        If _currentVm IsNot Nothing Then
            RemoveHandler _currentVm.TerminalOutputReceived, AddressOf OnTerminalOutput
            RemoveHandler _currentVm.TerminalExited, AddressOf OnTerminalExited
            _currentVm = Nothing
        End If
    End Sub

    Private Sub OnWebMessageReceived(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Try
            Dim json = e.WebMessageAsJson
            Dim doc = JsonDocument.Parse(json)
            Dim root = doc.RootElement

            Dim msgType = root.GetProperty("type").GetString()

            Select Case msgType
                Case "input"
                    ' User typed something - send to TabViewModel's terminal
                    Dim data = root.GetProperty("data").GetString()
                    Dim vm = TryCast(DataContext, TabViewModel)
                    vm?.SendTerminalInput(data)

                Case "resize"
                    ' Terminal resized - update ConPTY size
                    Dim cols = root.GetProperty("cols").GetInt16()
                    Dim rows = root.GetProperty("rows").GetInt16()
                    Dim vm = TryCast(DataContext, TabViewModel)
                    vm?.ResizeTerminal(cols, rows)

                Case "ready"
                    ' xterm.js is ready
                    _isWebViewReady = True

                    ' Send any pending output
                    For Each output In _pendingOutput
                        SendOutputToXterm(output)
                    Next
                    _pendingOutput.Clear()

                    ' Start terminal if not already started
                    Dim vm = TryCast(DataContext, TabViewModel)
                    If vm IsNot Nothing AndAlso Not vm.IsTerminalStarted Then
                        Dim cols As Short = 120
                        Dim rows As Short = 30
                        Dim colsElement As JsonElement
                        Dim rowsElement As JsonElement
                        If root.TryGetProperty("cols", colsElement) Then
                            cols = colsElement.GetInt16()
                        End If
                        If root.TryGetProperty("rows", rowsElement) Then
                            rows = rowsElement.GetInt16()
                        End If
                        If Not vm.StartTerminal(cols, rows) Then
                            SendOutputToXterm($"Failed to start terminal: {vm.Terminal?.LastError}{vbCrLf}")
                        End If
                    End If
            End Select

        Catch ex As Exception
            ' Ignore parsing errors
        End Try
    End Sub

    Private Sub OnTerminalOutput(sender As Object, output As String)
        ' Send output to xterm.js
        Dispatcher.BeginInvoke(Sub()
            If _isWebViewReady Then
                SendOutputToXterm(output)
            Else
                _pendingOutput.Add(output)
            End If
        End Sub)
    End Sub

    Private Sub OnTerminalExited(sender As Object, exitCode As Integer)
        Dispatcher.BeginInvoke(Sub()
            SendOutputToXterm($"{vbCrLf}[Process exited with code {exitCode}]{vbCrLf}")
        End Sub)
    End Sub

    Private Sub SendOutputToXterm(output As String)
        If WebView?.CoreWebView2 Is Nothing Then Return

        Try
            ' Escape the output for JavaScript
            Dim escaped = JsonSerializer.Serialize(output)
            WebView.CoreWebView2.ExecuteScriptAsync($"receiveOutput({escaped})")
        Catch ex As Exception
            ' Ignore errors
        End Try
    End Sub

    Private Function GetEmbeddedTerminalHtml() As String
        Return "<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Terminal</title>
    <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/xterm@5.3.0/css/xterm.css"" />
    <style>
        html, body { margin: 0; padding: 0; height: 100%; overflow: hidden; background-color: #1e1e1e; }
        #terminal { height: 100%; width: 100%; }
    </style>
</head>
<body>
    <div id=""terminal""></div>
    <script src=""https://cdn.jsdelivr.net/npm/xterm@5.3.0/lib/xterm.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/xterm-addon-fit@0.8.0/lib/xterm-addon-fit.js""></script>
    <script>
        const term = new Terminal({
            cursorBlink: true, fontSize: 14, fontFamily: 'Consolas, monospace',
            theme: { background: '#1e1e1e', foreground: '#cccccc', cursor: '#ffffff' }
        });
        const fitAddon = new FitAddon.FitAddon();
        term.loadAddon(fitAddon);
        term.open(document.getElementById('terminal'));
        fitAddon.fit();
        window.addEventListener('resize', () => { fitAddon.fit();
            if(window.chrome?.webview) window.chrome.webview.postMessage({type:'resize',cols:term.cols,rows:term.rows}); });
        setTimeout(() => { fitAddon.fit();
            if(window.chrome?.webview) window.chrome.webview.postMessage({type:'ready',cols:term.cols,rows:term.rows}); }, 100);
        term.onData(data => { if(window.chrome?.webview) window.chrome.webview.postMessage({type:'input',data:data}); });
        window.receiveOutput = function(data) { term.write(data); };
        term.focus();
    </script>
</body>
</html>"
    End Function

End Class
