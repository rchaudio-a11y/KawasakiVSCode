' File:        ControllerConnectionTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Connecting, and the failures a connection reports in khi's own words: nothing listening,
'              a silent controller, a controller that closes (contracts/cli.md, Article X).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Net
Imports System.Net.Sockets
Imports System.Threading.Tasks
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Core.Reporting
Imports KawasakiVSCode.Core.Robots
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Connection

    ''' <summary>The connection's failures (contracts/cli.md, Timeout and connection messages).</summary>
    <TestClass>
    Public Class ControllerConnectionTests

        Private Shared ReadOnly ShortTimeouts As New Timeouts(TimeSpan.FromMilliseconds(300))

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(5)

        ''' <summary>Connecting to a port nobody listens on gives exit 4, naming the robot, the address and the system's reason. Windows retries a refused connect for about two seconds, so the Connect wait here is four seconds, not the short value the other tests use.</summary>
        <TestMethod>
        Public Sub NothingListeningIsReportedWithTheSystemReason()
            Dim port As Integer = FreePort()
            Dim robot As New RobotEntry("sim", "127.0.0.1", port, True)
            Dim refusal As KhiRefusal = RefuseWithin(Sub() ControllerConnection.Connect(robot, ShortTimeouts.WithStep(WaitStep.Connect, TimeSpan.FromSeconds(4))).Dispose())
            Assert.AreEqual(ExitCode.ConnectionFailed, refusal.ExitCode, refusal.Message)
            Dim start As String = "khi: cannot connect to robot sim at 127.0.0.1:" & port.ToString() & ": "
            StringAssert.StartsWith(refusal.Message, start)
            Assert.IsTrue(refusal.Message.Length > start.Length, "the system's reason is missing")
        End Sub

        ''' <summary>A read against a silent controller gives exit 3, naming the step and the robot.</summary>
        <TestMethod, Capture("a-connect-login")>
        Public Sub ASilentControllerTimesOutNamingTheStep()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("a-connect-login").StopAt(1, StandInFault.Silent).Start()
                Using connection As ControllerConnection = ControllerConnection.Connect(Robot(standIn), ShortTimeouts)
                    Dim refusal As KhiRefusal = RefuseWithin(Sub() connection.ReadUntil(WaitStep.Negotiation, Nothing, New Byte() {&HFF, &HFD, &H18}))
                    Assert.AreEqual(ExitCode.TimedOut, refusal.ExitCode)
                    Assert.AreEqual("khi: timed out waiting for telnet negotiation (robot sim)", refusal.Message)
                End Using
            End Using
        End Sub

        ''' <summary>A controller that closes the connection gives exit 4, naming the robot.</summary>
        <TestMethod, Capture("a-connect-login")>
        Public Sub AControllerThatClosesIsReported()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("a-connect-login").StopAt(1, StandInFault.Close).Start()
                Using connection As ControllerConnection = ControllerConnection.Connect(Robot(standIn), ShortTimeouts)
                    Dim refusal As KhiRefusal = RefuseWithin(Sub() connection.ReadUntil(WaitStep.Negotiation, Nothing, New Byte() {&HFF, &HFD, &H18}))
                    Assert.AreEqual(ExitCode.ConnectionFailed, refusal.ExitCode)
                    Assert.AreEqual("khi: robot sim closed the connection", refusal.Message)
                End Using
                Assert.IsTrue(standIn.WaitForEnd(Patience))
            End Using
        End Sub

        ''' <summary>The stand-in as a robot entry named sim.</summary>
        Friend Shared Function Robot(standIn As ControllerStandIn) As RobotEntry
            Return New RobotEntry("sim", "127.0.0.1", standIn.Port, True)
        End Function

        ''' <summary>
        ''' Runs an action that must fail with a KhiRefusal, and fails the test if it has not ended within five
        ''' seconds, so a wait with no timeout fails instead of hanging the suite (INV-5).
        ''' </summary>
        Friend Shared Function RefuseWithin(action As Action) As KhiRefusal
            Dim running As Task = Task.Run(action)
            Try
                If Not running.Wait(TimeSpan.FromSeconds(5)) Then
                    Assert.Fail("the call had not ended after five seconds: a wait without a timeout")
                End If
            Catch ex As AggregateException When TypeOf ex.InnerException Is KhiRefusal
                Return DirectCast(ex.InnerException, KhiRefusal)
            End Try
            Assert.Fail("the call ended without a refusal")
            Return Nothing
        End Function

        Private Shared Function FreePort() As Integer
            Dim listener As New TcpListener(IPAddress.Loopback, 0)
            listener.Start()
            Dim port As Integer = DirectCast(listener.LocalEndpoint, IPEndPoint).Port
            listener.Stop()
            Return port
        End Function

    End Class

End Namespace
