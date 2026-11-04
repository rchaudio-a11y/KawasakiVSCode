' File:        ControllerConnection.vb
' Project:     KawasakiVSCode.Core
' Description: The TCP connection to one controller: every read has its step's timeout, and bytes leave
'              khi only through the telnet negotiation and Send (Article III, Article X, research R15).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Globalization
Imports System.IO
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports KawasakiVSCode.Core.Commands
Imports KawasakiVSCode.Core.Reporting
Imports KawasakiVSCode.Core.Robots

Namespace Connection

    ''' <summary>
    ''' The connection to one controller. Every read goes through ReadUntil with a named WaitStep and that
    ''' step's timeout on each read (Article X). Bytes leave khi only through Negotiate, which sends the one
    ''' telnet offer capture (a) shows, and Send, which takes only a ControllerCommand (Article III). Framing
    ''' and pass-through content get their doors with the code that sends them (User Story 2).
    ''' </summary>
    Public NotInheritable Class ControllerConnection
        Implements IDisposable

        Private Shared ReadOnly LineEnd As Byte() = {&HD, &HA}

        Private ReadOnly _robot As RobotEntry
        Private ReadOnly _timeouts As Timeouts
        Private ReadOnly _client As TcpClient
        Private ReadOnly _stream As NetworkStream
        Private ReadOnly _pending As New List(Of Byte)()
        Private ReadOnly _buffer(8191) As Byte

        Private Sub New(robot As RobotEntry, timeouts As Timeouts, client As TcpClient)
            _robot = robot
            _timeouts = timeouts
            _client = client
            _stream = client.GetStream()
        End Sub

        ''' <summary>Connects to a robot, within the Connect step's timeout.</summary>
        ''' <param name="robot">The robot RobotSelection.Resolve picked.</param>
        ''' <param name="timeouts">The timeout for each wait step.</param>
        ''' <returns>The open connection.</returns>
        ''' <exception cref="KhiRefusal">The connect timed out (exit 3), or failed or was refused (exit 4).</exception>
        Public Shared Function Connect(robot As RobotEntry, timeouts As Timeouts) As ControllerConnection
            Dim where As String = robot.Address & ":" & robot.Port.ToString(CultureInfo.InvariantCulture)
            Dim client As New TcpClient() With {.NoDelay = True}
            Try
                Using cancel As New CancellationTokenSource(timeouts.ValueFor(WaitStep.Connect))
                    client.ConnectAsync(robot.Address, robot.Port, cancel.Token).AsTask().GetAwaiter().GetResult()
                End Using
            Catch ex As OperationCanceledException
                client.Dispose()
                Throw TimedOut(WaitStep.Connect, where, robot)
            Catch ex As SocketException
                client.Dispose()
                Throw New KhiRefusal(ExitCode.ConnectionFailed, "khi: cannot connect to robot " & robot.Name & " at " & where & ": " & ex.Message)
            End Try
            Return New ControllerConnection(robot, timeouts, client)
        End Function

        ''' <summary>
        ''' Plays the telnet negotiation capture (a) shows: sends TelnetNegotiator.Offer and waits, in the
        ''' Negotiation step, for TelnetNegotiator.Answer.
        ''' </summary>
        ''' <returns>The controller's bytes read, up to and including the answer.</returns>
        ''' <exception cref="KhiRefusal">The wait timed out (exit 3), or the controller closed the connection (exit 4).</exception>
        Public Function Negotiate() As Byte()
            Write(TelnetNegotiator.Offer)
            Return ReadUntil(WaitStep.Negotiation, Nothing, TelnetNegotiator.Answer)
        End Function

        ''' <summary>
        ''' Sends a command as its capture shows, and reads the controller's reply. The login is sent as the line
        ''' and 0d 0a, and its reply ends at 0d 0a and the prompt (capture (a)). A LOAD or SAVE is sent as the
        ''' line; after its echo, 18, the line again and 0d 0a; and its reply ends at > and the line and 0d 0a
        ''' (D1, captures (b), (d), (i)).
        ''' </summary>
        ''' <param name="command">The command; nothing else is ever sent as one (Article III).</param>
        ''' <returns>The controller's bytes read.</returns>
        ''' <exception cref="KhiRefusal">A wait timed out (exit 3), or the controller closed the connection (exit 4).</exception>
        Public Function Send(command As ControllerCommand) As Byte()
            Dim line As Byte() = Encoding.ASCII.GetBytes(command.Line)
            If command.Kind = ControllerCommandKind.Login Then
                Write(Join(line, LineEnd))
                Return ReadUntil(WaitStep.LoginReply, Nothing, Join(LineEnd, New Byte() {&H3E}))
            End If
            Write(line)
            Dim echo As Byte() = ReadUntil(WaitStep.CommandReply, command.Line, line)
            Write(Join(New Byte() {&H18}, line, LineEnd))
            Dim redraw As Byte() = ReadUntil(WaitStep.CommandReply, command.Line, Join(New Byte() {&H3E}, line, LineEnd))
            Return Join(echo, redraw)
        End Function

        ''' <summary>
        ''' Reads until the controller has sent a given run of bytes, with the step's timeout on every read, so
        ''' a controller that keeps sending never times out and one that stops does. Bytes after the run are
        ''' kept for the next read.
        ''' </summary>
        ''' <param name="item">The step being waited in, named in a timeout message.</param>
        ''' <param name="detail">The detail Timeouts.Describe takes for the step, or Nothing.</param>
        ''' <param name="until">The run of bytes that ends the wait.</param>
        ''' <returns>The bytes read, up to and including the run.</returns>
        ''' <exception cref="KhiRefusal">The step timed out (exit 3), or the controller closed the connection (exit 4).</exception>
        Public Function ReadUntil(item As WaitStep, detail As String, until As Byte()) As Byte()
            Dim searched As Integer = 0
            Do
                Dim at As Integer = Find(until, searched)
                If at >= 0 Then
                    Dim count As Integer = at + until.Length
                    Dim result As Byte() = _pending.GetRange(0, count).ToArray()
                    _pending.RemoveRange(0, count)
                    Return result
                End If
                searched = Math.Max(0, _pending.Count - until.Length + 1)
                _stream.ReadTimeout = CInt(Math.Min(Integer.MaxValue, Math.Max(1.0, _timeouts.ValueFor(item).TotalMilliseconds)))
                Dim read As Integer
                Try
                    read = _stream.Read(_buffer, 0, _buffer.Length)
                Catch ex As IOException When IsTimeout(ex)
                    Throw TimedOut(item, detail, _robot)
                Catch ex As IOException
                    Throw Closed()
                End Try
                If read = 0 Then Throw Closed()
                For index As Integer = 0 To read - 1
                    _pending.Add(_buffer(index))
                Next
            Loop
        End Function

        ''' <summary>Closes the connection.</summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            _client.Dispose()
        End Sub

        Private Sub Write(bytes As Byte())
            Try
                _stream.Write(bytes, 0, bytes.Length)
            Catch ex As IOException
                Throw Closed()
            End Try
        End Sub

        Private Function Find(run As Byte(), from As Integer) As Integer
            For start As Integer = from To _pending.Count - run.Length
                Dim found As Boolean = True
                For index As Integer = 0 To run.Length - 1
                    If _pending(start + index) <> run(index) Then
                        found = False
                        Exit For
                    End If
                Next
                If found Then Return start
            Next
            Return -1
        End Function

        Private Shared Function TimedOut(item As WaitStep, detail As String, robot As RobotEntry) As KhiRefusal
            Return New KhiRefusal(ExitCode.TimedOut, "khi: timed out waiting for " & Timeouts.Describe(item, detail) & " (robot " & robot.Name & ")")
        End Function

        Private Function Closed() As KhiRefusal
            Return New KhiRefusal(ExitCode.ConnectionFailed, "khi: robot " & _robot.Name & " closed the connection")
        End Function

        Private Shared Function IsTimeout(ex As IOException) As Boolean
            Dim inner As SocketException = TryCast(ex.InnerException, SocketException)
            Return inner IsNot Nothing AndAlso inner.SocketErrorCode = SocketError.TimedOut
        End Function

        Private Shared Function Join(ParamArray parts As Byte()()) As Byte()
            Dim joined As New List(Of Byte)()
            For Each part As Byte() In parts
                joined.AddRange(part)
            Next
            Return joined.ToArray()
        End Function

    End Class

End Namespace
