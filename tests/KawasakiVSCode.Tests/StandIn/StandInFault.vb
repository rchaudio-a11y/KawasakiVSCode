' File:        StandInFault.vb
' Project:     KawasakiVSCode.Tests
' Description: How the stand-in cuts a capture short at a given record (contracts/capture-format.md,
'              Faults).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace StandIn

    ''' <summary>How the stand-in cuts a capture short (contracts/capture-format.md, Faults). A fault only cuts a capture short.</summary>
    Public Enum StandInFault

        ''' <summary>Sends nothing more and holds the connection open (INV-5, INV-6, INV-10).</summary>
        Silent

        ''' <summary>Closes the connection (INV-4).</summary>
        Close

    End Enum

End Namespace
