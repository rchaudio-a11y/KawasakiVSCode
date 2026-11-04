' File:        LoadQuietTests.vb
' Project:     KawasakiVSCode.Tests
' Description: The D3 fallback: an error line followed by the controller going quiet, with no prompt, is
'              handed to the person instead of timing out; quiet after normal lines still times out
'              (FR-021, T055).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Tests.Route
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Protocol

    ''' <summary>
    ''' Capture (f)'s first LOAD as a slice of records 1 to 8 and 19 to 27 (D4), silent from record 28: after
    ''' Cannot execute because program already in use. (record 26) and khi's end-of-file block (record 27),
    ''' the controller goes quiet. Short timeouts, so a bounded wait would end the run.
    ''' </summary>
    <TestClass>
    Public Class LoadQuietTests

        Private Shared ReadOnly Waits As New Timeouts(TimeSpan.FromMilliseconds(300))

        ''' <summary>Longer than every timeout: three of them and more.</summary>
        Private Shared ReadOnly Quiet As TimeSpan = TimeSpan.FromMilliseconds(1200)

        ' Fire demonstration (Article VII), 2026-11-04: made LoadOperation time out after an error line as after
        ' a normal one; this test failed with "khi ended instead of handing the line to the person: exit 3, khi:
        ' timed out waiting for the end of the transfer (robot sim)". Restored the file; it passed.
        ''' <summary>
        ''' When the TransferEnd wait runs out after the error line, khi hands the line to the person and keeps
        ''' waiting. The 1 and Enter typed are sent as typed, after the stop; with the controller still quiet,
        ''' the line is the person's again, and input then ending gives exit 1 and the no-answer message.
        ''' </summary>
        <TestMethod, Capture("f-typed-save-load")>
        Public Sub AnErrorLineThenQuietGoesToThePerson()
            Using standIn As ControllerStandIn = ControllerStandIn.PlaySlice("f-typed-save-load", New RecordRange(1, 8), New RecordRange(19, 27)).StopAt(28, StandInFault.Silent).AllowLine("1").Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  person As New AnonymousPipeServerStream(PipeDirection.Out),
                  keys As New AnonymousPipeClientStream(PipeDirection.In, person.ClientSafePipeHandle)
                workspace.CopyFromCapture("b-load-small", "capb.as")
                Dim running As Task(Of RunResult) = workspace.Start(Waits, keys, "load", "capb.as")
                Thread.Sleep(Quiet)
                Assert.IsFalse(running.IsCompleted, "khi ended instead of handing the line to the person: " & Describe(running))
                Dim typed As Byte() = Encoding.ASCII.GetBytes("1" & vbCrLf)
                person.Write(typed, 0, typed.Length)
                person.Flush()
                Thread.Sleep(Quiet)
                Assert.IsFalse(running.IsCompleted, "khi ended after the answer instead of handing the line to the person again: " & Describe(running))
                person.Dispose()
                Dim result As RunResult = KhiWorkspace.Finish(running)
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                Assert.AreEqual(1, result.ExitCode, result.Errors)
                Assert.AreEqual("khi: no answer to the controller's question; robot sim may still be waiting on it and may need a restart" & Environment.NewLine, result.Errors)
                CollectionAssert.AreEqual(New Byte() {&H31, &HD, &HA}, standIn.ReceivedAfterEnd, "khi sent other than what the person typed")
                StringAssert.EndsWith(Encoding.UTF8.GetString(result.Output), "capb.as: Cannot execute because program already in use." & vbCrLf &
                                      "khi load failed: robot sim, file ""capb.as"", 103 bytes, programs capb" & vbCrLf)
            End Using
        End Sub

        ''' <summary>
        ''' A controller that goes quiet after normal lines only still times out, exit 3: capture (b) silent at
        ''' record 36, after Loading is still to come and only the command's echo has been printed.
        ''' </summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub QuietAfterNormalLinesStillTimesOut()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("b-load-small").Typed(New RecordRange(9, 31), Encoding.ASCII.GetBytes("load capb.as")).StopAt(36, StandInFault.Silent).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream()
                workspace.CopyFromCapture("b-load-small", "capb.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "capb.as")
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                Assert.AreEqual(3, result.ExitCode, result.Errors)
                StringAssert.StartsWith(result.Errors, "khi: timed out waiting for transfer data (robot sim)")
            End Using
        End Sub

        Private Shared Function Describe(running As Task(Of RunResult)) As String
            If Not running.IsCompleted OrElse running.IsFaulted Then Return "(still running or failed)"
            Return "exit " & running.Result.ExitCode.ToString() & ", " & running.Result.Errors
        End Function

    End Class

End Namespace
