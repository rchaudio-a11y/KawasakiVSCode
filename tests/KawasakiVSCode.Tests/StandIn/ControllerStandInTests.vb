' File:        ControllerStandInTests.vb
' Project:     KawasakiVSCode.Tests
' Description: The stand-in replays a capture byte for byte over a real socket to a raw TcpClient,
'              reports a wrong client byte, stops silent or closed on request, plays slices, and
'              records client bytes after the end (Article V, contracts/capture-format.md).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Linq
Imports System.Net.Sockets
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace StandIn

    ''' <summary>The stand-in, driven by a raw TcpClient (Article V; User Story 1 scenario 2).</summary>
    <TestClass>
    Public Class ControllerStandInTests

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(5)

        ''' <summary>Sending capture (a)'s client bytes, one record at a time, receives its controller bytes byte for byte.</summary>
        <TestMethod>
        Public Sub ReplaysCaptureARecordByRecord()
            Dim records As IReadOnlyList(Of CaptureRecord) = CaptureReader.Read("a-connect-login")
            Using standIn As ControllerStandIn = ControllerStandIn.Play("a-connect-login").Start()
                Using client As TcpClient = Connect(standIn)
                    Dim stream As NetworkStream = client.GetStream()
                    For Each record As CaptureRecord In records
                        If record.Direction = "C"c Then
                            stream.Write(record.Bytes, 0, record.Bytes.Length)
                        Else
                            CollectionAssert.AreEqual(record.Bytes, ReadExactly(stream, record.Bytes.Length), "record " & record.Number.ToString())
                        End If
                    Next
                End Using
                Assert.IsTrue(standIn.WaitForEnd(Patience))
            End Using
        End Sub

        ''' <summary>Client bytes in other chunks than the capture's still get every controller byte, in order.</summary>
        <TestMethod>
        Public Sub ReplaysCaptureAWhateverTheChunking()
            Dim records As IReadOnlyList(Of CaptureRecord) = CaptureReader.Read("a-connect-login")
            Dim controllerBytes As Byte() = records.Where(Function(record As CaptureRecord) record.Direction = "S"c).SelectMany(Function(record As CaptureRecord) record.Bytes).ToArray()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("a-connect-login").Start()
                Using client As TcpClient = Connect(standIn)
                    Dim stream As NetworkStream = client.GetStream()
                    stream.Write(records(0).Bytes, 0, records(0).Bytes.Length)
                    Dim login As Byte() = {&H61, &H73, &HD, &HA}
                    stream.Write(login, 0, login.Length)
                    CollectionAssert.AreEqual(controllerBytes, ReadExactly(stream, controllerBytes.Length))
                End Using
                Assert.IsTrue(standIn.WaitForEnd(Patience))
            End Using
        End Sub

        ''' <summary>A wrong client byte is reported with its record, the expected bytes and the bytes received.</summary>
        <TestMethod>
        Public Sub AWrongClientByteIsReported()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("a-connect-login").ExpectMismatch().Start()
                Using client As TcpClient = Connect(standIn)
                    Dim wrong As Byte() = {&HFF, &HFB, &H19}
                    client.GetStream().Write(wrong, 0, wrong.Length)
                    Assert.IsTrue(standIn.WaitForEnd(Patience))
                End Using
                Assert.AreEqual(1, standIn.Failures.Count)
                StringAssert.Contains(standIn.Failures(0), "record 1")
                StringAssert.Contains(standIn.Failures(0), "ff fb 18")
                StringAssert.Contains(standIn.Failures(0), "ff fb 19")
            End Using
        End Sub

        ''' <summary>Stopped silent at record 2, the stand-in sends nothing and holds the connection open.</summary>
        <TestMethod>
        Public Sub StopsSilentAndHoldsTheConnection()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("a-connect-login").StopAt(2, StandInFault.Silent).Start()
                Using client As TcpClient = Connect(standIn)
                    Dim stream As NetworkStream = client.GetStream()
                    stream.Write(New Byte() {&HFF, &HFB, &H18}, 0, 3)
                    stream.ReadTimeout = 300
                    Dim buffer(15) As Byte
                    Dim read As Integer = -1
                    Try
                        read = stream.Read(buffer, 0, buffer.Length)
                    Catch ex As IOException
                    End Try
                    Assert.AreEqual(-1, read, "the stand-in sent bytes (a count) or closed (0) instead of staying silent")
                End Using
            End Using
        End Sub

        ''' <summary>Stopped closed at record 2, the stand-in closes the connection.</summary>
        <TestMethod>
        Public Sub StopsClosed()
            Using standIn As ControllerStandIn = ControllerStandIn.Play("a-connect-login").StopAt(2, StandInFault.Close).Start()
                Using client As TcpClient = Connect(standIn)
                    Dim stream As NetworkStream = client.GetStream()
                    stream.Write(New Byte() {&HFF, &HFB, &H18}, 0, 3)
                    stream.ReadTimeout = 5000
                    Dim buffer(15) As Byte
                    Assert.AreEqual(0, stream.Read(buffer, 0, buffer.Length))
                End Using
            End Using
        End Sub

        ''' <summary>Records 1 to 8 of capture (f) followed by records 19 and 20 play as one capture.</summary>
        <TestMethod>
        Public Sub PlaysASlice()
            Dim records As IReadOnlyList(Of CaptureRecord) = CaptureReader.Read("f-typed-save-load")
            Using standIn As ControllerStandIn = ControllerStandIn.PlaySlice("f-typed-save-load", New RecordRange(1, 8), New RecordRange(19, 20)).Start()
                Using client As TcpClient = Connect(standIn)
                    Dim stream As NetworkStream = client.GetStream()
                    For Each record As CaptureRecord In records.Where(Function(item As CaptureRecord) item.Number <= 8 OrElse (item.Number >= 19 AndAlso item.Number <= 20))
                        If record.Direction = "C"c Then
                            stream.Write(record.Bytes, 0, record.Bytes.Length)
                        Else
                            CollectionAssert.AreEqual(record.Bytes, ReadExactly(stream, record.Bytes.Length), "record " & record.Number.ToString())
                        End If
                    Next
                End Using
                Assert.IsTrue(standIn.WaitForEnd(Patience))
            End Using
        End Sub

        ''' <summary>Client bytes after the capture's last record are recorded, not compared.</summary>
        <TestMethod>
        Public Sub RecordsClientBytesAfterTheEnd()
            Dim records As IReadOnlyList(Of CaptureRecord) = CaptureReader.Read("a-connect-login")
            Dim extra As Byte() = Encoding.ASCII.GetBytes("hello" & vbCrLf)
            Using standIn As ControllerStandIn = ControllerStandIn.Play("a-connect-login").AllowLine("hello").Start()
                Using client As TcpClient = Connect(standIn)
                    Dim stream As NetworkStream = client.GetStream()
                    For Each record As CaptureRecord In records
                        If record.Direction = "C"c Then
                            stream.Write(record.Bytes, 0, record.Bytes.Length)
                        Else
                            ReadExactly(stream, record.Bytes.Length)
                        End If
                    Next
                    stream.Write(extra, 0, extra.Length)
                End Using
                Assert.IsTrue(standIn.WaitForEnd(Patience))
                CollectionAssert.AreEqual(extra, standIn.ReceivedAfterEnd)
            End Using
        End Sub

        ''' <summary>Client bytes after a silent stop are recorded, not compared.</summary>
        <TestMethod>
        Public Sub RecordsClientBytesAfterASilentStop()
            Dim extra As Byte() = Encoding.ASCII.GetBytes("1" & vbCrLf)
            Using standIn As ControllerStandIn = ControllerStandIn.Play("a-connect-login").StopAt(2, StandInFault.Silent).AllowLine("1").Start()
                Using client As TcpClient = Connect(standIn)
                    Dim stream As NetworkStream = client.GetStream()
                    stream.Write(New Byte() {&HFF, &HFB, &H18}, 0, 3)
                    stream.Write(extra, 0, extra.Length)
                End Using
                Assert.IsTrue(standIn.WaitForEnd(Patience))
                CollectionAssert.AreEqual(extra, standIn.ReceivedAfterEnd)
            End Using
        End Sub

        Private Shared Function Connect(standIn As ControllerStandIn) As TcpClient
            Dim client As New TcpClient()
            client.Connect("127.0.0.1", standIn.Port)
            Return client
        End Function

        Private Shared Function ReadExactly(stream As NetworkStream, count As Integer) As Byte()
            stream.ReadTimeout = 5000
            Dim buffer(count - 1) As Byte
            Dim filled As Integer = 0
            Do While filled < count
                Dim read As Integer = stream.Read(buffer, filled, count - filled)
                If read = 0 Then Throw New IOException("closed after " & filled.ToString() & " of " & count.ToString() & " bytes")
                filled += read
            Loop
            Return buffer
        End Function

    End Class

End Namespace
