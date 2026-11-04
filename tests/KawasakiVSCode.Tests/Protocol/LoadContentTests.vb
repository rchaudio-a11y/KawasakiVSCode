' File:        LoadContentTests.vb
' Project:     KawasakiVSCode.Tests
' Description: INV-1, not an editor: the bytes khi load sends for a file equal the file on disk, trailing
'              spaces, tabs and mixed line endings included (Article II, T048).
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
    ''' INV-1 against capture (b). Its payload region, record 37 bytes 8 to 110 (the capture's note), is
    ''' compared with the fixture Fixtures/inv1-mixed/capb.as instead of the captured file, so the stand-in
    ''' fails the test on any byte khi changes.
    ''' </summary>
    <TestClass>
    Public Class LoadContentTests

        Private Const Capture As String = "b-load-small"

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(5)

        ''' <summary>The fixture holds what an editor would change: trailing spaces and tabs, and CR LF, LF and CR line ends, in under 512 bytes.</summary>
        <TestMethod>
        Public Sub TheFixtureHoldsWhatAnEditorWouldChange()
            Dim fixture As Byte() = File.ReadAllBytes(KhiWorkspace.FixturePath("inv1-mixed", "capb.as"))
            Dim text As String = Encoding.ASCII.GetString(fixture)
            Assert.IsTrue(fixture.Length < 512, "the fixture must fit one block")
            StringAssert.Contains(text, "   " & vbCrLf, "trailing spaces before CR LF")
            StringAssert.Contains(text, vbTab & " " & vbLf, "a trailing tab and space before a bare LF")
            StringAssert.Contains(text, vbTab & vbCr & vbTab, "a trailing tab before a bare CR")
        End Sub

        ' Fire demonstration (Article VII), 2026-11-04: made LoadVerb trim trailing spaces and tabs from each
        ' line of the file before sending; this test failed with "record 37: the payload differs from the
        ' fixture at byte 16 of the file". Restored the file; it passed.
        ''' <summary>The stand-in receives the fixture unchanged, and the result line counts its bytes.</summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub TheFileGoesUnchanged()
            Dim fixture As Byte() = File.ReadAllBytes(KhiWorkspace.FixturePath("inv1-mixed", "capb.as"))
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Typed(New RecordRange(9, 31), Encoding.ASCII.GetBytes("load capb.as")).Payload(fixture, 8, 37).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFixture("inv1-mixed", "capb.as")
                Dim result As RunResult = workspace.Run(New Timeouts(TimeSpan.FromSeconds(2)), input, "load", "capb.as")
                Assert.IsTrue(standIn.WaitForEnd(Patience), "the stand-in's play had not ended")
                Assert.AreEqual(0, result.ExitCode, result.Errors)
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText(Capture, New RecordRange(10, 42)),
                                                            "khi load ok: robot sim, file ""capb.as"", " & fixture.Length.ToString() & " bytes, programs capb"), result.Output)
            End Using
        End Sub

    End Class

End Namespace
