' File:        LargeLoadTests.vb
' Project:     KawasakiVSCode.Tests
' Description: khi load of files longer than one block: 512 file bytes per C block, one block per
'              controller request, a short or empty block last, then the end-of-file block (FR-016,
'              captures (l) and (s), T049).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Text
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Tests.Route
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Protocol

    ''' <summary>
    ''' The block rule captures (l) and (s) show (their notes): each controller C request gets the next
    ''' block of up to 512 file bytes; a block shorter than 512, an empty one included, is the file's last,
    ''' and the request after it gets the end-of-file block.
    ''' </summary>
    <TestClass>
    Public Class LargeLoadTests

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(20)

        Private Shared ReadOnly Waits As New Timeouts(TimeSpan.FromSeconds(2))

        ''' <summary>
        ''' capl.as, 61,441 bytes, goes as 120 C blocks of 512 file bytes and one of 1 byte, then the
        ''' end-of-file block; the 121 payloads, the odd records 15 to 255, are compared with the capture, which
        ''' holds files/capl.as byte for byte. Exit 0.
        ''' </summary>
        <TestMethod, Capture("l-load-large")>
        Public Sub ALargeFileGoesInBlocksOf512()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("l-load-large").Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFromCapture("l-load-large", "capl.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "capl.as")
                Assert.IsTrue(standIn.WaitForEnd(Patience), "the stand-in's play had not ended")
                Assert.AreEqual(0, result.ExitCode, result.Errors)
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText("l-load-large", New RecordRange(10, 260)),
                                                            "khi load ok: robot sim, file ""capl.as"", 61441 bytes, programs capl"), result.Output)
            End Using
        End Sub

        ''' <summary>
        ''' A block goes only after the controller's next request: with the stand-in silent from record 16, its
        ''' second request, khi has sent the first block (record 15) and nothing more when it times out.
        ''' </summary>
        <TestMethod, Capture("l-load-large")>
        Public Sub EachBlockWaitsForTheNextRequest()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("l-load-large").StopAt(16, StandInFault.Silent).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFromCapture("l-load-large", "capl.as")
                Dim result As RunResult = workspace.Run(New Timeouts(TimeSpan.FromMilliseconds(500)), input, "load", "capl.as")
                Assert.IsTrue(standIn.WaitForEnd(Patience), "the stand-in's play had not ended")
                Assert.AreEqual(3, result.ExitCode, result.Errors)
                Assert.AreEqual(0, standIn.ReceivedAfterEnd.Length, "khi sent more of the file before the controller asked for it")
            End Using
        End Sub

        ''' <summary>
        ''' caps.as, exactly 1,024 bytes, replayed as capture (s) records 1 to 8 and 30 to 55 (D4), records 30
        ''' to 40 being load caps.as typed in pieces: two full blocks, then an empty block, then the end-of-file
        ''' block at the next request, as records 46 to 52 show. Exit 0.
        ''' </summary>
        <TestMethod, Capture("s-load-1024")>
        Public Sub AFileOfTwoFullBlocksEndsWithAnEmptyBlock()
            Using standIn As ControllerStandIn = ControllerStandIn.PlaySlice("s-load-1024", New RecordRange(1, 8), New RecordRange(30, 55)).Typed(New RecordRange(30, 40), Encoding.ASCII.GetBytes("load caps.as")).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFromCapture("s-load-1024", "caps.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "caps.as")
                Assert.IsTrue(standIn.WaitForEnd(Patience), "the stand-in's play had not ended")
                Assert.AreEqual(0, result.ExitCode, result.Errors)
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText("s-load-1024", New RecordRange(31, 55)),
                                                            "khi load ok: robot sim, file ""caps.as"", 1024 bytes, programs caps"), result.Output)
            End Using
        End Sub

    End Class

End Namespace
