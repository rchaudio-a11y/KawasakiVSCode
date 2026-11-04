' File:        RobotSelection.vb
' Project:     KawasakiVSCode.Core
' Description: Picks the robot a verb uses from --robot or the file's default; the one door
'              (FR-008, research R15).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports KawasakiVSCode.Core.Reporting

Namespace Robots

    ''' <summary>Picks the robot a verb uses (FR-008). Resolve is the one door (research R15).</summary>
    Public NotInheritable Class RobotSelection

        Private Sub New()
        End Sub

        ''' <summary>The entry --robot names, matched ignoring case; or the default when --robot is left out.</summary>
        ''' <param name="robots">The checked robots file.</param>
        ''' <param name="requested">The --robot value, or Nothing when the option was left out.</param>
        ''' <returns>The entry.</returns>
        ''' <exception cref="KhiRefusal">The name matches no entry, or there is neither --robot nor a default; exit 2, before connecting.</exception>
        Public Shared Function Resolve(robots As RobotsFile, requested As String) As RobotEntry
            Dim names As String = String.Join(", ", robots.Robots.Select(Function(entry As RobotEntry) entry.Name))
            Dim wanted As String = requested
            If wanted Is Nothing Then
                wanted = robots.DefaultName
                If wanted Is Nothing Then
                    Throw New KhiRefusal(ExitCode.RefusedBeforeConnecting, "khi: no --robot given and no default set; robots are: " & names)
                End If
            End If
            For Each entry As RobotEntry In robots.Robots
                If String.Equals(entry.Name, wanted, StringComparison.OrdinalIgnoreCase) Then Return entry
            Next
            Throw New KhiRefusal(ExitCode.RefusedBeforeConnecting, "khi: unknown robot " & wanted & "; robots are: " & names)
        End Function

    End Class

End Namespace
