' File:        RobotsFileTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Reading khi.robots.json, and refusing a missing file and each broken rule by name
'              before connecting (contracts/robots-file.md).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Reporting
Imports KawasakiVSCode.Core.Robots
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Robots

    ''' <summary>khi.robots.json, read from a folder (contracts/robots-file.md).</summary>
    <TestClass>
    Public Class RobotsFileTests

        Private _folder As String

        ''' <summary>Makes an empty folder for each test.</summary>
        <TestInitialize>
        Public Sub MakeFolder()
            _folder = Path.Combine(Path.GetTempPath(), "khi-robots-" & Guid.NewGuid().ToString("N"))
            Directory.CreateDirectory(_folder)
        End Sub

        ''' <summary>Removes the test's folder.</summary>
        <TestCleanup>
        Public Sub RemoveFolder()
            Directory.Delete(_folder, True)
        End Sub

        ''' <summary>A valid file reads, with its entries in order and its default.</summary>
        <TestMethod>
        Public Sub AValidFileReads()
            Write("{""default"": ""sim"", ""robots"": [" &
                  "{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 9105, ""simulator"": true}," &
                  "{""name"": ""cell1"", ""address"": ""192.168.0.2"", ""port"": 23, ""simulator"": false}]}")
            Dim file As RobotsFile = RobotsFile.Load(_folder)
            Assert.AreEqual(2, file.Robots.Count)
            Assert.AreEqual("sim", file.Robots(0).Name)
            Assert.AreEqual("127.0.0.1", file.Robots(0).Address)
            Assert.AreEqual(9105, file.Robots(0).Port)
            Assert.IsTrue(file.Robots(0).Simulator)
            Assert.AreEqual("cell1", file.Robots(1).Name)
            Assert.IsFalse(file.Robots(1).Simulator)
            Assert.AreEqual("sim", file.DefaultName)
        End Sub

        ''' <summary>The default is optional.</summary>
        <TestMethod>
        Public Sub TheDefaultIsOptional()
            Write("{""robots"": [{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 9105, ""simulator"": true}]}")
            Assert.IsNull(RobotsFile.Load(_folder).DefaultName)
        End Sub

        ''' <summary>A missing file is refused naming the folder.</summary>
        <TestMethod>
        Public Sub AMissingFileIsRefused()
            Dim refusal As KhiRefusal = Refuse()
            Assert.AreEqual(ExitCode.RefusedBeforeConnecting, refusal.ExitCode)
            Assert.AreEqual("khi: no khi.robots.json in " & _folder, refusal.Message)
        End Sub

        ''' <summary>Each broken rule is refused with what is wrong and where, exit 2.</summary>
        <TestMethod>
        Public Sub EachBrokenRuleIsRefused()
            Dim good As String = "{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 9105, ""simulator"": true}"
            Dim cases As New Dictionary(Of String, String) From {
                {"not json", "not valid JSON"},
                {"[]", "must hold one JSON object"},
                {"{}", """robots"" must be a list with at least one entry"},
                {"{""robots"": []}", """robots"" must be a list with at least one entry"},
                {"{""robots"": [" & good & "], ""robot"": 1}", "unknown key ""robot"""},
                {"{""robots"": [{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 9105, ""simulator"": true, ""adress"": ""x""}]}", "robots[0]: unknown key ""adress"""},
                {"{""robots"": [{""name"": """", ""address"": ""127.0.0.1"", ""port"": 9105, ""simulator"": true}]}", "robots[0].name is required and must not be empty"},
                {"{""robots"": [" & good & ", {""name"": ""SIM"", ""address"": ""127.0.0.2"", ""port"": 9105, ""simulator"": true}]}", "robots[1].name ""SIM"" repeats robots[0].name"},
                {"{""robots"": [{""name"": ""sim"", ""address"": """", ""port"": 9105, ""simulator"": true}]}", "robots[0].address is required: a host name or IP address"},
                {"{""robots"": [{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 0, ""simulator"": true}]}", "robots[0].port must be a whole number from 1 to 65535"},
                {"{""robots"": [{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 65536, ""simulator"": true}]}", "robots[0].port must be a whole number from 1 to 65535"},
                {"{""robots"": [{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 9105.5, ""simulator"": true}]}", "robots[0].port must be a whole number from 1 to 65535"},
                {"{""robots"": [{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 9105, ""simulator"": ""yes""}]}", "robots[0].simulator must be true or false"},
                {"{""robots"": [{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 9105}]}", "robots[0].simulator must be true or false"},
                {"{""default"": ""cell9"", ""robots"": [" & good & "]}", "default ""cell9"" names no robot"}}
            For Each item As KeyValuePair(Of String, String) In cases
                Write(item.Key)
                Dim refusal As KhiRefusal = Refuse()
                Assert.AreEqual(ExitCode.RefusedBeforeConnecting, refusal.ExitCode, item.Key)
                StringAssert.StartsWith(refusal.Message, "khi: khi.robots.json: ", item.Key)
                StringAssert.Contains(refusal.Message, item.Value, item.Key)
            Next
        End Sub

        Private Sub Write(json As String)
            File.WriteAllText(Path.Combine(_folder, "khi.robots.json"), json)
        End Sub

        Private Function Refuse() As KhiRefusal
            Try
                RobotsFile.Load(_folder)
            Catch refusal As KhiRefusal
                Return refusal
            End Try
            Assert.Fail("the robots file was not refused")
            Return Nothing
        End Function

    End Class

End Namespace
