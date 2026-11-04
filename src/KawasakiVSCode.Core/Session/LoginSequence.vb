' File:        LoginSequence.vb
' Project:     KawasakiVSCode.Core
' Description: Connect-time negotiation and login, as capture (a) shows: the telnet offer, the login
'              prompt, the login name, and the prompt after login (Article IV, Article IX).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Text
Imports KawasakiVSCode.Core.Commands
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Core.Reporting

Namespace Session

    ''' <summary>
    ''' The login, as capture (a) shows it: the telnet negotiation (records 1 and 2), the login prompt
    ''' "login: " (record 2), the login name and Enter (records 3, 5 and 7), and the banner ending at 0d 0a
    ''' and the > prompt (record 8). Every wait is its own step with its own timeout (Article X).
    ''' </summary>
    Public NotInheritable Class LoginSequence

        Private Sub New()
        End Sub

        ''' <summary>The login prompt, 6c 6f 67 69 6e 3a 20, as capture (a) record 2 shows it.</summary>
        Public Shared ReadOnly Property LoginPrompt As Byte()
            Get
                Return Encoding.ASCII.GetBytes("login: ")
            End Get
        End Property

        ''' <summary>Negotiates and logs in on a fresh connection, returning at the prompt after login.</summary>
        ''' <param name="connection">The connection, just opened.</param>
        ''' <returns>Every byte the controller sent, up to and including the prompt.</returns>
        ''' <exception cref="KhiRefusal">A wait timed out (exit 3), naming its step, or the controller closed the connection (exit 4).</exception>
        Public Shared Function Run(connection As ControllerConnection) As Byte()
            Dim received As New List(Of Byte)()
            received.AddRange(connection.Negotiate())
            received.AddRange(connection.ReadUntil(WaitStep.LoginPrompt, Nothing, LoginPrompt))
            received.AddRange(connection.Send(ControllerCommand.Login))
            Return received.ToArray()
        End Function

    End Class

End Namespace
