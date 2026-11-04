' File:        RobotSelectionTests.vb
' Project:     KawasakiVSCode.Tests
' Description: RobotSelection.Resolve picks the named robot or the default, and refuses an unknown
'              name or the lack of both, listing the names (Article IX, FR-008).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Reporting
Imports KawasakiVSCode.Core.Robots
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Robots

    ''' <summary>Which robot a verb uses (Article IX, FR-008).</summary>
    <TestClass>
    Public Class RobotSelectionTests

        ''' <summary>--robot picks that entry, matched ignoring case.</summary>
        <TestMethod>
        Public Sub TheNamedRobotIsPicked()
            Dim file As RobotsFile = Load(True)
            Assert.AreEqual("cell1", RobotSelection.Resolve(file, "cell1").Name)
            Assert.AreEqual("cell1", RobotSelection.Resolve(file, "CELL1").Name)
        End Sub

        ''' <summary>With no --robot, the default is used.</summary>
        <TestMethod>
        Public Sub TheDefaultIsUsed()
            Assert.AreEqual("sim", RobotSelection.Resolve(Load(True), Nothing).Name)
        End Sub

        ''' <summary>With neither --robot nor a default, khi refuses and lists the names.</summary>
        <TestMethod>
        Public Sub NeitherIsRefused()
            Dim refusal As KhiRefusal = Refuse(Load(False), Nothing)
            Assert.AreEqual(ExitCode.RefusedBeforeConnecting, refusal.ExitCode)
            Assert.AreEqual("khi: no --robot given and no default set; robots are: sim, cell1", refusal.Message)
        End Sub

        ''' <summary>An unknown name is refused by that name, listing the names.</summary>
        <TestMethod>
        Public Sub AnUnknownNameIsRefused()
            Dim refusal As KhiRefusal = Refuse(Load(True), "cell9")
            Assert.AreEqual(ExitCode.RefusedBeforeConnecting, refusal.ExitCode)
            Assert.AreEqual("khi: unknown robot cell9; robots are: sim, cell1", refusal.Message)
        End Sub

        Private Shared Function Load(withDefault As Boolean) As RobotsFile
            Dim folder As String = Path.Combine(Path.GetTempPath(), "khi-select-" & Guid.NewGuid().ToString("N"))
            Directory.CreateDirectory(folder)
            Try
                Dim defaultPart As String = If(withDefault, """default"": ""sim"", ", "")
                File.WriteAllText(Path.Combine(folder, "khi.robots.json"), "{" & defaultPart & """robots"": [" &
                    "{""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": 9105, ""simulator"": true}," &
                    "{""name"": ""cell1"", ""address"": ""192.168.0.2"", ""port"": 23, ""simulator"": false}]}")
                Return RobotsFile.Load(folder)
            Finally
                Directory.Delete(folder, True)
            End Try
        End Function

        Private Shared Function Refuse(file As RobotsFile, requested As String) As KhiRefusal
            Try
                RobotSelection.Resolve(file, requested)
            Catch refusal As KhiRefusal
                Return refusal
            End Try
            Assert.Fail("the selection was not refused")
            Return Nothing
        End Function

    End Class

End Namespace
