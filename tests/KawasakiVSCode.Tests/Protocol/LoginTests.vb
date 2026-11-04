' File:        LoginTests.vb
' Project:     KawasakiVSCode.Tests
' Description: The telnet negotiation and the login, replayed from capture (a), and the timeout of each
'              wait in them (INV-3, INV-5, Article IX).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Linq
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Core.Reporting
Imports KawasakiVSCode.Core.Session
Imports KawasakiVSCode.Tests.Connection
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Protocol

    ''' <summary>Logging in, against capture (a).</summary>
    <TestClass>
    Public Class LoginTests

        Private Const CaptureName As String = "a-connect-login"

        Private Shared ReadOnly ShortTimeouts As New Timeouts(TimeSpan.FromMilliseconds(300))

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(5)

        ''' <summary>
        ''' khi sends ff fb 18 first, nothing in reply to ff fd 18, and 61 73 0d 0a at the login prompt, as the
        ''' stand-in checks against the capture; it returns at the prompt after the banner, having read every
        ''' byte the controller sent and sent nothing more.
        ''' </summary>
        <TestMethod, Capture(CaptureName)>
        Public Sub LogsInAsCaptureAShows()
            Dim controllerBytes As Byte() = CaptureReader.Read(CaptureName).Where(Function(record As CaptureRecord) record.Direction = "S"c).SelectMany(Function(record As CaptureRecord) record.Bytes).ToArray()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(CaptureName).Start()
                Dim received As Byte()
                Using connection As ControllerConnection = ControllerConnection.Connect(ControllerConnectionTests.Robot(standIn), ShortTimeouts)
                    received = LoginSequence.Run(connection)
                End Using
                Assert.IsTrue(standIn.WaitForEnd(Patience))
                CollectionAssert.AreEqual(controllerBytes, received)
                Assert.AreEqual(0, standIn.ReceivedAfterEnd.Length, "khi sent bytes after the login")
            End Using
        End Sub

        ' Fire demonstration (Article VII, INV-5), 2026-11-04, for this test and the next: set the read timeout
        ' in ControllerConnection.ReadUntil to Timeout.Infinite; both tests failed after five seconds with "the
        ' call had not ended after five seconds: a wait without a timeout". Restored the file; both passed.
        ''' <summary>INV-5: with the controller silent at record 2, the login ends with exit 3 naming the telnet negotiation.</summary>
        <TestMethod, Capture(CaptureName)>
        Public Sub SilentAtTheNegotiation()
            Assert.AreEqual("khi: timed out waiting for telnet negotiation (robot sim)", SilentAt(2))
        End Sub

        ''' <summary>INV-5: with the controller silent at record 8, the login ends with exit 3 naming the prompt after login.</summary>
        <TestMethod, Capture(CaptureName)>
        Public Sub SilentAfterTheLogin()
            Assert.AreEqual("khi: timed out waiting for the prompt after login (robot sim)", SilentAt(8))
        End Sub

        Private Shared Function SilentAt(record As Integer) As String
            Using standIn As ControllerStandIn = ControllerStandIn.Play(CaptureName).StopAt(record, StandInFault.Silent).Start()
                Using connection As ControllerConnection = ControllerConnection.Connect(ControllerConnectionTests.Robot(standIn), ShortTimeouts)
                    Dim refusal As KhiRefusal = ControllerConnectionTests.RefuseWithin(Sub() LoginSequence.Run(connection))
                    Assert.AreEqual(ExitCode.TimedOut, refusal.ExitCode)
                    Return refusal.Message
                End Using
            End Using
        End Function

    End Class

End Namespace
