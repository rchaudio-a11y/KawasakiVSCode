' File:        LoadInUseTests.vb
' Project:     KawasakiVSCode.Tests
' Description: khi load of a program in use: the controller's refusal printed verbatim, a file-reference
'              line for it, the result line, and exit 1 (User Story 2 scenario 5, FR-017, FR-018, T052).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Tests.Route
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Protocol

    ''' <summary>
    ''' Capture (f)'s first LOAD, as a slice of records 1 to 8 and 19 to 30 (D4): the controller's C request
    ''' after the file (record 26) carries Program  capb() and Cannot execute because program already in use.,
    ''' and the load ends File load completed. (1 errors) (the capture's note, observation 4).
    ''' </summary>
    <TestClass>
    Public Class LoadInUseTests

        Private Const Capture As String = "f-typed-save-load"

        ''' <summary>
        ''' The refusal is printed verbatim with the rest of the controller's text, khi adds capb.as: and the
        ''' line, and the run ends with the result line and exit 1.
        ''' </summary>
        <TestMethod, Capture("f-typed-save-load")>
        Public Sub AProgramInUseIsReportedInTheControllersWords()
            Using standIn As ControllerStandIn = ControllerStandIn.PlaySlice(Capture, New RecordRange(1, 8), New RecordRange(19, 30)).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFromCapture("b-load-small", "capb.as")
                Dim result As RunResult = workspace.Run(New Timeouts(TimeSpan.FromSeconds(2)), input, "load", "capb.as")
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText(Capture, New RecordRange(20, 30)),
                                                            "capb.as: Cannot execute because program already in use.",
                                                            "khi load failed: robot sim, file ""capb.as"", 103 bytes, programs capb"), result.Output)
                Assert.AreEqual(1, result.ExitCode, result.Errors)
            End Using
        End Sub

    End Class

End Namespace
