' File:        LoadLineRule.vb
' Project:     KawasakiVSCode.Core
' Description: The one door for what a line the controller prints during a LOAD means: normal, an error
'              line, a question, or a refusal ending at the prompt; the program it names; the error count
'              (D2, D3, H1, FR-018, FR-021, research R15).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Globalization
Imports System.Text.RegularExpressions

Namespace Transfer

    ''' <summary>
    ''' The one door for the meaning of a line during a LOAD (research R15). A line is the controller's text up
    ''' to 0d 0a, without it, with the transfer blocks taken out.
    ''' <para>
    ''' The normal lines are those a good LOAD prints in captures (b), (i), (l), (q) and (s): the echo and redraw
    ''' of the load line, Loading...(file), Program  name() with two spaces, and File load completed. (n errors).
    ''' 90209-1022 §5.3 (LOAD, pages 5-34 and 5-35) shows them as Loading..., Program a1() and Loading done.;
    ''' khi relies on the captured text. A blank line, as between the P0118 line and STEP syntax error. in
    ''' captures (c), (n), (o), (p) and (r), and the controller's echo of the answer the person typed (captures
    ''' (n), (o), (p), (r)) carry no message and are normal too.
    ''' </para>
    ''' <para>
    ''' Every other line is an error line (D2), such as Cannot execute because program already in use. (capture
    ''' (f); P1014 in 90209-1022 Appendix 4, printed without its code). A question is a line of the captured form
    ''' (0:Change to comment and continue, 1:Delete program and abort) (captures (c), (n), (o); §5.3 gives it as
    ''' "The step format is incorrect (0: Continue load 1: Delete program and exit)"). An error line or question
    ''' followed by the > prompt is a refusal, as (P2016)Cannot open the file. is in capture (j) (P2016 in
    ''' Appendix 4).
    ''' </para>
    ''' </summary>
    Public NotInheritable Class LoadLineRule

        Private Shared ReadOnly LoadLineRedraw As New Regex(">load \S+$", RegexOptions.CultureInvariant)

        Private Shared ReadOnly LoadingLine As New Regex("^Loading\.\.\.\(.+\)$", RegexOptions.CultureInvariant)

        Private Shared ReadOnly ProgramLine As New Regex("^Program  (\S+)\(\)$", RegexOptions.CultureInvariant)

        Private Shared ReadOnly CompletionLine As New Regex("^File load completed\. \((\d+) errors\)$", RegexOptions.CultureInvariant)

        Private Shared ReadOnly QuestionLine As New Regex("^\(\d:[^,()]+, \d:[^,()]+\)$", RegexOptions.CultureInvariant)

        Private Shared ReadOnly Choice As New Regex("(\d):([^,()]+)", RegexOptions.CultureInvariant)

        Private Sub New()
        End Sub

        ''' <summary>What a line is.</summary>
        ''' <param name="line">The line, without its 0d 0a.</param>
        ''' <param name="answer">The answer the person last typed, without Enter, or Nothing; the controller's echo of it is normal.</param>
        ''' <param name="followedByPrompt">True when the > prompt came after the line.</param>
        ''' <returns>Normal, ErrorLine, Question, or Refusal for an error line or question the prompt follows.</returns>
        Public Shared Function Verdict(line As String, answer As String, followedByPrompt As Boolean) As LoadLineVerdict
            If IsNormal(line, answer) Then Return LoadLineVerdict.Normal
            If followedByPrompt Then Return LoadLineVerdict.Refusal
            If QuestionLine.IsMatch(line) Then Return LoadLineVerdict.Question
            Return LoadLineVerdict.ErrorLine
        End Function

        ''' <summary>
        ''' The controller's label for an answer, as its question printed it: the text after the answer and its
        ''' colon, up to the next choice or the closing parenthesis.
        ''' </summary>
        ''' <param name="question">The question line.</param>
        ''' <param name="answer">The answer the person typed, without Enter.</param>
        ''' <returns>The label, or Nothing when the line is not a question or the answer matches no choice.</returns>
        Public Shared Function LabelFor(question As String, answer As String) As String
            If question Is Nothing OrElse answer Is Nothing OrElse Not QuestionLine.IsMatch(question) Then Return Nothing
            For Each found As Match In Choice.Matches(question)
                If found.Groups(1).Value = answer Then Return found.Groups(2).Value
            Next
            Return Nothing
        End Function

        ''' <summary>The program a Program  name() line names, as the controller reported it (FR-028).</summary>
        ''' <param name="line">The line.</param>
        ''' <returns>The name, or Nothing when the line is not a program line.</returns>
        Public Shared Function ProgramName(line As String) As String
            Dim found As Match = ProgramLine.Match(line)
            Return If(found.Success, found.Groups(1).Value, Nothing)
        End Function

        ''' <summary>The controller's error count in File load completed. (n errors) (FR-017).</summary>
        ''' <param name="line">The line.</param>
        ''' <returns>The count, or Nothing when the line is not the completion line.</returns>
        Public Shared Function ErrorCount(line As String) As Integer?
            Dim found As Match = CompletionLine.Match(line)
            If Not found.Success Then Return Nothing
            Return Integer.Parse(found.Groups(1).Value, NumberStyles.None, CultureInfo.InvariantCulture)
        End Function

        Private Shared Function IsNormal(line As String, answer As String) As Boolean
            Return line.Length = 0 OrElse
                   (answer IsNot Nothing AndAlso line = answer) OrElse
                   LoadLineRedraw.IsMatch(line) OrElse
                   LoadingLine.IsMatch(line) OrElse
                   ProgramLine.IsMatch(line) OrElse
                   CompletionLine.IsMatch(line)
        End Function

    End Class

End Namespace
