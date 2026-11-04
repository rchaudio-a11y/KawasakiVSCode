' File:        ResultLine.vb
' Project:     KawasakiVSCode.Core
' Description: Formats the one result line khi load and khi save end with (contracts/cli.md, FR-021,
'              FR-028).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Globalization

Namespace Reporting

    ''' <summary>The result line, the last line on stdout of khi load and khi save (contracts/cli.md).</summary>
    Public NotInheritable Class ResultLine

        Private Sub New()
        End Sub

        ''' <summary>Formats the result line for an outcome.</summary>
        ''' <param name="outcome">How the run ended.</param>
        ''' <returns>The line, without a line ending.</returns>
        Public Shared Function Format(outcome As TransferOutcome) As String
            Dim programs As String = If(outcome.Programs.Count = 0, "none", String.Join(", ", outcome.Programs))
            Dim bytes As String = outcome.Bytes.ToString(CultureInfo.InvariantCulture) & " bytes"
            If outcome.Bytes <> outcome.FileSize Then
                bytes = "sent " & outcome.Bytes.ToString(CultureInfo.InvariantCulture) & " of " &
                        outcome.FileSize.ToString(CultureInfo.InvariantCulture) & " bytes"
            End If
            Dim line As String = "khi " & outcome.Verb & " " & If(outcome.Succeeded, "ok", "failed") &
                                 ": robot " & outcome.Robot &
                                 ", file """ & outcome.FilePath & """, " &
                                 bytes & ", programs " & programs
            Dim question As QuestionAnswer = outcome.Question
            If question IsNot Nothing Then
                line &= "; completed with " & question.ErrorCount.ToString(CultureInfo.InvariantCulture) &
                        If(question.ErrorCount = 1, " error", " errors") &
                        "; answered " & question.Answer
                If question.Label IsNot Nothing Then line &= " (" & question.Label & ")"
                If question.CopyDiffers Then line &= "; the controller's copy differs from the file"
            End If
            Return line
        End Function

    End Class

End Namespace
