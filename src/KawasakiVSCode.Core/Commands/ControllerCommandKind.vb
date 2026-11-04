' File:        ControllerCommandKind.vb
' Project:     KawasakiVSCode.Core
' Description: The kinds of command khi originates (data-model.md, ControllerCommand; Article III).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Commands

    ''' <summary>The kinds of command khi originates (Article III); there are no others.</summary>
    Public Enum ControllerCommandKind

        ''' <summary>The login, at the login prompt.</summary>
        Login

        ''' <summary>A LOAD, for khi load.</summary>
        Load

        ''' <summary>A SAVE, for khi save.</summary>
        Save

    End Enum

End Namespace
