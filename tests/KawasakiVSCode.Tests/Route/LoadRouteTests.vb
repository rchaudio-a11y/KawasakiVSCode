' File:        LoadRouteTests.vb
' Project:     KawasakiVSCode.Tests
' Description: khi load's refusals and failures through KhiApp.Run: a missing file, a name outside ASCII,
'              nothing listening, a silent and a closed controller mid-transfer, and Ctrl+C during the
'              transfer (Article XIII, FR-019, FR-020, FR-041, FR-044, INV-5, T054).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Tests.Protocol
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Route

    ''' <summary>khi load's refusals and failures, each reaching the exit code and the output (Article XIII).</summary>
    <TestClass>
    Public Class LoadRouteTests

        Private Const Capture As String = "b-load-small"

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(5)

        Private Shared ReadOnly Waits As New Timeouts(TimeSpan.FromMilliseconds(500))

        Private Shared ReadOnly NotFinished As String = "khi: the load did not finish and the controller's memory may have changed"

        ''' <summary>A file that does not exist is refused by name before connecting, exit 2 (FR-019).</summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub AMissingFileIsRefusedBeforeConnecting()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                Dim result As RunResult = workspace.Run(Waits, input, "load", "nosuch.as")
                Assert.AreEqual(2, result.ExitCode)
                Assert.AreEqual("khi: nosuch.as does not exist" & Environment.NewLine, result.Errors)
                Assert.AreEqual(0, result.Output.Length)
                Assert.IsFalse(standIn.Accepted, "khi connected before refusing")
            End Using
        End Sub

        ''' <summary>A file whose name holds a character outside ASCII is refused by name before connecting, exit 2 (FR-044).</summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub ANameOutsideAsciiIsRefusedBeforeConnecting()
            Dim name As String = ChrW(&HE7) & "apb.as"
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                File.Copy(Path.Combine(CaptureReader.Folder(Capture), "files", "capb.as"), Path.Combine(workspace.Folder, name))
                Dim result As RunResult = workspace.Run(Waits, input, "load", name)
                Assert.AreEqual(2, result.ExitCode)
                Assert.AreEqual("khi: " & name & " has a name outside ASCII; it cannot be sent" & Environment.NewLine, result.Errors)
                Assert.AreEqual(0, result.Output.Length)
                Assert.IsFalse(standIn.Accepted, "khi connected before refusing")
            End Using
        End Sub

        ' Red before the change, 2026-11-04: with LoadVerb refusing an unreadable file with exit 5, this test
        ' failed with "expected: 2, actual: 5" and "khi: cannot read capb.as: The process cannot access the file
        ' ... because it is being used by another process.". With exit 2, it passed.
        ''' <summary>
        ''' A file that exists but cannot be read, here held open by another handle that shares nothing, is
        ''' refused before connecting, exit 2: khi load reads the whole file before it connects, so this is a
        ''' refusal like a missing file, not a failed file step after a transfer (Architect, 2026-11-04).
        ''' </summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub AnUnreadableFileIsRefusedBeforeConnecting()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                Dim copied As String = workspace.CopyFromCapture(Capture, "capb.as")
                Dim result As RunResult
                Using held As New FileStream(copied, FileMode.Open, FileAccess.Read, FileShare.None)
                    result = workspace.Run(Waits, input, "load", "capb.as")
                End Using
                Assert.AreEqual(2, result.ExitCode, result.Errors)
                StringAssert.StartsWith(result.Errors, "khi: cannot read capb.as: ")
                Assert.AreEqual(0, result.Output.Length)
                Assert.IsFalse(standIn.Accepted, "khi connected before refusing")
            End Using
        End Sub

        ''' <summary>
        ''' With nothing listening at the robot's port, exit 4 naming the robot, the address and the system's
        ''' reason. Windows retries a refused connect for about two seconds, so the Connect wait is four seconds.
        ''' </summary>
        <TestMethod>
        Public Sub NothingListeningIsExitFour()
            Dim port As Integer = FreePort()
            Using workspace As New KhiWorkspace(port),
                  input As New MemoryStream()
                workspace.CopyFromCapture(Capture, "capb.as")
                Dim result As RunResult = workspace.Run(Waits.WithStep(WaitStep.Connect, TimeSpan.FromSeconds(4)), input, "load", "capb.as")
                Assert.AreEqual(4, result.ExitCode, result.Errors)
                StringAssert.StartsWith(result.Errors, "khi: cannot connect to robot sim at 127.0.0.1:" & port.ToString() & ": ")
            End Using
        End Sub

        ''' <summary>
        ''' INV-5: capture (b) silent at record 36, the controller's first C request, gives exit 3 naming
        ''' transfer data, the FR-020 message, and the result line.
        ''' </summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub ASilentControllerMidTransferTimesOut()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Typed(New RecordRange(9, 31), Encoding.ASCII.GetBytes("load capb.as")).StopAt(36, StandInFault.Silent).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFromCapture(Capture, "capb.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "capb.as")
                Assert.IsTrue(standIn.WaitForEnd(Patience), "the stand-in's play had not ended")
                Assert.AreEqual(3, result.ExitCode, result.Errors)
                Assert.AreEqual("khi: timed out waiting for transfer data (robot sim)" & Environment.NewLine & NotFinished & Environment.NewLine, result.Errors)
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText(Capture, New RecordRange(10, 34)),
                                                            "khi load failed: robot sim, file ""capb.as"", sent 0 of 103 bytes, programs none"), result.Output)
            End Using
        End Sub

        ''' <summary>
        ''' Capture (b) closed at record 38, after the file has gone, gives exit 4, the FR-020 message, and the
        ''' result line (User Story 2 scenario 7).
        ''' </summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub AClosedConnectionMidTransferSaysTheLoadDidNotFinish()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Typed(New RecordRange(9, 31), Encoding.ASCII.GetBytes("load capb.as")).StopAt(38, StandInFault.Close).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFromCapture(Capture, "capb.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "capb.as")
                Assert.IsTrue(standIn.WaitForEnd(Patience), "the stand-in's play had not ended")
                Assert.AreEqual(4, result.ExitCode, result.Errors)
                Assert.AreEqual("khi: robot sim closed the connection" & Environment.NewLine & NotFinished & Environment.NewLine, result.Errors)
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText(Capture, New RecordRange(10, 36)),
                                                            "khi load failed: robot sim, file ""capb.as"", 103 bytes, programs none"), result.Output)
            End Using
        End Sub

        ''' <summary>A Ctrl+C in the input during the transfer is never sent and ends nothing: the load runs to exit 0 (FR-041, INV-14).</summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub ControlCDuringTheTransferEndsNothing()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Typed(New RecordRange(9, 31), Encoding.ASCII.GetBytes("load capb.as")).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream(New Byte() {&H3, &H3, &H3})
                workspace.CopyFromCapture(Capture, "capb.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "capb.as")
                Assert.IsTrue(standIn.WaitForEnd(Patience), "the stand-in's play had not ended")
                Assert.AreEqual(0, result.ExitCode, result.Errors)
                StringAssert.EndsWith(Encoding.UTF8.GetString(result.Output), "khi load ok: robot sim, file ""capb.as"", 103 bytes, programs capb" & vbCrLf)
            End Using
        End Sub

        Private Shared Function FreePort() As Integer
            Dim listener As New TcpListener(IPAddress.Loopback, 0)
            listener.Start()
            Dim port As Integer = DirectCast(listener.LocalEndpoint, IPEndPoint).Port
            listener.Stop()
            Return port
        End Function

    End Class

End Namespace
