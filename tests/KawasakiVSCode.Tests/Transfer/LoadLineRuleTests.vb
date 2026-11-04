' File:        LoadLineRuleTests.vb
' Project:     KawasakiVSCode.Tests
' Description: LoadLineRule, the one door for what a line during a LOAD means: normal, an error line, a
'              question, or a refusal ending at the prompt, each read from a capture's controller text
'              (D2, D3, H1, FR-018, FR-021, research R15, T058).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Text
Imports KawasakiVSCode.Core.Transfer
Imports KawasakiVSCode.Tests.Protocol
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Transfer

    ''' <summary>
    ''' The line rule, on lines taken from the captures' controller text (transfer blocks taken out), not
    ''' written by hand. Capture (j) is a typed LOAD answered with A-1052, which khi load never sends, since it
    ''' refuses a missing file before connecting; so its text is checked here, on the rule.
    ''' </summary>
    <TestClass>
    Public Class LoadLineRuleTests

        ' Fire demonstration (Article VII), 2026-11-04: made LoadLineRule.Verdict ignore the prompt after a line,
        ' so the line stayed one the fallback may hand to the person; this test failed with "expected: Refusal,
        ' actual: ErrorLine". Restored the file; it passed.
        ''' <summary>
        ''' (P2016)Cannot open the file. followed by 0d 0a 3e (capture (j) records 14 to 16) is a refusal ending at
        ''' the prompt, never handed to the person; with no prompt after it, it is an error line.
        ''' </summary>
        <TestMethod, Capture("j-load-missing")>
        Public Sub ALineFollowedByThePromptIsARefusal()
            Dim lines As List(Of String) = LinesOf(LoadOutput.ControllerText("j-load-missing", New RecordRange(14, 16)))
            Assert.AreEqual(2, lines.Count)
            Assert.AreEqual("(P2016)Cannot open the file.", lines(0))
            Assert.AreEqual(">", lines(1), "the prompt follows the line")
            Assert.AreEqual(LoadLineVerdict.Refusal, LoadLineRule.Verdict(lines(0), Nothing, followedByPrompt:=True))
            Assert.AreEqual(LoadLineVerdict.ErrorLine, LoadLineRule.Verdict(lines(0), Nothing, followedByPrompt:=False))
        End Sub

        ''' <summary>
        ''' Capture (n) record 16: Program  capn() is normal and names capn; the step, the P0118 line and STEP
        ''' syntax error. are error lines; the blank line between them is normal; the choice line is a question,
        ''' and its labels are the controller's.
        ''' </summary>
        <TestMethod, Capture("n-load-question-0")>
        Public Sub TheChoiceLineIsAQuestion()
            Dim lines As List(Of String) = LinesOf(LoadOutput.ControllerText("n-load-question-0", New RecordRange(16, 16)))
            Assert.AreEqual(6, lines.Count)
            Assert.AreEqual(LoadLineVerdict.Normal, LoadLineRule.Verdict(lines(0), Nothing, False), lines(0))
            Assert.AreEqual("capn", LoadLineRule.ProgramName(lines(0)))
            Assert.AreEqual(LoadLineVerdict.ErrorLine, LoadLineRule.Verdict(lines(1), Nothing, False), lines(1))
            Assert.AreEqual(LoadLineVerdict.ErrorLine, LoadLineRule.Verdict(lines(2), Nothing, False), lines(2))
            Assert.AreEqual("", lines(3))
            Assert.AreEqual(LoadLineVerdict.Normal, LoadLineRule.Verdict(lines(3), Nothing, False), "the blank line")
            Assert.AreEqual(LoadLineVerdict.ErrorLine, LoadLineRule.Verdict(lines(4), Nothing, False), lines(4))
            Dim question As String = lines(5)
            Assert.AreEqual(LoadLineVerdict.Question, LoadLineRule.Verdict(question, Nothing, False), question)
            Assert.AreEqual("Change to comment and continue", LoadLineRule.LabelFor(question, "0"))
            Assert.AreEqual("Delete program and abort", LoadLineRule.LabelFor(question, "1"))
            Assert.IsNull(LoadLineRule.LabelFor(question, "2"), "an answer matching no choice has no label")
            Assert.IsNull(LoadLineRule.ProgramName(question))
        End Sub

        ''' <summary>
        ''' Capture (f) record 26: Cannot execute because program already in use., with no prompt after it, is an
        ''' error line, which the fallback may hand to the person (D3).
        ''' </summary>
        <TestMethod, Capture("f-typed-save-load")>
        Public Sub ALineWithNoPromptAfterItIsAnErrorLine()
            Dim lines As List(Of String) = LinesOf(LoadOutput.ControllerText("f-typed-save-load", New RecordRange(26, 26)))
            Assert.AreEqual(2, lines.Count)
            Assert.AreEqual(LoadLineVerdict.Normal, LoadLineRule.Verdict(lines(0), Nothing, False), lines(0))
            Assert.AreEqual("Cannot execute because program already in use.", lines(1))
            Assert.AreEqual(LoadLineVerdict.ErrorLine, LoadLineRule.Verdict(lines(1), Nothing, False))
        End Sub

        ''' <summary>
        ''' Every line of capture (b)'s LOAD is normal: the echo and redraw of the load line, Loading...(capb.as),
        ''' Program  capb() and File load completed. (0 errors), whose count is 0.
        ''' </summary>
        <TestMethod, Capture("b-load-small")>
        Public Sub TheLinesOfCaptureBAreNormal()
            Dim lines As List(Of String) = LinesOf(LoadOutput.ControllerText("b-load-small", New RecordRange(10, 42)))
            Assert.AreEqual(5, lines.Count)
            Assert.AreEqual(">", lines(4), "the prompt ends the text")
            For Each line As String In lines.Take(4)
                Assert.AreEqual(LoadLineVerdict.Normal, LoadLineRule.Verdict(line, Nothing, False), line)
            Next
            Assert.AreEqual(0, LoadLineRule.ErrorCount(lines(3)))
            Assert.IsNull(LoadLineRule.ErrorCount(lines(1)), "only the completion line carries a count")
        End Sub

        ''' <summary>The count in capture (n)'s completion line is 1.</summary>
        <TestMethod, Capture("n-load-question-0")>
        Public Sub TheCompletionLineCarriesTheControllersCount()
            Dim lines As List(Of String) = LinesOf(LoadOutput.ControllerText("n-load-question-0", New RecordRange(23, 23)))
            Assert.AreEqual("File load completed. (1 errors)", lines(0))
            Assert.AreEqual(1, LoadLineRule.ErrorCount(lines(0)))
        End Sub

        ''' <summary>
        ''' After the answer, the controller echoes it, and the line ends at the 0d 0a before the E block (capture
        ''' (n) records 19 and 21): the echo of the person's answer is normal; the same line with no answer typed
        ''' is not.
        ''' </summary>
        <TestMethod, Capture("n-load-question-0")>
        Public Sub TheEchoOfTheAnswerIsNormal()
            Dim lines As List(Of String) = LinesOf(LoadOutput.ControllerText("n-load-question-0", New RecordRange(19, 21)))
            Assert.AreEqual("0", lines(0))
            Assert.AreEqual(LoadLineVerdict.Normal, LoadLineRule.Verdict(lines(0), "0", False))
            Assert.AreEqual(LoadLineVerdict.ErrorLine, LoadLineRule.Verdict(lines(0), Nothing, False))
        End Sub

        ''' <summary>The text's lines, split at 0d 0a; a last piece with no line end, such as the prompt, is kept as a line.</summary>
        Private Shared Function LinesOf(text As Byte()) As List(Of String)
            Dim lines As New List(Of String)(Encoding.Latin1.GetString(text).Split(vbCrLf))
            If lines(lines.Count - 1).Length = 0 Then lines.RemoveAt(lines.Count - 1)
            Return lines
        End Function

    End Class

End Namespace
