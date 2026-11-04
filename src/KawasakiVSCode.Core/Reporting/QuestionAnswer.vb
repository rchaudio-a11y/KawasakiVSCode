' File:        QuestionAnswer.vb
' Project:     KawasakiVSCode.Core
' Description: What the result line says after a load question: the controller's error count, the
'              person's answer, its label, and whether the controller's copy differs (FR-021).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Reporting

    ''' <summary>The question part of a load's result line (FR-021, contracts/cli.md).</summary>
    Public NotInheritable Class QuestionAnswer

        ''' <summary>Makes the question part.</summary>
        ''' <param name="errorCount">The controller's own count, from "File load completed. (N errors)".</param>
        ''' <param name="answer">What the person typed, without Enter.</param>
        ''' <param name="label">The controller's label for that answer as its question printed it, or Nothing when the answer matches no choice.</param>
        ''' <param name="copyDiffers">True after the answer 0, which keeps the program with the step made a comment (capture (n)).</param>
        Public Sub New(errorCount As Integer, answer As String, label As String, copyDiffers As Boolean)
            Me.ErrorCount = errorCount
            Me.Answer = answer
            Me.Label = label
            Me.CopyDiffers = copyDiffers
        End Sub

        ''' <summary>The controller's error count.</summary>
        Public ReadOnly Property ErrorCount As Integer

        ''' <summary>What the person typed, without Enter.</summary>
        Public ReadOnly Property Answer As String

        ''' <summary>The controller's label for the answer, or Nothing when it matches no choice.</summary>
        Public ReadOnly Property Label As String

        ''' <summary>Whether the controller's copy differs from the file.</summary>
        Public ReadOnly Property CopyDiffers As Boolean

    End Class

End Namespace
