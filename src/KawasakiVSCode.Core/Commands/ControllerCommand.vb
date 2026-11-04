' File:        ControllerCommand.vb
' Project:     KawasakiVSCode.Core
' Description: The one door for the commands khi originates: the login, a LOAD and a SAVE, each with its
'              text taken from its capture (Article III, data-model.md, research R15).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Reporting
Imports KawasakiVSCode.Core.Session

Namespace Commands

    ''' <summary>
    ''' A command khi originates, and the only kind ControllerConnection.Send takes (Article III, research R15).
    ''' The forms come from the captures: the login from capture (a), load from captures (b) and (i), SAVE from
    ''' captures (d), (e) and (g). AS Language Reference Manual 90209-1022 §2.6.2 and §5.3 describe LOAD and
    ''' SAVE over the PC connection.
    ''' </summary>
    Public NotInheritable Class ControllerCommand

        Private Sub New(kind As ControllerCommandKind, line As String)
            Me.Kind = kind
            Me.Line = line
        End Sub

        ''' <summary>The login: the constant as (LoginName).</summary>
        Public Shared ReadOnly Property Login As New ControllerCommand(ControllerCommandKind.Login, LoginName.Value)

        ''' <summary>A LOAD of a file: load and the file's bare name, cased as captured (FR-027).</summary>
        ''' <param name="filePath">The file as the person gave it; only its name is sent.</param>
        ''' <returns>The command.</returns>
        ''' <exception cref="KhiRefusal">The bare name holds a character outside ASCII (FR-044); exit 2, before connecting.</exception>
        Public Shared Function Load(filePath As String) As ControllerCommand
            Return New ControllerCommand(ControllerCommandKind.Load, "load " & BareName(filePath))
        End Function

        ''' <summary>A SAVE into a file: the qualifier's command word and the file's bare name.</summary>
        ''' <param name="qualifier">The qualifier SaveQualifier.Parse gave.</param>
        ''' <param name="filePath">The file as the person gave it; only its name is sent.</param>
        ''' <returns>The command.</returns>
        ''' <exception cref="KhiRefusal">The bare name holds a character outside ASCII (FR-044); exit 2, before connecting.</exception>
        Public Shared Function Save(qualifier As SaveQualifier, filePath As String) As ControllerCommand
            Return New ControllerCommand(ControllerCommandKind.Save, qualifier.CommandWord & " " & BareName(filePath))
        End Function

        ''' <summary>The bare name a command sends, refused when it holds a character outside ASCII: the line goes to the controller as ASCII, and no capture shows a name outside it (FR-044).</summary>
        Private Shared Function BareName(filePath As String) As String
            Dim name As String = Path.GetFileName(filePath)
            For Each character As Char In name
                If Convert.ToInt32(character) > 127 Then
                    Throw New KhiRefusal(ExitCode.RefusedBeforeConnecting, "khi: " & filePath & " has a name outside ASCII; it cannot be sent")
                End If
            Next
            Return name
        End Function

        ''' <summary>The kind of command.</summary>
        Public ReadOnly Property Kind As ControllerCommandKind

        ''' <summary>The command's text, without a line ending.</summary>
        Public ReadOnly Property Line As String

    End Class

End Namespace
