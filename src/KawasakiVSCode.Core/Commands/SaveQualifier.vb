' File:        SaveQualifier.vb
' Project:     KawasakiVSCode.Core
' Description: The SAVE qualifiers khi supports, and the one door that turns --qualifier into a SAVE
'              command word (FR-024, FR-025, research R15).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports KawasakiVSCode.Core.Reporting

Namespace Commands

    ''' <summary>
    ''' A SAVE qualifier khi supports. The supported forms are those the manual lists and a capture backs:
    ''' none (capture (g)), P (capture (d)) and FULL (capture (e)) (FR-024, FR-025). Parse is the one door
    ''' (research R15).
    ''' </summary>
    Public NotInheritable Class SaveQualifier

        Private Const Supported As String = "khi save takes --qualifier P, --qualifier FULL, or no qualifier"

        Private Shared ReadOnly ListedWithoutCapture As String() = {"L", "R", "S", "A", "SYS", "ROB", "ELOG", "OPLOG", "ALLLOG", "STG"}

        Private Sub New(commandWord As String)
            Me.CommandWord = commandWord
        End Sub

        ''' <summary>The command word the controller is sent: SAVE, SAVE/P or SAVE/FULL.</summary>
        Public ReadOnly Property CommandWord As String

        ''' <summary>
        ''' Turns the --qualifier value into a qualifier. Written as the manual writes it, without the slash
        ''' and in capitals.
        ''' </summary>
        ''' <param name="qualifier">The --qualifier value, or Nothing when the option was left out.</param>
        ''' <returns>The qualifier.</returns>
        ''' <exception cref="KhiRefusal">The qualifier is one the manual lists with no capture behind it, or
        ''' one khi does not know; exit 2, before connecting.</exception>
        Public Shared Function Parse(qualifier As String) As SaveQualifier
            If qualifier Is Nothing Then Return New SaveQualifier("SAVE")
            If qualifier = "P" Then Return New SaveQualifier("SAVE/P")
            If qualifier = "FULL" Then Return New SaveQualifier("SAVE/FULL")
            If Array.IndexOf(ListedWithoutCapture, qualifier) >= 0 Then
                Throw New KhiRefusal(ExitCode.RefusedBeforeConnecting,
                                     "khi: qualifier """ & qualifier & """ is not supported; " & Supported)
            End If
            Throw New KhiRefusal(ExitCode.RefusedBeforeConnecting,
                                 "khi: unknown qualifier """ & qualifier & """; " & Supported)
        End Function

    End Class

End Namespace
