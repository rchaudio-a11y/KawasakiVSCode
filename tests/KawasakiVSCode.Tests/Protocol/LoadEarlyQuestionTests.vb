' File:        LoadEarlyQuestionTests.vb
' Project:     KawasakiVSCode.Tests
' Description: U1, a question while file data is still to be sent: the request it comes with is answered
'              with the file's next block, then the person answers; after 1 no more of the file goes, and
'              after 0 the rest goes as the controller asks (FR-016, FR-021, captures (p) and (r), T056).
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
    ''' Captures (p) and (r), 1,815-byte files with capture (c)'s error in the first block (their notes). The
    ''' question comes in the controller's request for the second block, and KRterm answered that request
    ''' with the second block before any answer was typed.
    ''' </summary>
    <TestClass>
    Public Class LoadEarlyQuestionTests

        Private Shared ReadOnly Waits As New Timeouts(TimeSpan.FromMilliseconds(300))

        ''' <summary>Longer than every timeout: three of them and more.</summary>
        Private Shared ReadOnly Quiet As TimeSpan = TimeSpan.FromMilliseconds(1200)

        ' Fire demonstration (Article VII), 2026-11-04: made LoadOperation answer every request after the first
        ' block with the end-of-file block, so the request the question came with got it in place of the next
        ' data block, as the overruled reading had it; this test failed with "record 19, byte 8: expected 02 43
        ' 20 20 20 20 30 59 50 45 ..., received 02 43 20 20 20 20 30 1a 17". Restored the file; it passed.
        ''' <summary>
        ''' Capture (p) as a slice of records 1 to 8 and 11 to 25, leaving out the stray Ctrl+V of records 9 and
        ''' 10 (D4). The question arrives in record 18; khi sends bytes 513 to 1,024 (record 19) before anything is
        ''' typed, then waits for the person. The 1 and Enter typed are sent, the replay runs to its end with no
        ''' more of the file, and the result line reads sent 1024 of 1815 bytes. Exit 1.
        ''' </summary>
        <TestMethod, Capture("p-load-long-error")>
        Public Sub TheRequestIsAnsweredThenThePersonAnswers()
            Using standIn As ControllerStandIn = ControllerStandIn.PlaySlice("p-load-long-error", New RecordRange(1, 8), New RecordRange(11, 25)).AllowLine("1").Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  person As New AnonymousPipeServerStream(PipeDirection.Out),
                  keys As New AnonymousPipeClientStream(PipeDirection.In, person.ClientSafePipeHandle)
                workspace.CopyFromCapture("p-load-long-error", "capp.as")
                Dim running As Task(Of RunResult) = workspace.Start(Waits, keys, "load", "capp.as")
                Thread.Sleep(Quiet)
                Assert.IsFalse(running.IsCompleted, "khi ended before the person answered: " & Describe(running))
                Assert.AreEqual(0, standIn.Failures.Count, String.Join("; ", standIn.Failures))
                Dim typed As Byte() = Encoding.ASCII.GetBytes("1" & vbCrLf)
                person.Write(typed, 0, typed.Length)
                person.Flush()
                Dim result As RunResult = KhiWorkspace.Finish(running)
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText("p-load-long-error", New RecordRange(12, 25)),
                                                            "capp.as:    3 x = 1 +",
                                                            "capp.as:             ^(P0118)Illegal variable type.",
                                                            "capp.as: STEP syntax error.",
                                                            "capp.as: (0:Change to comment and continue, 1:Delete program and abort)",
                                                            "khi load failed: robot sim, file ""capp.as"", sent 1024 of 1815 bytes, programs capp; completed with 1 error; answered 1 (Delete program and abort)"), result.Output)
                Assert.AreEqual(1, result.ExitCode, result.Errors)
            End Using
        End Sub

        ''' <summary>
        ''' Capture (r): after the second block and the answer 0 and Enter, khi answers each further request as
        ''' (r) shows, the third block, the fourth and last of 279 bytes, then the end-of-file block. The whole
        ''' file goes, and the result line names the answer. Exit 1.
        ''' </summary>
        <TestMethod, Capture("r-load-long-error-0")>
        Public Sub AfterZeroTheRestOfTheFileGoesAsAsked()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("r-load-long-error-0").AllowLine("0").Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream(Encoding.ASCII.GetBytes("0" & vbCrLf))
                workspace.CopyFromCapture("r-load-long-error-0", "capr.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "capr.as")
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText("r-load-long-error-0", New RecordRange(10, 29)),
                                                            "capr.as:    3 x = 1 +",
                                                            "capr.as:             ^(P0118)Illegal variable type.",
                                                            "capr.as: STEP syntax error.",
                                                            "capr.as: (0:Change to comment and continue, 1:Delete program and abort)",
                                                            "khi load failed: robot sim, file ""capr.as"", 1815 bytes, programs capr; completed with 1 error; answered 0 (Change to comment and continue); the controller's copy differs from the file"), result.Output)
                Assert.AreEqual(1, result.ExitCode, result.Errors)
            End Using
        End Sub

        Private Shared Function Describe(running As Task(Of RunResult)) As String
            If Not running.IsCompleted OrElse running.IsFaulted Then Return "(still running or failed)"
            Return "exit " & running.Result.ExitCode.ToString() & ", " & running.Result.Errors
        End Function

    End Class

End Namespace
