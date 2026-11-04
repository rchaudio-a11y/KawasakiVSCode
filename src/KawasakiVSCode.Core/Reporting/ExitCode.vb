' File:        ExitCode.vb
' Project:     KawasakiVSCode.Core
' Description: The exit codes khi ends with (contracts/cli.md, Exit codes; research R16).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Reporting

    ''' <summary>The exit codes khi ends with (contracts/cli.md, research R16).</summary>
    Public Enum ExitCode

        ''' <summary>Success, or a terminal session the person ended.</summary>
        Success = 0

        ''' <summary>The controller reported an error or refusal, or asked a question the person gave no answer to.</summary>
        ControllerError = 1

        ''' <summary>khi refused before connecting.</summary>
        RefusedBeforeConnecting = 2

        ''' <summary>A wait timed out.</summary>
        TimedOut = 3

        ''' <summary>The connection failed, was refused, or was closed by the controller.</summary>
        ConnectionFailed = 4

        ''' <summary>A local file step failed, such as replacing the target.</summary>
        LocalFileFailed = 5

    End Enum

End Namespace
