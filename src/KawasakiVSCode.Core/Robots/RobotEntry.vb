' File:        RobotEntry.vb
' Project:     KawasakiVSCode.Core
' Description: One controller in khi.robots.json: its name, address, port and simulator flag
'              (contracts/robots-file.md, Article IX).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Robots

    ''' <summary>One controller in khi.robots.json (contracts/robots-file.md). Addresses and ports come only from that file (Article IX).</summary>
    Public NotInheritable Class RobotEntry

        ''' <summary>Makes an entry; RobotsFile.Load has already checked every field.</summary>
        ''' <param name="name">The name --robot and default match, ignoring case.</param>
        ''' <param name="address">A host name or IP address.</param>
        ''' <param name="port">A port from 1 to 65535.</param>
        ''' <param name="simulator">Whether the entry is a simulator; no behaviour depends on it in this feature.</param>
        Public Sub New(name As String, address As String, port As Integer, simulator As Boolean)
            Me.Name = name
            Me.Address = address
            Me.Port = port
            Me.Simulator = simulator
        End Sub

        ''' <summary>The name, as the file writes it.</summary>
        Public ReadOnly Property Name As String

        ''' <summary>A host name or IP address.</summary>
        Public ReadOnly Property Address As String

        ''' <summary>The port, from 1 to 65535.</summary>
        Public ReadOnly Property Port As Integer

        ''' <summary>Whether the entry is a simulator. Read and type-checked; no behaviour depends on it in this feature.</summary>
        Public ReadOnly Property Simulator As Boolean

    End Class

End Namespace
