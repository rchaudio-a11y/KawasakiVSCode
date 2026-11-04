' File:        TransferOutcome.vb
' Project:     KawasakiVSCode.Core
' Description: How a run of khi load or khi save ended, from which the result line is built
'              (data-model.md, TransferOutcome and ResultLine).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Reporting

    ''' <summary>
    ''' How a run of khi load or khi save ended (data-model.md). The SaveLines field the data model lists
    ''' comes with khi save (User Story 4); the result line does not print it.
    ''' </summary>
    Public NotInheritable Class TransferOutcome

        ''' <summary>Makes an outcome.</summary>
        ''' <param name="verb">load or save.</param>
        ''' <param name="succeeded">True for ok, False for failed.</param>
        ''' <param name="robot">The robot entry's name.</param>
        ''' <param name="filePath">The path as the person gave it.</param>
        ''' <param name="bytes">File content only, sent or written; never protocol traffic.</param>
        ''' <param name="programs">The names the controller reported, in its order; empty when it reported none.</param>
        ''' <param name="question">The question part after a load question, or Nothing.</param>
        ''' <param name="exitCode">The exit code khi ends with.</param>
        Public Sub New(verb As String, succeeded As Boolean, robot As String, filePath As String, bytes As Long,
                       programs As IReadOnlyList(Of String), question As QuestionAnswer, exitCode As ExitCode)
            Me.Verb = verb
            Me.Succeeded = succeeded
            Me.Robot = robot
            Me.FilePath = filePath
            Me.Bytes = bytes
            Me.Programs = programs
            Me.Question = question
            Me.ExitCode = exitCode
        End Sub

        ''' <summary>load or save.</summary>
        Public ReadOnly Property Verb As String

        ''' <summary>True for ok, False for failed.</summary>
        Public ReadOnly Property Succeeded As Boolean

        ''' <summary>The robot entry's name.</summary>
        Public ReadOnly Property Robot As String

        ''' <summary>The path as the person gave it.</summary>
        Public ReadOnly Property FilePath As String

        ''' <summary>File content only, sent or written.</summary>
        Public ReadOnly Property Bytes As Long

        ''' <summary>The names the controller reported, in its order.</summary>
        Public ReadOnly Property Programs As IReadOnlyList(Of String)

        ''' <summary>The question part, or Nothing when the controller asked none.</summary>
        Public ReadOnly Property Question As QuestionAnswer

        ''' <summary>The exit code khi ends with.</summary>
        Public ReadOnly Property ExitCode As ExitCode

    End Class

End Namespace
