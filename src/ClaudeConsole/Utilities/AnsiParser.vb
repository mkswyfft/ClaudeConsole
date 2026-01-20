Option Strict On
Option Explicit On

Imports System.Text.RegularExpressions
Imports System.Windows.Documents
Imports System.Windows.Media

Namespace Utilities
    ''' <summary>
    ''' Parses ANSI escape codes and converts them to WPF formatted text.
    ''' </summary>
    Public Class AnsiParser
        ' ANSI escape code pattern: ESC[...m
        Private Shared ReadOnly AnsiPattern As New Regex("\x1B\[([0-9;]*)m", RegexOptions.Compiled)

        ' Standard ANSI colors
        Private Shared ReadOnly AnsiColors As Brush() = {
            Brushes.Black,      ' 0 - Black
            Brushes.DarkRed,    ' 1 - Red
            Brushes.DarkGreen,  ' 2 - Green
            Brushes.DarkGoldenrod, ' 3 - Yellow
            Brushes.DarkBlue,   ' 4 - Blue
            Brushes.DarkMagenta, ' 5 - Magenta
            Brushes.DarkCyan,   ' 6 - Cyan
            Brushes.Gray        ' 7 - White
        }

        ' Bright ANSI colors
        Private Shared ReadOnly BrightAnsiColors As Brush() = {
            Brushes.DimGray,    ' 0 - Bright Black (Gray)
            Brushes.Red,        ' 1 - Bright Red
            Brushes.LimeGreen,  ' 2 - Bright Green
            Brushes.Yellow,     ' 3 - Bright Yellow
            Brushes.DodgerBlue, ' 4 - Bright Blue
            Brushes.Magenta,    ' 5 - Bright Magenta
            Brushes.Cyan,       ' 6 - Bright Cyan
            Brushes.White       ' 7 - Bright White
        }

        ''' <summary>
        ''' Represents a text segment with formatting.
        ''' </summary>
        Public Class TextSegment
            Public Property Text As String
            Public Property Foreground As Brush
            Public Property IsBold As Boolean
        End Class

        ''' <summary>
        ''' Parses text containing ANSI codes into formatted segments.
        ''' </summary>
        ''' <param name="input">The text with ANSI codes.</param>
        ''' <returns>A list of text segments with formatting info.</returns>
        Public Shared Function Parse(input As String) As List(Of TextSegment)
            Dim segments As New List(Of TextSegment)

            If String.IsNullOrEmpty(input) Then
                Return segments
            End If

            Dim currentForeground As Brush = Brushes.LightGray
            Dim isBold As Boolean = False
            Dim lastIndex As Integer = 0

            For Each match As Match In AnsiPattern.Matches(input)
                ' Add text before this escape code
                If match.Index > lastIndex Then
                    Dim text As String = input.Substring(lastIndex, match.Index - lastIndex)
                    If Not String.IsNullOrEmpty(text) Then
                        segments.Add(New TextSegment() With {
                            .Text = text,
                            .Foreground = currentForeground,
                            .IsBold = isBold
                        })
                    End If
                End If

                ' Parse the escape code
                Dim codes As String = match.Groups(1).Value
                If Not String.IsNullOrEmpty(codes) Then
                    For Each codeStr As String In codes.Split(";"c)
                        Dim code As Integer
                        If Integer.TryParse(codeStr, code) Then
                            Select Case code
                                Case 0 ' Reset
                                    currentForeground = Brushes.LightGray
                                    isBold = False
                                Case 1 ' Bold
                                    isBold = True
                                Case 22 ' Normal intensity
                                    isBold = False
                                Case 30 To 37 ' Standard foreground colors
                                    Dim colorIndex As Integer = code - 30
                                    currentForeground = If(isBold, BrightAnsiColors(colorIndex), AnsiColors(colorIndex))
                                Case 90 To 97 ' Bright foreground colors
                                    Dim colorIndex As Integer = code - 90
                                    currentForeground = BrightAnsiColors(colorIndex)
                                Case 39 ' Default foreground
                                    currentForeground = Brushes.LightGray
                            End Select
                        End If
                    Next
                End If

                lastIndex = match.Index + match.Length
            Next

            ' Add remaining text after last escape code
            If lastIndex < input.Length Then
                Dim text As String = input.Substring(lastIndex)
                If Not String.IsNullOrEmpty(text) Then
                    segments.Add(New TextSegment() With {
                        .Text = text,
                        .Foreground = currentForeground,
                        .IsBold = isBold
                    })
                End If
            End If

            Return segments
        End Function

        ''' <summary>
        ''' Creates WPF Run elements from text segments.
        ''' </summary>
        ''' <param name="segments">The parsed text segments.</param>
        ''' <returns>A list of Run elements for adding to a FlowDocument.</returns>
        Public Shared Function CreateRuns(segments As List(Of TextSegment)) As List(Of Run)
            Dim runs As New List(Of Run)

            For Each segment In segments
                Dim run As New Run(segment.Text) With {
                    .Foreground = segment.Foreground
                }
                If segment.IsBold Then
                    run.FontWeight = FontWeights.Bold
                End If
                runs.Add(run)
            Next

            Return runs
        End Function

        ''' <summary>
        ''' Strips all ANSI codes from text.
        ''' </summary>
        ''' <param name="input">The text with ANSI codes.</param>
        ''' <returns>Plain text without ANSI codes.</returns>
        Public Shared Function StripAnsiCodes(input As String) As String
            If String.IsNullOrEmpty(input) Then
                Return input
            End If
            Return AnsiPattern.Replace(input, String.Empty)
        End Function
    End Class
End Namespace
