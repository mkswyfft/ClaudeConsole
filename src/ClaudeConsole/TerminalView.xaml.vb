Option Strict On
Option Explicit On

Imports System.Windows.Input
Imports ClaudeConsole.ViewModels

Partial Class TerminalView

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub OnInputKeyDown(sender As Object, e As KeyEventArgs)
        If e.Key = Key.Enter Then
            Dim vm = TryCast(DataContext, TabViewModel)
            If vm IsNot Nothing AndAlso vm.SendCommand.CanExecute(Nothing) Then
                vm.SendCommand.Execute(Nothing)
                e.Handled = True
            End If
        ElseIf e.Key = Key.Up Then
            ' TODO: Command history navigation
            e.Handled = True
        ElseIf e.Key = Key.Down Then
            ' TODO: Command history navigation
            e.Handled = True
        End If
    End Sub

    Private Sub TerminalView_Loaded(sender As Object, e As RoutedEventArgs)
        ' Focus the input box when the control is loaded
        InputTextBox.Focus()
    End Sub
End Class
