' File:        LoadQuestionTests.vb
' Project:     KawasakiVSCode.Tests
' Description: INV-7 and INV-11 at the LOAD question: the controller's words verbatim, nothing sent until
'              the person types, only what they type sent, and exit 1 after either answer (FR-021, T053).
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
    ''' Captures (n) and (o), records 1 to 23 (their notes): the question comes in record 16 with the
    ''' controller's request for the next block, khi answers that request with the end-of-file block (record
    ''' 17), and the answer is the digit and Enter (records 18 and 20). The test types into a pipe, as a
    ''' person would at the console, with short timeouts, so a wait with a limit would end the run.
    ''' </summary>
    <TestClass>
    Public Class LoadQuestionTests

        Private Shared ReadOnly Waits As New Timeouts(TimeSpan.FromMilliseconds(300))

        ''' <summary>Longer than every timeout: three of them and more.</summary>
        Private Shared ReadOnly Quiet As TimeSpan = TimeSpan.FromMilliseconds(1200)

        Private Shared ReadOnly NoAnswer As String = "khi: no answer to the controller's question; robot sim may still be waiting on it and may need a restart"

        ' Fire demonstrations (Article VII), 2026-11-04. INV-11: made LoadOperation send 1 and Enter at the
        ' question before reading the person's input; this test failed with "record 18, byte 1: expected 30,
        ' received 31". INV-7: made LoadOperation print "Syntax error in step." for the controller's "STEP
        ' syntax error."; this test failed, stdout differing from the capture's text at index 135. As it arrives:
        ' made LoadOperation print the controller's text only once the LOAD had ended; this test failed at its
        ' check of stdout while khi waits, and once the session closed MSTest showed the stand-in's report in
        ' its place, "record 18: expected 30, but the client closed the connection". Restored the file each
        ' time; it passed.
        ''' <summary>The answer 0, typed after the controller has waited past every timeout and after a Ctrl+C, runs capture (n) to its end.</summary>
        <TestMethod, Capture("n-load-question-0")>
        Public Sub TheAnswerZeroIsThePersonsAlone()
            AnswerAndCheck("n-load-question-0", "capn", "0",
                           "khi load failed: robot sim, file ""capn.as"", 148 bytes, programs capn; completed with 1 error; answered 0 (Change to comment and continue); the controller's copy differs from the file")
        End Sub

        ''' <summary>The answer 1, typed the same way, runs capture (o) to its end.</summary>
        <TestMethod, Capture("o-load-question-1")>
        Public Sub TheAnswerOneIsThePersonsAlone()
            AnswerAndCheck("o-load-question-1", "capo", "1",
                           "khi load failed: robot sim, file ""capo.as"", 148 bytes, programs capo; completed with 1 error; answered 1 (Delete program and abort)")
        End Sub

        ''' <summary>
        ''' Input ending before an answer closes the session with exit 1 and says the robot may still be waiting;
        ''' a Ctrl+C typed first is not an answer, and nothing is sent (the stand-in is silent from record 18).
        ''' </summary>
        <TestMethod, Capture("n-load-question-0")>
        Public Sub InputEndingBeforeAnAnswerSaysTheRobotMayBeWaiting()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("n-load-question-0").StopAt(18, StandInFault.Silent).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream(New Byte() {&H3})
                workspace.CopyFromCapture("n-load-question-0", "capn.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "capn.as")
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                Assert.AreEqual(1, result.ExitCode, result.Errors)
                Assert.AreEqual(NoAnswer & Environment.NewLine, result.Errors)
                Assert.AreEqual(0, standIn.ReceivedAfterEnd.Length, "khi sent something at the question with nothing typed")
                StringAssert.EndsWith(Encoding.UTF8.GetString(result.Output), "khi load failed: robot sim, file ""capn.as"", 148 bytes, programs capn" & vbCrLf)
            End Using
        End Sub

        ' Built in T062 before this test, which was added on the Architect's ruling of 2026-11-04; its fire
        ' demonstration is its proof (Article VII). Fire, 2026-11-04: made PersonInput take 1d as an ordinary
        ' key; this test failed, the stand-in receiving the 0 and Enter typed after it and reporting "commands
        ' outside the allowed set (INV-2)". Restored the file; it passed.
        ''' <summary>
        ''' Ctrl+] at the question is no answer: khi closes the session with exit 1 and says the robot may still
        ''' be waiting, as when the input ends, and the 0 and Enter typed after it are never sent (Architect,
        ''' 2026-11-04). The stand-in is silent from record 18.
        ''' </summary>
        <TestMethod, Capture("n-load-question-0")>
        Public Sub ControlBracketAtTheQuestionIsNoAnswer()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("n-load-question-0").StopAt(18, StandInFault.Silent).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  input As New MemoryStream(New Byte() {&H1D, &H30, &HD, &HA})
                workspace.CopyFromCapture("n-load-question-0", "capn.as")
                Dim result As RunResult = workspace.Run(Waits, input, "load", "capn.as")
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                Assert.AreEqual(1, result.ExitCode, result.Errors)
                Assert.AreEqual(NoAnswer & Environment.NewLine, result.Errors)
                Assert.AreEqual(0, standIn.ReceivedAfterEnd.Length, "khi sent something after Ctrl+] at the question")
                StringAssert.EndsWith(Encoding.UTF8.GetString(result.Output), "khi load failed: robot sim, file ""capn.as"", 148 bytes, programs capn" & vbCrLf)
            End Using
        End Sub

        ''' <summary>
        ''' Replays records 1 to 23 of a question capture, typing nothing past every timeout, then a Ctrl+C, then
        ''' the answer and Enter; checks the stand-in saw only what was typed, and the output and exit code.
        ''' </summary>
        Private Shared Sub AnswerAndCheck(capture As String, program As String, answer As String, resultLine As String)
            Using standIn As ControllerStandIn = ControllerStandIn.PlaySlice(capture, New RecordRange(1, 23)).AllowLine(answer).Start(),
                  workspace As New KhiWorkspace(standIn.Port),
                  person As New AnonymousPipeServerStream(PipeDirection.Out),
                  keys As New AnonymousPipeClientStream(PipeDirection.In, person.ClientSafePipeHandle),
                  stdout As New MemoryStream()
                workspace.CopyFromCapture(capture, program & ".as")
                Dim running As Task(Of RunResult) = workspace.Start(Waits, keys, stdout, "load", program & ".as")
                Thread.Sleep(Quiet)
                Assert.IsFalse(running.IsCompleted, "khi ended before the person answered: " & Describe(running))
                Assert.AreEqual(0, standIn.Failures.Count, "khi sent something at the question with nothing typed: " & String.Join("; ", standIn.Failures))
                Assert.AreEqual(Encoding.Latin1.GetString(LoadOutput.ControllerText(capture, New RecordRange(10, 16))), Encoding.Latin1.GetString(stdout.ToArray()),
                                "stdout while khi waits: the controller's text up to the question, printed as it came")
                person.WriteByte(&H3)
                person.Flush()
                Thread.Sleep(Quiet)
                Assert.IsFalse(running.IsCompleted, "a Ctrl+C ended khi at the question: " & Describe(running))
                Assert.AreEqual(0, standIn.Failures.Count, "a Ctrl+C reached the controller: " & String.Join("; ", standIn.Failures))
                Dim typed As Byte() = Encoding.ASCII.GetBytes(answer & vbCrLf)
                person.Write(typed, 0, typed.Length)
                person.Flush()
                Dim result As RunResult = KhiWorkspace.Finish(running)
                Assert.IsTrue(standIn.WaitForEnd(TimeSpan.FromSeconds(5)), "the stand-in's play had not ended")
                LoadOutput.AssertOutput(LoadOutput.Expected(LoadOutput.ControllerText(capture, New RecordRange(10, 23)),
                                                            program & ".as:    3 x = 1 +",
                                                            program & ".as:             ^(P0118)Illegal variable type.",
                                                            program & ".as: STEP syntax error.",
                                                            program & ".as: (0:Change to comment and continue, 1:Delete program and abort)",
                                                            resultLine), result.Output)
                Assert.AreEqual("", result.Errors)
                Assert.AreEqual(1, result.ExitCode)
            End Using
        End Sub

        Private Shared Function Describe(running As Task(Of RunResult)) As String
            If Not running.IsCompleted OrElse running.IsFaulted Then Return "(still running or failed)"
            Return "exit " & running.Result.ExitCode.ToString() & ", " & running.Result.Errors
        End Function

    End Class

End Namespace
