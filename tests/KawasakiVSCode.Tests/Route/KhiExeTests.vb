' File:        KhiExeTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Runs the built khi.exe as a process, proving Main wires to KhiApp.Run (Article XIII,
'              research R12).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Tests.Conformance
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Route

    ''' <summary>The built khi.exe, run as a process (research R12).</summary>
    <TestClass>
    Public Class KhiExeTests

        ''' <summary>khi.exe with no arguments exits 2 with the usage refusal on stderr and nothing on stdout.</summary>
        <TestMethod>
        Public Sub NoArgumentsExitTwoWithTheUsage()
            Dim exe As String = KhiExePath()
            Assert.IsTrue(File.Exists(exe), "khi.exe is not built at " & exe)
            Dim start As New ProcessStartInfo(exe) With {
                .RedirectStandardInput = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .UseShellExecute = False,
                .CreateNoWindow = True}
            Using running As Process = Process.Start(start)
                running.StandardInput.Close()
                Dim errorText As Threading.Tasks.Task(Of String) = running.StandardError.ReadToEndAsync()
                Dim outputText As Threading.Tasks.Task(Of String) = running.StandardOutput.ReadToEndAsync()
                Assert.IsTrue(running.WaitForExit(30000), "khi.exe did not exit within 30 s")
                Assert.AreEqual(2, running.ExitCode)
                StringAssert.StartsWith(errorText.Result, "khi: no verb given")
                StringAssert.Contains(errorText.Result, "usage: khi term [--robot <name>]")
                Assert.AreEqual("", outputText.Result)
            End Using
        End Sub

        ''' <summary>The Cli project's output folder, in the configuration the tests were built in.</summary>
        Private Shared Function KhiExePath() As String
            Dim framework As DirectoryInfo = New DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))
            Dim configuration As String = framework.Parent.Name
            Return Path.Combine(RepositoryPaths.Root(), "src", "KawasakiVSCode.Cli", "bin", configuration, framework.Name, "khi.exe")
        End Function

    End Class

End Namespace
