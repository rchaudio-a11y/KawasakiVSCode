' File:        LoadProtocolTests.vb
' Project:     KawasakiVSCode.Tests
' Description: khi load against capture (b): the command, the transfer framing, the controller's text
'              copied unaltered, the result line and exit 0; and no connection on a refused command line
'              (User Story 2 scenario 1, FR-016, FR-017, FR-028, T047).
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
    ''' khi load replayed against capture (b), a LOAD of a 103-byte program (the capture's note). The stand-in
    ''' compares every byte khi sends with the capture: the command (D1), A    0 for the A block, the one C
    ''' block 02 43 20 20 20 20 30 with the 103 file bytes and 17, the end-of-file block 02 43 20 20 20 20 30
    ''' 1a 17, and E    0. Records 9 to 31, load capb.as typed one character per record, are a typed region:
    ''' khi sends the line whole (D1).
    ''' </summary>
    <TestClass>
    Public Class LoadProtocolTests

        Private Const Capture As String = "b-load-small"

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(5)

        Private Shared ReadOnly Waits As New Timeouts(TimeSpan.FromSeconds(2))

        ''' <summary>
        ''' khi load capb.as sends the command, answers each block as capture (b) shows, copies the controller's
        ''' text to stdout unaltered, ends with the result line, and exits 0.
        ''' </summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub LoadsASmallFileAsCaptureBShows()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Typed(New RecordRange(9, 31), Encoding.ASCII.GetBytes("load capb.as")).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFromCapture(Capture, "capb.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "capb.as")
                Assert.IsTrue(standIn.WaitForEnd(Patience), "the stand-in's play had not ended")
                Assert.AreEqual("", result.Errors)
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText(Capture, New RecordRange(10, 42)),
                                                            "khi load ok: robot sim, file ""capb.as"", 103 bytes, programs capb"), result.Output)
                Assert.AreEqual(0, result.ExitCode)
                CollectionAssert.AreEqual(New String() {"as", "load capb.as"}, standIn.Commands.ToArray())
            End Using
        End Sub

        ' Fire demonstration (Article VII), 2026-11-04: made KhiApp.Run connect to the default robot before
        ' parsing the command line; this test failed, the stand-in reporting "record 1: expected ff fb 18, but
        ' the client closed the connection after (nothing)". Restored the file; it passed.
        ''' <summary>
        ''' Every command line T038 refuses is refused with exit 2 before connecting, in a folder holding the
        ''' file and a robots file naming a listening stand-in: the stand-in accepts no connection. khi load is
        ''' the first verb that connects, so this is asserted here (T047). Until khi load connected, this passed
        ''' vacuously and was not counted as Red (Article VII); its fire demonstration proves it.
        ''' </summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub ARefusedCommandLineMakesNoConnection()
            Dim refused As String()() = {
                New String() {},
                New String() {"run"},
                New String() {"LOAD", "capb.as"},
                New String() {"load", "capb.as", "--force"},
                New String() {"load", "capb.as", "-r", "sim"},
                New String() {"term", "--qualifier", "P"},
                New String() {"load", "capb.as", "--qualifier", "P"},
                New String() {"load", "capb.as", "--robot", "sim", "--robot", "sim"},
                New String() {"load", "capb.as", "--robot"},
                New String() {"save", "capb.as", "--qualifier", "--robot", "sim"},
                New String() {"load"},
                New String() {"save", "--robot", "sim"},
                New String() {"load", "capb.as", "capb.as"},
                New String() {"term", "capb.as"},
                New String() {"save", "capb.as", "--qualifier", "L"},
                New String() {"save", "capb.as", "--qualifier", "full"}}
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Start(),
                  workspace As New KhiWorkspace(standIn.Port)
                workspace.CopyFromCapture(Capture, "capb.as")
                For Each args As String() In refused
                    Using input As New MemoryStream()
                        Dim result As RunResult = workspace.Run(Waits, input, args)
                        Assert.AreEqual(2, result.ExitCode, String.Join(" ", args))
                    End Using
                Next
                Threading.Thread.Sleep(200)
                Assert.IsFalse(standIn.Accepted, "a refused command line connected to the robot")
            End Using
        End Sub

    End Class

End Namespace
