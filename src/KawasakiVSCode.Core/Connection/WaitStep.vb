' File:        WaitStep.vb
' Project:     KawasakiVSCode.Core
' Description: The steps khi waits on the controller in, each with its own timeout (Article X,
'              data-model.md, Timeouts and WaitStep).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Connection

    ''' <summary>The steps khi waits on the controller in; each has its own timeout (Article X, data-model.md).</summary>
    Public Enum WaitStep

        ''' <summary>Connecting to the robot's address and port.</summary>
        Connect

        ''' <summary>The telnet negotiation (capture (a)).</summary>
        Negotiation

        ''' <summary>The login prompt (capture (a)).</summary>
        LoginPrompt

        ''' <summary>The prompt after login (capture (a)).</summary>
        LoginReply

        ''' <summary>The controller's reply to a command khi sent.</summary>
        CommandReply

        ''' <summary>Transfer data, during a LOAD or SAVE.</summary>
        TransferData

        ''' <summary>The end of a transfer.</summary>
        TransferEnd

        ''' <summary>A SAVE or LOAD the person typed in khi term.</summary>
        TypedTransfer

    End Enum

End Namespace
