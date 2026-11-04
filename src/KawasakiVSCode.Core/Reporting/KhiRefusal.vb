' File:        KhiRefusal.vb
' Project:     KawasakiVSCode.Core
' Description: A refusal or failure khi reports in its own words with an exit code; KhiApp.Run prints
'              the message on stderr and exits with the code.
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Reporting

    ''' <summary>
    ''' A refusal or failure khi reports in its own words, with the exit code it ends with. KhiApp.Run prints
    ''' the message on stderr and exits with the code.
    ''' </summary>
    Public NotInheritable Class KhiRefusal
        Inherits Exception

        ''' <summary>Makes a refusal.</summary>
        ''' <param name="exitCode">The exit code khi ends with.</param>
        ''' <param name="message">The message khi prints on stderr, beginning "khi: ".</param>
        Public Sub New(exitCode As ExitCode, message As String)
            MyBase.New(message)
            Me.ExitCode = exitCode
        End Sub

        ''' <summary>The exit code khi ends with.</summary>
        Public ReadOnly Property ExitCode As ExitCode

    End Class

End Namespace
