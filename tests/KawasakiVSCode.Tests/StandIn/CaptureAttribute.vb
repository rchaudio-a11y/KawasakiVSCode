' File:        CaptureAttribute.vb
' Project:     KawasakiVSCode.Tests
' Description: Names the capture a protocol test replays (INV-3, contracts/capture-format.md).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace StandIn

    ''' <summary>Names the capture a protocol test replays (INV-3). Every protocol test carries one.</summary>
    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False)>
    Public NotInheritable Class CaptureAttribute
        Inherits Attribute

        ''' <summary>Names the capture.</summary>
        ''' <param name="folder">The capture's folder name under Captures/, such as a-connect-login.</param>
        Public Sub New(folder As String)
            Me.Folder = folder
        End Sub

        ''' <summary>The capture's folder name under Captures/.</summary>
        Public ReadOnly Property Folder As String

    End Class

End Namespace
