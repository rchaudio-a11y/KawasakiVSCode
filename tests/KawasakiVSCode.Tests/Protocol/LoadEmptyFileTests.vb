' File:        LoadEmptyFileTests.vb
' Project:     KawasakiVSCode.Tests
' Description: U5, an empty file to load: khi sends what capture (q) shows KRterm sending, and the
'              controller's completion line decides the exit code (FR-016, FR-017, T057).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Tests.Route
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Protocol

    ''' <summary>
    ''' Capture (q), files/capq.as of 0 bytes (its note): at the first request a C block holding no file bytes
    ''' (record 15), at the next the end-of-file block (record 17), then File load completed. (0 errors).
    ''' </summary>
    <TestClass>
    Public Class LoadEmptyFileTests

        Private Const Capture As String = "q-load-empty"

        ''' <summary>khi sends an empty block, then the end-of-file block; the result line counts 0 bytes and names no program; exit 0, as (0 errors) says.</summary>
        <TestMethod, Capture("q-load-empty")>
        Public Sub AnEmptyFileGoesAsCaptureQShows()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                Dim copied As String = workspace.CopyFromCapture(Capture, "capq.as")
                Assert.AreEqual(0L, New FileInfo(copied).Length)
                Dim result As RunResult = workspace.Run(New Timeouts(TimeSpan.FromSeconds(2)), input, "load", "capq.as")
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText(Capture, New RecordRange(10, 20)),
                                                            "khi load ok: robot sim, file ""capq.as"", 0 bytes, programs none"), result.Output)
                Assert.AreEqual(0, result.ExitCode, result.Errors)
            End Using
        End Sub

    End Class

End Namespace
