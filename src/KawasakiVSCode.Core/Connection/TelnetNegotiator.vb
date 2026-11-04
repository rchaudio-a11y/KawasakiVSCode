' File:        TelnetNegotiator.vb
' Project:     KawasakiVSCode.Core
' Description: The telnet negotiation khi plays at connect, as capture (a) shows KRterm playing it
'              (Article IV).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Connection

    ''' <summary>
    ''' The telnet negotiation, byte for byte as capture (a) shows it (Article IV): the client offers the
    ''' terminal-type option first, ff fb 18 (record 1), and the controller answers ff fd 18 (record 2). KRterm
    ''' sends nothing in reply to that answer, and neither does khi. No other option is negotiated.
    ''' </summary>
    Public NotInheritable Class TelnetNegotiator

        Private Sub New()
        End Sub

        ''' <summary>What khi sends first: IAC WILL TERMINAL-TYPE, ff fb 18 (capture (a) record 1).</summary>
        Public Shared ReadOnly Property Offer As Byte()
            Get
                Return New Byte() {&HFF, &HFB, &H18}
            End Get
        End Property

        ''' <summary>The controller's answer khi waits for: IAC DO TERMINAL-TYPE, ff fd 18 (capture (a) record 2).</summary>
        Public Shared ReadOnly Property Answer As Byte()
            Get
                Return New Byte() {&HFF, &HFD, &H18}
            End Get
        End Property

    End Class

End Namespace
