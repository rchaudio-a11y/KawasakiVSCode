' File:        RunResult.vb
' Project:     KawasakiVSCode.Tests
' Description: What one in-process run of KhiApp.Run gave: its exit code, its stdout bytes and its
'              stderr text (research R12).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Route

    ''' <summary>What one run of KhiApp.Run gave (research R12).</summary>
    Public NotInheritable Class RunResult

        ''' <summary>Makes a result.</summary>
        ''' <param name="exitCode">The exit code Run returned.</param>
        ''' <param name="output">The bytes written to stdout.</param>
        ''' <param name="errors">The text written to stderr.</param>
        Public Sub New(exitCode As Integer, output As Byte(), errors As String)
            Me.ExitCode = exitCode
            Me.Output = output
            Me.Errors = errors
        End Sub

        ''' <summary>The exit code.</summary>
        Public ReadOnly Property ExitCode As Integer

        ''' <summary>The bytes written to stdout.</summary>
        Public ReadOnly Property Output As Byte()

        ''' <summary>The text written to stderr.</summary>
        Public ReadOnly Property Errors As String

    End Class

End Namespace
