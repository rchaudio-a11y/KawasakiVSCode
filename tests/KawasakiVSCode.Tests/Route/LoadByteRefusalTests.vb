' File:        LoadByteRefusalTests.vb
' Project:     KawasakiVSCode.Tests
' Description: U2, a ff or 17 byte in a file to load: refused before connecting, naming the file, the
'              byte and its offset from 0, exit 2 (FR-042, contracts/cli.md, T059).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Route

    ''' <summary>
    ''' The fixtures are capture (b)'s capb.as with one byte changed: Fixtures/u2-ff/capb.as holds ff at
    ''' offset 37, Fixtures/u2-etb/capb.as holds 17 at offset 52. No capture shows how telnet would carry ff,
    ''' and 17 ends a transfer block, so neither can be sent.
    ''' </summary>
    <TestClass>
    Public Class LoadByteRefusalTests

        ' Fire demonstration (Article VII), 2026-11-04, with the test below: removed LoadVerb's call to
        ' TransferFraming.CheckSendable, so the file was sent anyway; both tests failed, this one with "record
        ' 37, byte 45: expected 75 72 65 20 28 62 29 3a 20 61 ..., received 75 72 65 20 28 62 29 3a ff 61 ...",
        ' the stand-in seeing the ff sent. Restored the file; both passed.
        ''' <summary>A file holding ff is refused naming the byte and its offset, and no connection reaches the listening stand-in.</summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub AFileHoldingFfIsRefused()
            RefusedBeforeConnecting("u2-ff", "khi: capb.as holds byte ff at offset 37; it cannot be sent")
        End Sub

        ''' <summary>A file holding 17 is refused the same way.</summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub AFileHoldingEtbIsRefused()
            RefusedBeforeConnecting("u2-etb", "khi: capb.as holds byte 17 at offset 52; it cannot be sent")
        End Sub

        Private Shared Sub RefusedBeforeConnecting(fixture As String, message As String)
            Using standIn As ControllerStandIn = ControllerStandIn.Play("b-load-small").Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFixture(fixture, "capb.as")
                Dim result As RunResult = workspace.Run(New Timeouts(TimeSpan.FromSeconds(2)), input, "load", "capb.as")
                Assert.AreEqual(2, result.ExitCode)
                Assert.AreEqual(message & Environment.NewLine, result.Errors)
                Assert.AreEqual(0, result.Output.Length)
                Threading.Thread.Sleep(200)
                Assert.IsFalse(standIn.Accepted, "khi connected before refusing")
            End Using
        End Sub

    End Class

End Namespace
