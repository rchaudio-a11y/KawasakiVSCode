' File:        CommandRecorderTests.vb
' Project:     KawasakiVSCode.Tests
' Description: The command recorder turns client bytes into the commands the controller would run,
'              skipping telnet negotiation and transfer frames, and lists those outside the allowed
'              set (INV-2).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Linq
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace StandIn

    ''' <summary>Recording the commands a client sends, and checking them against the allowed set (INV-2).</summary>
    <TestClass>
    Public Class CommandRecorderTests

        ''' <summary>Capture (b)'s client bytes record the login and the LOAD line, KRterm's CAN and repeat dropped, framing skipped.</summary>
        <TestMethod>
        Public Sub CaptureBRecordsTheLoginAndTheLoadLine()
            Dim recorder As CommandRecorder = RecordClient("b-load-small")
            CollectionAssert.AreEqual(New String() {"as", "load capb.as"}, recorder.Commands.ToArray())
            Assert.AreEqual(0, recorder.Disallowed(Array.Empty(Of String)()).Count)
        End Sub

        ''' <summary>Capture (d)'s client bytes record the login and the SAVE/P line, both allowed.</summary>
        <TestMethod>
        Public Sub CaptureDRecordsTheSaveLine()
            Dim recorder As CommandRecorder = RecordClient("d-save-p")
            CollectionAssert.AreEqual(New String() {"as", "SAVE/P capd.as"}, recorder.Commands.ToArray())
            Assert.AreEqual(0, recorder.Disallowed(Array.Empty(Of String)()).Count)
        End Sub

        ''' <summary>Capture (k)'s DELETE/P lines and confirmations are outside the allowed set.</summary>
        <TestMethod>
        Public Sub CaptureKCommandsAreNotAllowed()
            Dim recorder As CommandRecorder = RecordClient("k-delete-p")
            CollectionAssert.AreEqual(New String() {"DELETE/P capl", "1", "DELETE/P nosuch", "1", "DELETE/P capb", "1", "DIRECTORY/P"}, recorder.Disallowed(Array.Empty(Of String)()).ToArray())
        End Sub

        ''' <summary>A command khi never sends, such as ERESET, is outside the allowed set; a line the test typed is inside it.</summary>
        <TestMethod>
        Public Sub OnlyTheAllowedSetAndTypedLinesPass()
            Dim recorder As New CommandRecorder()
            recorder.Add(Encoding.ASCII.GetBytes("as" & vbCrLf & "ERESET" & vbCrLf & "0" & vbCrLf))
            CollectionAssert.AreEqual(New String() {"ERESET"}, recorder.Disallowed(New String() {"0"}).ToArray())
        End Sub

        Private Shared Function RecordClient(capture As String) As CommandRecorder
            Dim recorder As New CommandRecorder()
            For Each record As CaptureRecord In CaptureReader.Read(capture)
                If record.Direction = "C"c Then recorder.Add(record.Bytes)
            Next
            Return recorder
        End Function

    End Class

End Namespace
