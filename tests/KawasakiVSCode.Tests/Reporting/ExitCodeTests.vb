' File:        ExitCodeTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Pins the exit codes of contracts/cli.md and checks that a refusal carries its exit
'              code and message.
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports KawasakiVSCode.Core.Reporting
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Reporting

    ''' <summary>The exit codes of contracts/cli.md, and the refusal that carries one.</summary>
    <TestClass>
    Public Class ExitCodeTests

        ''' <summary>Each exit code has the value contracts/cli.md gives it.</summary>
        <TestMethod>
        Public Sub ExitCodesHaveTheContractValues()
            Assert.AreEqual(0, CInt(ExitCode.Success))
            Assert.AreEqual(1, CInt(ExitCode.ControllerError))
            Assert.AreEqual(2, CInt(ExitCode.RefusedBeforeConnecting))
            Assert.AreEqual(3, CInt(ExitCode.TimedOut))
            Assert.AreEqual(4, CInt(ExitCode.ConnectionFailed))
            Assert.AreEqual(5, CInt(ExitCode.LocalFileFailed))
        End Sub

        ''' <summary>A refusal built with an exit code and a message carries both unchanged.</summary>
        <TestMethod>
        Public Sub ARefusalCarriesItsExitCodeAndMessage()
            Dim refusal As New KhiRefusal(ExitCode.RefusedBeforeConnecting, "khi: unknown verb run")
            Assert.AreEqual(ExitCode.RefusedBeforeConnecting, refusal.ExitCode)
            Assert.AreEqual("khi: unknown verb run", refusal.Message)
        End Sub

    End Class

End Namespace
