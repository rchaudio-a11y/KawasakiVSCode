' File:        LoadReplacesTests.vb
' Project:     KawasakiVSCode.Tests
' Description: khi load of a program already in the controller's memory: on K-ROSET the LOAD replaces
'              it, and khi exits 0 naming the program the controller reported (User Story 2 scenario 4, T051).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Tests.Route
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Protocol

    ''' <summary>
    ''' Capture i-load-existing-changed, records 1 to 20 (its note): capi.as holds program capb, already in
    ''' memory; the LOAD runs as a first LOAD does, and the controller prints Program  capb().
    ''' </summary>
    <TestClass>
    Public Class LoadReplacesTests

        Private Const Capture As String = "i-load-existing-changed"

        ''' <summary>khi load capi.as exits 0, and the result line names capb, the program the controller reported.</summary>
        <TestMethod, Capture("i-load-existing-changed")>
        Public Sub AReloadReplacesTheProgram()
            Using standIn As ControllerStandIn = ControllerStandIn.PlaySlice(Capture, New RecordRange(1, 20)).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFromCapture(Capture, "capi.as")
                Dim result As RunResult = workspace.Run(New Timeouts(TimeSpan.FromSeconds(2)), input, "load", "capi.as")
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                Assert.AreEqual(0, result.ExitCode, result.Errors)
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText(Capture, New RecordRange(10, 20)),
                                                            "khi load ok: robot sim, file ""capi.as"", 121 bytes, programs capb"), result.Output)
            End Using
        End Sub

    End Class

End Namespace
