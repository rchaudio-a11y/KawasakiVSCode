' File:        ControllerCommandTests.vb
' Project:     KawasakiVSCode.Tests
' Description: The commands khi originates, their text, and how Send puts a LOAD line on the wire, as
'              capture (i) shows KRterm doing (D1, FR-027, Article III).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Linq
Imports System.Text
Imports KawasakiVSCode.Core.Commands
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Core.Reporting
Imports KawasakiVSCode.Core.Session
Imports KawasakiVSCode.Tests.Connection
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Commands

    ''' <summary>ControllerCommand, the one door for the commands khi originates (Article III, research R15).</summary>
    <TestClass>
    Public Class ControllerCommandTests

        Private Shared ReadOnly Patience As TimeSpan = TimeSpan.FromSeconds(5)

        ''' <summary>The login is the constant as.</summary>
        <TestMethod>
        Public Sub TheLoginIsAs()
            Assert.AreEqual("as", ControllerCommand.Login.Line)
        End Sub

        ''' <summary>A LOAD names the file by its bare name, cased as captured (FR-027).</summary>
        <TestMethod>
        Public Sub ALoadSendsTheBareName()
            Assert.AreEqual("load capb.as", ControllerCommand.Load("C:\cells\capb.as").Line)
            Assert.AreEqual("load capb.as", ControllerCommand.Load("capb.as").Line)
        End Sub

        ''' <summary>A SAVE carries its qualifier as the manual writes it, and the bare name.</summary>
        <TestMethod>
        Public Sub ASaveCarriesItsQualifier()
            Assert.AreEqual("SAVE capd.as", ControllerCommand.Save(SaveQualifier.Parse(Nothing), "capd.as").Line)
            Assert.AreEqual("SAVE/P capd.as", ControllerCommand.Save(SaveQualifier.Parse("P"), "C:\backup\capd.as").Line)
            Assert.AreEqual("SAVE/FULL capd.as", ControllerCommand.Save(SaveQualifier.Parse("FULL"), "capd.as").Line)
        End Sub

        ' Fire demonstration (Article VII), 2026-11-04: made ControllerCommand.BareName accept every character;
        ' this test failed with "C:\cells\çapb.as was not refused". Restored the file; it passed.
        ''' <summary>
        ''' A bare name with a character outside ASCII is refused by name before connecting, exit 2 (FR-044). A
        ''' folder outside ASCII is never sent, so it is not refused.
        ''' </summary>
        <TestMethod>
        Public Sub ANameOutsideAsciiIsRefused()
            Dim loadPath As String = "C:\cells\" & ChrW(&HE7) & "apb.as"
            Dim savePath As String = "r" & ChrW(&HE9) & "sum" & ChrW(&HE9) & ".as"
            Dim cases As New Dictionary(Of String, Action) From {
                {loadPath, Sub() ControllerCommand.Load(loadPath)},
                {savePath, Sub() ControllerCommand.Save(SaveQualifier.Parse("P"), savePath)}}
            For Each item As KeyValuePair(Of String, Action) In cases
                Dim refusal As KhiRefusal = Nothing
                Try
                    item.Value.Invoke()
                Catch ex As KhiRefusal
                    refusal = ex
                End Try
                Assert.IsNotNull(refusal, item.Key & " was not refused")
                Assert.AreEqual(ExitCode.RefusedBeforeConnecting, refusal.ExitCode, item.Key)
                Assert.AreEqual("khi: " & item.Key & " has a name outside ASCII; it cannot be sent", refusal.Message)
            Next
            Assert.AreEqual("load capb.as", ControllerCommand.Load("C:\Z" & ChrW(&HFC) & "rich\capb.as").Line)
        End Sub

        ''' <summary>
        ''' Send puts a LOAD on the wire as capture (i) records 9 to 12 show (D1): the line; after its echo, 18,
        ''' the line again and 0d 0a; and it returns at > and the line and 0d 0a, with every byte read.
        ''' </summary>
        <TestMethod, Capture("i-load-existing")>
        Public Sub SendFollowsTheCapture()
            Dim records As IReadOnlyList(Of CaptureRecord) = CaptureReader.Read("i-load-existing")
            Dim line As Byte() = Encoding.ASCII.GetBytes("load capb.as")
            Dim redraw As Byte() = records(11).Bytes
            Dim ending As Byte() = New Byte() {&H3E}.Concat(line).Concat(New Byte() {&HD, &HA}).ToArray()
            Dim endAt As Integer = IndexOf(redraw, ending) + ending.Length
            Dim expected As Byte() = records(9).Bytes.Concat(redraw.Take(endAt)).ToArray()
            Using standIn As ControllerStandIn = ControllerStandIn.PlaySlice("i-load-existing", New RecordRange(1, 12)).Start()
                Dim reply As Byte()
                Using connection As ControllerConnection = ControllerConnection.Connect(ControllerConnectionTests.Robot(standIn), New Timeouts(TimeSpan.FromSeconds(2)))
                    LoginSequence.Run(connection)
                    reply = connection.Send(ControllerCommand.Load("capb.as"))
                End Using
                Assert.IsTrue(standIn.WaitForEnd(Patience))
                CollectionAssert.AreEqual(expected, reply)
            End Using
        End Sub

        Private Shared Function IndexOf(haystack As Byte(), needle As Byte()) As Integer
            For start As Integer = 0 To haystack.Length - needle.Length
                If haystack.Skip(start).Take(needle.Length).SequenceEqual(needle) Then Return start
            Next
            Throw New InvalidOperationException("the capture's record 12 does not hold > and the line")
        End Function

    End Class

End Namespace
