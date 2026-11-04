' File:        LoadOperation.vb
' Project:     KawasakiVSCode.Core
' Description: The PC end of a LOAD on a logged-in connection: the command, one block per controller
'              request, the controller's text to stdout unaltered, a question handed to the person, and
'              the outcome (FR-016 to FR-021, captures (b), (f), (i), (l), (n), (o), (p), (q), (r), (s)).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Text
Imports KawasakiVSCode.Core.Commands
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Core.Reporting
Imports KawasakiVSCode.Core.Terminal

Namespace Transfer

    ''' <summary>
    ''' The PC end of one LOAD, as KRterm plays it in the captures (research R2).
    ''' <list type="bullet">
    ''' <item>Start: the command (D1); the A block naming the file is answered A    0.</item>
    ''' <item>Blocks: each controller C request gets the next block of up to 512 file bytes; a block shorter than
    ''' 512, an empty one included, is the last, and the next request gets the end-of-file block (FR-016;
    ''' captures (b), (l), (q), (r), (s)). A question comes with a request, which is answered the same way
    ''' (capture (p)).</item>
    ''' <item>End: the E block is answered E    0, then File load completed. (n errors) and the > prompt.</item>
    ''' <item>Output: the controller's text, every byte but its blocks, goes to stdout unaltered as it comes.</item>
    ''' <item>A question (D3) waits on the person without limit (Article X), and the line typed is sent as typed
    ''' (FR-021). An error line followed by the > prompt is a refusal (H1). An error line followed by the
    ''' controller going quiet for its wait's timeout, with no prompt, goes to the person the same way (D3).</item>
    ''' <item>Waits: every other wait is the step's, bounded by its timeout.</item>
    ''' </list>
    ''' LoadLineRule says what each line is; TransferFraming reads and builds the blocks; PersonInput gives the
    ''' person's line.
    ''' </summary>
    Public NotInheritable Class LoadOperation

        Private Const Lf As Byte = &HA
        Private Const Prompt As String = ">"

        Private ReadOnly _connection As ControllerConnection
        Private ReadOnly _content As Byte()
        Private ReadOnly _person As PersonInput
        Private ReadOnly _output As Stream
        Private ReadOnly _framing As New TransferFraming()
        Private ReadOnly _line As New List(Of Byte)()
        Private ReadOnly _programs As New List(Of String)()
        Private ReadOnly _errorLines As New List(Of String)()

        Private _step As WaitStep
        Private _sent As Integer
        Private _lastBlockSent As Boolean
        Private _endOfFileSent As Boolean
        Private _unanswered As String
        Private _handedLine As String
        Private _answer As String
        Private _errorCount As Integer?
        Private _commandSent As Boolean
        Private _refused As Boolean
        Private _noAnswer As Boolean
        Private _ended As Boolean
        Private _atLineStart As Boolean = True

        ''' <summary>Makes the operation for one file on a logged-in connection.</summary>
        ''' <param name="connection">The connection, at the prompt after login.</param>
        ''' <param name="content">The file's bytes, sent as they are (Article II).</param>
        ''' <param name="person">The person's input, read only at a question.</param>
        ''' <param name="output">stdout, raw bytes.</param>
        Public Sub New(connection As ControllerConnection, content As Byte(), person As PersonInput, output As Stream)
            _connection = connection
            _content = content
            _person = person
            _output = output
        End Sub

        ''' <summary>
        ''' Sends the command and plays the LOAD to its end: the completion line and the prompt, a refusal at the
        ''' prompt, or the person's input ending at a question.
        ''' </summary>
        ''' <param name="command">The LOAD command, ControllerCommand.Load.</param>
        ''' <exception cref="KhiRefusal">A wait timed out (exit 3), the controller closed the connection (exit 4), or it sent a block no capture shows (exit 1).</exception>
        Public Sub Run(command As ControllerCommand)
            _commandSent = True
            WriteOut(_connection.Send(command))
            _step = WaitStep.TransferData
            Do Until _ended
                Dim chunk As Byte() = Nothing
                Dim quiet As Boolean = False
                Try
                    chunk = _connection.ReadNext(_step, Nothing)
                Catch timeout As KhiRefusal When timeout.ExitCode = ExitCode.TimedOut AndAlso _unanswered IsNot Nothing
                    quiet = True
                End Try
                If quiet Then
                    AskPerson(_unanswered)
                Else
                    _framing.Add(chunk)
                    TakeAll()
                End If
            Loop
        End Sub

        ''' <summary>True once the command has been sent, so the controller's memory may have changed (FR-020).</summary>
        Public ReadOnly Property CommandSent As Boolean
            Get
                Return _commandSent
            End Get
        End Property

        ''' <summary>True when the person's input ended, or Ctrl+] came, while a question waited (FR-021).</summary>
        Public ReadOnly Property NoAnswer As Boolean
            Get
                Return _noAnswer
            End Get
        End Property

        ''' <summary>The error lines, in order, each the controller's text without its line end (D2, FR-018).</summary>
        Public ReadOnly Property ErrorLines As IReadOnlyList(Of String)
            Get
                Return _errorLines.AsReadOnly()
            End Get
        End Property

        ''' <summary>True when everything written to stdout so far ends a line, so khi's own lines can follow.</summary>
        Public ReadOnly Property AtLineStart As Boolean
            Get
                Return _atLineStart
            End Get
        End Property

        ''' <summary>How the LOAD ended, for the result line (FR-017, FR-021, FR-028).</summary>
        ''' <param name="robot">The robot's name.</param>
        ''' <param name="filePath">The file as the person gave it.</param>
        ''' <param name="failure">The timeout or dropped connection that ended Run, or Nothing.</param>
        ''' <returns>The outcome: ok and exit 0 only on (0 errors) with no question asked.</returns>
        Public Function Outcome(robot As String, filePath As String, failure As KhiRefusal) As TransferOutcome
            Dim code As ExitCode = ExitCode.ControllerError
            Dim question As QuestionAnswer = Nothing
            If failure IsNot Nothing Then
                code = failure.ExitCode
            ElseIf Not _noAnswer AndAlso Not _refused AndAlso _errorCount.HasValue Then
                If _answer Is Nothing Then
                    If _errorCount.Value = 0 Then code = ExitCode.Success
                Else
                    Dim label As String = LoadLineRule.LabelFor(_handedLine, _answer)
                    question = New QuestionAnswer(_errorCount.Value, _answer, label, _answer = "0" AndAlso label IsNot Nothing)
                End If
            End If
            Return New TransferOutcome("load", code = ExitCode.Success, robot, filePath, _sent, _content.Length,
                                       _programs.AsReadOnly(), question, code)
        End Function

        ''' <summary>Takes the text and blocks that have come, in order.</summary>
        Private Sub TakeAll()
            Do
                Dim text As Byte() = _framing.TakeText()
                If text.Length > 0 Then
                    WriteOut(text)
                    For Each value As Byte In text
                        TakeTextByte(value)
                        If _ended Then Return
                    Next
                    If Encoding.ASCII.GetString(_line.ToArray()) = Prompt Then AtPrompt()
                    If _ended Then Return
                End If
                Dim frame As TransferFrame = _framing.TakeFrame()
                If frame Is Nothing Then Return
                AnswerFrame(frame)
            Loop
        End Sub

        Private Sub TakeTextByte(value As Byte)
            If value <> Lf Then
                _line.Add(value)
                Return
            End If
            Dim count As Integer = _line.Count
            If count > 0 AndAlso _line(count - 1) = &HD Then count -= 1
            Dim line As String = Encoding.Latin1.GetString(_line.ToArray(), 0, count)
            _line.Clear()
            TakeLine(line)
        End Sub

        Private Sub TakeLine(line As String)
            Select Case LoadLineRule.Verdict(line, _answer, followedByPrompt:=False)
                Case LoadLineVerdict.Normal
                    If line.Length > 0 Then _unanswered = Nothing
                    Dim name As String = LoadLineRule.ProgramName(line)
                    If name IsNot Nothing Then _programs.Add(name)
                    Dim count As Integer? = LoadLineRule.ErrorCount(line)
                    If count.HasValue Then _errorCount = count
                Case LoadLineVerdict.Question
                    _errorLines.Add(line)
                    _unanswered = line
                    AskPerson(line)
                Case Else
                    _errorLines.Add(line)
                    _unanswered = line
            End Select
        End Sub

        ''' <summary>The > prompt: the end after the completion line; otherwise the controller has ended the LOAD, a refusal (H1).</summary>
        Private Sub AtPrompt()
            If _errorCount.HasValue Then
                _ended = True
            ElseIf _unanswered Is Nothing OrElse LoadLineRule.Verdict(_unanswered, _answer, followedByPrompt:=True) = LoadLineVerdict.Refusal Then
                _refused = True
                _ended = True
            End If
        End Sub

        Private Sub AnswerFrame(frame As TransferFrame)
            _unanswered = Nothing
            Select Case frame.Letter
                Case "A"c
                    _connection.SendFrame(TransferFraming.Answer("A"c))
                    _step = WaitStep.TransferData
                Case "C"c
                    If _endOfFileSent Then Throw Unknown("a request after the end-of-file block")
                    If _lastBlockSent Then
                        _connection.SendFrame(TransferFraming.EndOfFile)
                        _endOfFileSent = True
                        _step = WaitStep.TransferEnd
                    Else
                        Dim block As TransferFrame = TransferFraming.DataBlock(_content, _sent)
                        _connection.SendFrame(block)
                        _sent += block.FileBytes
                        _lastBlockSent = block.FileBytes < TransferFraming.BlockSize
                        _step = WaitStep.TransferData
                    End If
                Case "E"c
                    _connection.SendFrame(TransferFraming.Answer("E"c))
                    _step = WaitStep.TransferEnd
                Case Else
                    Throw Unknown("a " & frame.Letter & " block")
            End Select
        End Sub

        ''' <summary>Hands a line to the person and sends the line they type, waiting without limit (Article X, FR-021).</summary>
        Private Sub AskPerson(line As String)
            _handedLine = line
            Dim typed As Byte() = _person.ReadLine()
            If typed Is Nothing Then
                _noAnswer = True
                _ended = True
                Return
            End If
            _connection.PassThrough(typed)
            _answer = Encoding.Latin1.GetString(typed, 0, typed.Length - 2)
        End Sub

        Private Sub WriteOut(bytes As Byte())
            If bytes.Length = 0 Then Return
            _output.Write(bytes, 0, bytes.Length)
            _output.Flush()
            _atLineStart = bytes(bytes.Length - 1) = Lf
        End Sub

        Private Shared Function Unknown(what As String) As KhiRefusal
            Return New KhiRefusal(ExitCode.ControllerError, "khi: the controller sent " & what & " during the LOAD, which no capture shows")
        End Function

    End Class

End Namespace
