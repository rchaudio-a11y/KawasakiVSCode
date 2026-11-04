' File:        LoadPathTests.vb
' Project:     KawasakiVSCode.Tests
' Description: INV-12, the path stays on the PC: a file given by its full path is loaded with only its
'              bare name in the command (FR-027, T050).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Text
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Tests.Route
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Protocol

    ''' <summary>INV-12 against capture (b), whose command is load capb.as (the capture's note).</summary>
    <TestClass>
    Public Class LoadPathTests

        Private Const Capture As String = "b-load-small"

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(5)

        ' Fire demonstration (Article VII), 2026-11-04: made ControllerCommand.Load send the full path; this
        ' test failed with "record 9, byte 6: expected 6c 6f 61 64 20 63 61 70 62 2e 61 73, received 6c 6f 61
        ' 64 20 43 3a 5c 55 73 65 72". Restored the file; it passed.
        ''' <summary>
        ''' capb.as given by its full path: the stand-in records the command load capb.as, and the result line
        ''' names the file as the person gave it.
        ''' </summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub OnlyTheBareNameIsSent()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Typed(New RecordRange(9, 31), Encoding.ASCII.GetBytes("load capb.as")).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                Dim fullPath As String = workspace.CopyFromCapture(Capture, "capb.as")
                Assert.IsTrue(Path.IsPathRooted(fullPath))
                Dim result As RunResult = workspace.Run(New Timeouts(TimeSpan.FromSeconds(2)), input, "load", fullPath)
                Assert.IsTrue(standIn.WaitForEnd(Patience), "the stand-in's play had not ended")
                Assert.AreEqual(0, result.ExitCode, result.Errors)
                CollectionAssert.AreEqual(New String() {"as", "load capb.as"}, standIn.Commands.ToArray())
                StringAssert.EndsWith(Encoding.UTF8.GetString(result.Output), "khi load ok: robot sim, file """ & fullPath & """, 103 bytes, programs capb" & vbCrLf)
            End Using
        End Sub

    End Class

End Namespace
