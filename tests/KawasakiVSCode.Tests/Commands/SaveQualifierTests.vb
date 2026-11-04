' File:        SaveQualifierTests.vb
' Project:     KawasakiVSCode.Tests
' Description: SaveQualifier.Parse gives the captured SAVE forms and refuses every other qualifier by
'              name (FR-024, FR-025).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports KawasakiVSCode.Core.Commands
Imports KawasakiVSCode.Core.Reporting
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Commands

    ''' <summary>The supported SAVE qualifiers and the refusal of every other (FR-024, FR-025).</summary>
    <TestClass>
    Public Class SaveQualifierTests

        ''' <summary>No qualifier, P and FULL give SAVE, SAVE/P and SAVE/FULL, as captures (g), (d) and (e) show.</summary>
        <TestMethod>
        Public Sub TheCapturedFormsParse()
            Assert.AreEqual("SAVE", SaveQualifier.Parse(Nothing).CommandWord)
            Assert.AreEqual("SAVE/P", SaveQualifier.Parse("P").CommandWord)
            Assert.AreEqual("SAVE/FULL", SaveQualifier.Parse("FULL").CommandWord)
        End Sub

        ''' <summary>A qualifier the manual lists but no capture backs is refused by name as not supported, exit 2.</summary>
        <TestMethod>
        Public Sub ListedButUncapturedQualifiersAreRefused()
            For Each qualifier As String In New String() {"L", "R", "S", "A", "SYS", "ROB", "ELOG", "OPLOG", "ALLLOG", "STG"}
                Dim refusal As KhiRefusal = Refuse(qualifier)
                Assert.AreEqual(ExitCode.RefusedBeforeConnecting, refusal.ExitCode, qualifier)
                StringAssert.Contains(refusal.Message, qualifier)
                StringAssert.Contains(refusal.Message, "not supported")
            Next
        End Sub

        ''' <summary>Anything else, a misspelling or another case included, is refused by name as unknown, exit 2.</summary>
        <TestMethod>
        Public Sub UnknownQualifiersAreRefused()
            For Each qualifier As String In New String() {"X", "full", "p", "/P", ""}
                Dim refusal As KhiRefusal = Refuse(qualifier)
                Assert.AreEqual(ExitCode.RefusedBeforeConnecting, refusal.ExitCode, qualifier)
                StringAssert.Contains(refusal.Message, "unknown qualifier")
            Next
        End Sub

        Private Shared Function Refuse(qualifier As String) As KhiRefusal
            Try
                SaveQualifier.Parse(qualifier)
            Catch refusal As KhiRefusal
                Return refusal
            End Try
            Assert.Fail("qualifier '" & qualifier & "' was not refused")
            Return Nothing
        End Function

    End Class

End Namespace
