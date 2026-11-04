' File:        StandInRegionTests.vb
' Project:     KawasakiVSCode.Tests
' Description: The stand-in's payload regions take a fixture of any length and report one that differs,
'              and its typed regions take the test's own input (contracts/capture-format.md, T021).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Linq
Imports System.Net.Sockets
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace StandIn

    ''' <summary>
    ''' Payload and typed regions, on capture (b): its payload is record 37, bytes 8 to 110, and its typed
    ''' region is records 9 to 31, load capb.as one character per record (the capture's note).
    ''' </summary>
    <TestClass>
    Public Class StandInRegionTests

        Private Const Capture As String = "b-load-small"

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(5)

        Private Shared ReadOnly Fixture As Byte() = Encoding.ASCII.GetBytes(".PROGRAM capb()" & vbCrLf & "  TYPE ""a shorter fixture""" & vbCrLf & ".END" & vbCrLf)

        ' Fire demonstration (Article VII), 2026-11-04: made ControllerStandIn.Expect compare a payload
        ' record with the captured bytes instead of the fixture; this test failed with "record 37, byte 27:
        ' expected" the captured file, "received" the fixture. Restored the file; it passed.
        ''' <summary>A payload region takes a fixture of another length than the captured file.</summary>
        <TestMethod>
        Public Sub APayloadRegionTakesAFixtureOfAnotherLength()
            Assert.AreNotEqual(103, Fixture.Length)
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Payload(Fixture, 8, 37).Start()
                Drive(standIn, 42, Block(Fixture), Nothing)
                Assert.IsTrue(standIn.WaitForEnd(Patience))
            End Using
        End Sub

        ' Fire demonstration (Article VII), 2026-11-04: made the payload comparison accept any byte within
        ' the fixture's length; this test failed, the stand-in reporting a later record instead of record 37.
        ' Restored the file; it passed.
        ''' <summary>A payload that differs from the fixture is reported with the record and the byte of the file.</summary>
        <TestMethod>
        Public Sub APayloadThatDiffersFromTheFixtureIsReported()
            Dim wrong As Byte() = CType(Fixture.Clone(), Byte())
            wrong(20) = CByte(AscW("X"c))
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Payload(Fixture, 8, 37).ExpectMismatch().Start()
                Drive(standIn, 37, Block(wrong), Nothing)
                Assert.IsTrue(standIn.WaitForEnd(Patience))
                Assert.AreEqual(1, standIn.Failures.Count)
                StringAssert.Contains(standIn.Failures(0), "record 37")
                StringAssert.Contains(standIn.Failures(0), "differs from the fixture at byte 21 of the file")
            End Using
        End Sub

        ' Fire demonstration (Article VII), 2026-11-04: removed the length check at the end of the play; this
        ' test failed, no mismatch being seen. Restored the file; it passed.
        ''' <summary>A payload shorter than the fixture is reported once the capture has played.</summary>
        <TestMethod>
        Public Sub APayloadShorterThanTheFixtureIsReported()
            Dim shorter As Byte() = Fixture.Take(Fixture.Length - 5).ToArray()
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Payload(Fixture, 8, 37).ExpectMismatch().Start()
                Drive(standIn, 42, Block(shorter), Nothing)
                Assert.IsTrue(standIn.WaitForEnd(Patience))
                Assert.AreEqual(1, standIn.Failures.Count)
                StringAssert.Contains(standIn.Failures(0), "the fixture holds " & Fixture.Length.ToString() & " bytes and the client sent " & shorter.Length.ToString())
            End Using
        End Sub

        ' Fire demonstration (Article VII), 2026-11-04, with the test below: made ControllerStandIn.Expect
        ' ignore the test's input and compare the captured bytes; this test failed with "record 9, byte 1:
        ' expected 6c, received 4c", and the test below failed, its mismatch not naming the test's input.
        ' Restored the file; both passed.
        ''' <summary>A typed region is compared with the test's input, which here differs from what was captured.</summary>
        <TestMethod>
        Public Sub ATypedRegionTakesTheTestsInput()
            Dim typed As Byte() = Encoding.ASCII.GetBytes("LOAD capb.as")
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Typed(New RecordRange(9, 31), typed).Start()
                Drive(standIn, 42, Nothing, typed)
                Assert.IsTrue(standIn.WaitForEnd(Patience))
            End Using
        End Sub

        ''' <summary>In a typed region, the captured bytes are a mismatch when the test's input differs from them.</summary>
        <TestMethod>
        Public Sub ATypedRegionRefusesTheCapturedBytesWhenTheInputDiffers()
            Dim typed As Byte() = Encoding.ASCII.GetBytes("LOAD capb.as")
            Using standIn As ControllerStandIn = ControllerStandIn.Play(Capture).Typed(New RecordRange(9, 31), typed).ExpectMismatch().Start()
                Drive(standIn, 9, Nothing, Nothing)
                Assert.IsTrue(standIn.WaitForEnd(Patience))
                Assert.AreEqual(1, standIn.Failures.Count)
                StringAssert.Contains(standIn.Failures(0), "record 9, byte 1: expected 4c 4f 41 44")
            End Using
        End Sub

        ''' <summary>The C block of record 37 carrying other file bytes: the captured header, the bytes, and 17.</summary>
        Private Shared Function Block(content As Byte()) As Byte()
            Dim header As Byte() = {&H2, &H43, &H20, &H20, &H20, &H20, &H30}
            Return header.Concat(content).Concat(New Byte() {&H17}).ToArray()
        End Function

        ''' <summary>
        ''' Plays the client's side of capture (b) up to a record: each C record as captured, except record 37
        ''' when a block is given, and records 9 to 31 as the typed bytes, sent at record 9, when they are given.
        ''' Each S record is read and checked against the capture.
        ''' </summary>
        Private Shared Sub Drive(standIn As ControllerStandIn, lastRecord As Integer, record37 As Byte(), typed As Byte())
            Using client As New TcpClient()
                client.Connect("127.0.0.1", standIn.Port)
                Dim stream As NetworkStream = client.GetStream()
                stream.ReadTimeout = 5000
                For Each record As CaptureRecord In CaptureReader.Read(Capture).Where(Function(item As CaptureRecord) item.Number <= lastRecord)
                    If record.Direction = "S"c Then
                        CollectionAssert.AreEqual(record.Bytes, ReadExactly(stream, record.Bytes.Length), "record " & record.Number.ToString())
                    ElseIf typed IsNot Nothing AndAlso record.Number >= 9 AndAlso record.Number <= 31 Then
                        If record.Number = 9 Then stream.Write(typed, 0, typed.Length)
                    ElseIf record37 IsNot Nothing AndAlso record.Number = 37 Then
                        stream.Write(record37, 0, record37.Length)
                    Else
                        stream.Write(record.Bytes, 0, record.Bytes.Length)
                    End If
                Next
            End Using
        End Sub

        Private Shared Function ReadExactly(stream As NetworkStream, count As Integer) As Byte()
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
