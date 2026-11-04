' File:        ControllerStandIn.vb
' Project:     KawasakiVSCode.Tests
' Description: Replays one capture over a real socket to one client, checks the client's bytes against
'              the capture, and records its commands (Article V, contracts/capture-format.md).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace StandIn

    ''' <summary>
    ''' A TcpListener on 127.0.0.1, on a port the system assigns, that plays one capture against one client
    ''' (Article V, contracts/capture-format.md). Every byte it sends comes from the capture; no member
    ''' takes controller bytes from anywhere else (INV-3). Dispose fails the test on any mismatch the test did
    ''' not expect, and on any command outside the allowed set (INV-2).
    ''' </summary>
    Public NotInheritable Class ControllerStandIn
        Implements IDisposable

        Private Const Etb As Byte = &H17

        Private ReadOnly _capture As String
        Private ReadOnly _records As IReadOnlyList(Of CaptureRecord)
        Private ReadOnly _gate As New Object()
        Private ReadOnly _failures As New List(Of String)()
        Private ReadOnly _afterEnd As New List(Of Byte)()
        Private ReadOnly _allowed As New List(Of String)()
        Private ReadOnly _recorder As New CommandRecorder()
        Private ReadOnly _ended As New ManualResetEventSlim(False)

        ' Payload regions: record number to the fixture's index and the region's first byte, counted from 1.
        Private ReadOnly _payloadFixture As New Dictionary(Of Integer, Integer)()
        Private ReadOnly _payloadFirstByte As New Dictionary(Of Integer, Integer)()
        Private ReadOnly _fixtures As New List(Of Byte())()
        Private ReadOnly _fixtureReceived As New List(Of List(Of Byte))()

        ' Typed regions: the first client record of the region expects the test's input; the others expect nothing.
        Private ReadOnly _typedInput As New Dictionary(Of Integer, Byte())()
        Private ReadOnly _typedSkip As New HashSet(Of Integer)()

        Private _stopRecord As Integer
        Private _stopFault As StandInFault
        Private _expectMismatch As Boolean
        Private _listener As TcpListener
        Private _client As TcpClient
        Private _stream As NetworkStream
        Private _task As Task
        Private _disposing As Boolean
        Private ReadOnly _buffer(65535) As Byte
        Private _position As Integer
        Private _length As Integer
        Private _port As Integer

        Private Sub New(capture As String, records As IReadOnlyList(Of CaptureRecord))
            _capture = capture
            _records = records
        End Sub

        ''' <summary>Plays a whole capture.</summary>
        ''' <param name="capture">The capture's folder name under Captures/.</param>
        ''' <returns>The stand-in, not yet started.</returns>
        Public Shared Function Play(capture As String) As ControllerStandIn
            Return New ControllerStandIn(capture, CaptureReader.Read(capture))
        End Function

        ''' <summary>Plays ranges of a capture's records, in the order given, as one capture (D4).</summary>
        ''' <param name="capture">The capture's folder name under Captures/.</param>
        ''' <param name="ranges">The record ranges, numbered as the capture's note numbers them.</param>
        ''' <returns>The stand-in, not yet started.</returns>
        Public Shared Function PlaySlice(capture As String, ParamArray ranges As RecordRange()) As ControllerStandIn
            Dim all As IReadOnlyList(Of CaptureRecord) = CaptureReader.Read(capture)
            Dim chosen As New List(Of CaptureRecord)()
            For Each range As RecordRange In ranges
                chosen.AddRange(all.Where(Function(record As CaptureRecord) range.Contains(record.Number)))
            Next
            Return New ControllerStandIn(capture, chosen.AsReadOnly())
        End Function

        ''' <summary>Cuts the capture short at a record: nothing from that record on is sent or compared.</summary>
        ''' <param name="record">The record to stop at.</param>
        ''' <param name="fault">Silent, or Close.</param>
        ''' <returns>This stand-in.</returns>
        Public Function StopAt(record As Integer, fault As StandInFault) As ControllerStandIn
            _stopRecord = record
            _stopFault = fault
            Return Me
        End Function

        ''' <summary>Says the test expects a mismatch, so Dispose does not fail on one and fails if there is none.</summary>
        ''' <returns>This stand-in.</returns>
        Public Function ExpectMismatch() As ControllerStandIn
            _expectMismatch = True
            Return Me
        End Function

        ''' <summary>Adds a line the test typed, such as an answer at a load question, to the allowed set (INV-2).</summary>
        ''' <param name="line">The line, without its line ending.</param>
        ''' <returns>This stand-in.</returns>
        Public Function AllowLine(line As String) As ControllerStandIn
            _allowed.Add(line)
            Return Me
        End Function

        ''' <summary>
        ''' Marks payload regions (INV-1): in each record named, from the first byte given to the block's
        ''' closing 17, the client's bytes are compared with the fixture, the regions taken in order, instead
        ''' of with the captured file.
        ''' </summary>
        ''' <param name="fixture">The file content the regions carry, taken together.</param>
        ''' <param name="firstByte">Where each region starts in its record, counted from 1 as the notes count it.</param>
        ''' <param name="records">The client records holding the regions.</param>
        ''' <returns>This stand-in.</returns>
        Public Function Payload(fixture As Byte(), firstByte As Integer, ParamArray records As Integer()) As ControllerStandIn
            Dim index As Integer = _fixtures.Count
            _fixtures.Add(fixture)
            _fixtureReceived.Add(New List(Of Byte)())
            For Each number As Integer In records
                _payloadFixture(number) = index
                _payloadFirstByte(number) = firstByte
            Next
            Return Me
        End Function

        ''' <summary>Marks a typed region: its client records are compared with the test's own input instead of the captured bytes.</summary>
        ''' <param name="range">The records the person typed.</param>
        ''' <param name="input">The bytes the test's input makes khi send there.</param>
        ''' <returns>This stand-in.</returns>
        Public Function Typed(range As RecordRange, input As Byte()) As ControllerStandIn
            Dim clientRecords As List(Of CaptureRecord) = _records.Where(Function(record As CaptureRecord) record.Direction = "C"c AndAlso range.Contains(record.Number)).ToList()
            If clientRecords.Count = 0 Then Throw New ArgumentException("the range holds no client record", NameOf(range))
            _typedInput(clientRecords(0).Number) = input
            For Each record As CaptureRecord In clientRecords.Skip(1)
                _typedSkip.Add(record.Number)
            Next
            Return Me
        End Function

        ''' <summary>Starts listening and serves one client.</summary>
        ''' <returns>This stand-in.</returns>
        Public Function Start() As ControllerStandIn
            _listener = New TcpListener(IPAddress.Loopback, 0)
            _listener.Start()
            _port = DirectCast(_listener.LocalEndpoint, IPEndPoint).Port
            _task = Task.Factory.StartNew(AddressOf Serve, TaskCreationOptions.LongRunning)
            Return Me
        End Function

        ''' <summary>The port the stand-in listens on.</summary>
        Public ReadOnly Property Port As Integer
            Get
                Return _port
            End Get
        End Property

        ''' <summary>The mismatches seen, each naming the record, the expected bytes and the bytes received.</summary>
        Public ReadOnly Property Failures As IReadOnlyList(Of String)
            Get
                SyncLock _gate
                    Return _failures.ToArray()
                End SyncLock
            End Get
        End Property

        ''' <summary>Client bytes after the capture's last record, or after a stop: recorded, not compared.</summary>
        Public ReadOnly Property ReceivedAfterEnd As Byte()
            Get
                SyncLock _gate
                    Return _afterEnd.ToArray()
                End SyncLock
            End Get
        End Property

        ''' <summary>Waits until the play has ended: the client closed after the last record or a stop, or a mismatch closed the connection.</summary>
        ''' <param name="timeout">How long to wait.</param>
        ''' <returns>True when the play ended in time.</returns>
        Public Function WaitForEnd(timeout As TimeSpan) As Boolean
            Return _ended.Wait(timeout)
        End Function

        ''' <summary>Stops the stand-in and fails the test on an unexpected mismatch or a command outside the allowed set (INV-2).</summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            If _disposing Then Return
            _disposing = True
            If _listener IsNot Nothing Then _listener.Stop()
            SyncLock _gate
                If _client IsNot Nothing Then _client.Close()
            End SyncLock
            If _task IsNot Nothing Then _task.Wait(TimeSpan.FromSeconds(5))
            Dim seen As IReadOnlyList(Of String) = Failures
            If _expectMismatch Then
                If seen.Count = 0 Then Assert.Fail("capture " & _capture & ": a mismatch was expected and none was seen")
            ElseIf seen.Count > 0 Then
                Assert.Fail("capture " & _capture & ": " & String.Join("; ", seen))
            End If
            Dim outside As IReadOnlyList(Of String) = _recorder.Disallowed(_allowed)
            If outside.Count > 0 Then
                Assert.Fail("capture " & _capture & ": commands outside the allowed set (INV-2): " & String.Join(", ", outside))
            End If
        End Sub

        Private Sub Serve()
            Dim accepted As TcpClient = Nothing
            Try
                accepted = _listener.AcceptTcpClient()
                SyncLock _gate
                    _client = accepted
                End SyncLock
                _stream = accepted.GetStream()
                If PlayRecords(accepted) Then ReadToTheEnd()
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is SocketException OrElse TypeOf ex Is ObjectDisposedException OrElse TypeOf ex Is InvalidOperationException
                If Not _disposing Then Fail("the connection failed: " & ex.Message)
            Finally
                If accepted IsNot Nothing Then accepted.Close()
                _ended.Set()
            End Try
        End Sub

        Private Function PlayRecords(accepted As TcpClient) As Boolean
            For Each record As CaptureRecord In _records
                If record.Number = _stopRecord Then
                    If _stopFault = StandInFault.Close Then accepted.Client.Shutdown(SocketShutdown.Send)
                    Return True
                End If
                If record.Direction = "S"c Then
                    _stream.Write(record.Bytes, 0, record.Bytes.Length)
                ElseIf Not Expect(record) Then
                    Return False
                End If
            Next
            For index As Integer = 0 To _fixtures.Count - 1
                If _fixtureReceived(index).Count <> _fixtures(index).Length Then
                    Fail("payload: the fixture holds " & Num(_fixtures(index).Length) & " bytes and the client sent " & Num(_fixtureReceived(index).Count))
                    Return False
                End If
            Next
            Return True
        End Function

        Private Function Expect(record As CaptureRecord) As Boolean
            If _typedSkip.Contains(record.Number) Then Return True
            Dim typedBytes As Byte() = Nothing
            If _typedInput.TryGetValue(record.Number, typedBytes) Then Return Match(record.Number, typedBytes, 0, typedBytes.Length)
            Dim expected As Byte() = record.Bytes
            Dim firstByte As Integer
            If Not _payloadFirstByte.TryGetValue(record.Number, firstByte) Then Return Match(record.Number, expected, 0, expected.Length)
            Dim closing As Integer = Array.LastIndexOf(expected, Etb)
            If closing < firstByte - 1 Then
                Fail("record " & Num(record.Number) & ": the payload region starts at byte " & Num(firstByte) & " but the record's closing 17 comes before it")
                Return False
            End If
            If Not Match(record.Number, expected, 0, firstByte - 1) Then Return False
            If Not TakePayload(record.Number, _payloadFixture(record.Number)) Then Return False
            Return Match(record.Number, expected, closing, expected.Length - closing)
        End Function

        Private Function Match(number As Integer, expected As Byte(), offset As Integer, count As Integer) As Boolean
            Dim received As New List(Of Byte)()
            For index As Integer = 0 To count - 1
                Dim value As Integer = NextByte()
                If value < 0 Then
                    Fail("record " & Num(number) & ": expected " & HexOf(expected, offset, count) & ", but the client closed the connection after " & HexOf(received.ToArray(), 0, received.Count))
                    Return False
                End If
                received.Add(CByte(value))
                If value <> expected(offset + index) Then
                    For extra As Integer = _position To Math.Min(_length, _position + count - index - 1) - 1
                        received.Add(_buffer(extra))
                    Next
                    Dim start As Integer = Math.Max(0, index - 8)
                    Fail("record " & Num(number) & ", byte " & Num(offset + index + 1) & ": expected " &
                         HexOf(expected, offset + start, Math.Min(24, count - start)) & ", received " &
                         HexOf(received.ToArray(), start, Math.Min(24, received.Count - start)))
                    Return False
                End If
            Next
            Return True
        End Function

        Private Function TakePayload(number As Integer, fixtureIndex As Integer) As Boolean
            Dim fixture As Byte() = _fixtures(fixtureIndex)
            Dim received As List(Of Byte) = _fixtureReceived(fixtureIndex)
            Dim regionStart As Integer = received.Count
            Do
                Dim value As Integer = PeekByte()
                If value < 0 Then
                    Fail("record " & Num(number) & ": the client closed the connection inside the payload region")
                    Return False
                End If
                If value = Etb Then Exit Do
                received.Add(CByte(NextByte()))
            Loop
            For index As Integer = regionStart To received.Count - 1
                If index >= fixture.Length OrElse received(index) <> fixture(index) Then
                    Dim start As Integer = Math.Max(0, index - 8)
                    Fail("record " & Num(number) & ": the payload differs from the fixture at byte " & Num(index + 1) & " of the file: expected " &
                         HexOf(fixture, start, Math.Max(0, Math.Min(24, fixture.Length - start))) & ", received " &
                         HexOf(received.ToArray(), start, Math.Min(24, received.Count - start)))
                    Return False
                End If
            Next
            Return True
        End Function

        Private Sub ReadToTheEnd()
            SyncLock _gate
                For index As Integer = _position To _length - 1
                    _afterEnd.Add(_buffer(index))
                Next
            End SyncLock
            _position = _length
            Do
                Dim read As Integer = _stream.Read(_buffer, 0, _buffer.Length)
                If read = 0 Then Exit Do
                _recorder.Add(_buffer, 0, read)
                SyncLock _gate
                    For index As Integer = 0 To read - 1
                        _afterEnd.Add(_buffer(index))
                    Next
                End SyncLock
            Loop
        End Sub

        Private Function PeekByte() As Integer
            If _position >= _length Then
                Dim read As Integer = _stream.Read(_buffer, 0, _buffer.Length)
                If read = 0 Then Return -1
                _recorder.Add(_buffer, 0, read)
                _position = 0
                _length = read
            End If
            Return _buffer(_position)
        End Function

        Private Function NextByte() As Integer
            Dim value As Integer = PeekByte()
            If value >= 0 Then _position += 1
            Return value
        End Function

        Private Sub Fail(message As String)
            SyncLock _gate
                _failures.Add(message)
            End SyncLock
        End Sub

        Private Shared Function HexOf(bytes As Byte(), offset As Integer, count As Integer) As String
            If count <= 0 Then Return "(nothing)"
            Return String.Join(" ", bytes.Skip(offset).Take(count).Select(Function(value As Byte) value.ToString("x2", CultureInfo.InvariantCulture)))
        End Function

        Private Shared Function Num(value As Integer) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function

    End Class

End Namespace
