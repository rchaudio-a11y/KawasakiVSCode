' File:        KhiAppArgumentTests.vb
' Project:     KawasakiVSCode.Tests
' Description: khi's command line through KhiApp.Run, the code Main runs: every malformed command line
'              is refused by name with exit 2, and an unwired verb says so (contracts/cli.md, FR-032,
'              research R6, R12).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Cli
Imports KawasakiVSCode.Core.Connection
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Route

    ''' <summary>
    ''' The command line through KhiApp.Run (Article XIII, contracts/cli.md Synopsis). "No connection" is not
    ''' asserted here: nothing connects yet, so such an assertion would pass vacuously (Article VII). It is
    ''' asserted with the stand-in once khi load connects (User Story 2).
    ''' </summary>
    <TestClass>
    Public Class KhiAppArgumentTests

        ''' <summary>No verb is refused with the usage, exit 2.</summary>
        <TestMethod>
        Public Sub NoVerbIsRefusedWithTheUsage()
            Dim result As RunResult = Run()
            Assert.AreEqual(2, result.ExitCode)
            StringAssert.StartsWith(result.Errors, "khi: no verb given" & Environment.NewLine)
            StringAssert.Contains(result.Errors, "usage: khi term [--robot <name>]")
            StringAssert.Contains(result.Errors, "khi load <file> [--robot <name>]")
            StringAssert.Contains(result.Errors, "khi save <file> [--qualifier <q>] [--robot <name>]")
            Assert.AreEqual(0, result.Output.Length)
        End Sub

        ''' <summary>Each malformed command line is refused by name, exit 2, with nothing on stdout.</summary>
        <TestMethod>
        Public Sub EachMalformedCommandLineIsRefusedByName()
            Dim cases As New List(Of KeyValuePair(Of String(), String)) From {
                Pair("khi: unknown verb run", "run"),
                Pair("khi: unknown verb LOAD", "LOAD", "x.as"),
                Pair("khi: unknown option --force", "load", "x.as", "--force"),
                Pair("khi: unknown option -r", "load", "x.as", "-r", "sim"),
                Pair("khi: khi term does not take --qualifier", "term", "--qualifier", "P"),
                Pair("khi: khi load does not take --qualifier", "load", "x.as", "--qualifier", "P"),
                Pair("khi: --robot is given twice", "load", "x.as", "--robot", "sim", "--robot", "cell1"),
                Pair("khi: --robot needs a value", "load", "x.as", "--robot"),
                Pair("khi: --qualifier needs a value", "save", "x.as", "--qualifier", "--robot", "sim"),
                Pair("khi: khi load needs a <file>", "load"),
                Pair("khi: khi save needs a <file>", "save", "--robot", "sim"),
                Pair("khi: unexpected argument b.as", "load", "a.as", "b.as"),
                Pair("khi: unexpected argument x.as", "term", "x.as")}
            For Each item As KeyValuePair(Of String(), String) In cases
                Dim result As RunResult = Run(item.Key)
                Dim shown As String = String.Join(" ", item.Key)
                Assert.AreEqual(2, result.ExitCode, shown)
                Assert.AreEqual(item.Value & Environment.NewLine, result.Errors, shown)
                Assert.AreEqual(0, result.Output.Length, shown)
            Next
        End Sub

        ''' <summary>An unsupported qualifier is refused by name before connecting, exit 2.</summary>
        <TestMethod>
        Public Sub AnUnsupportedQualifierIsRefused()
            Dim listed As RunResult = Run("save", "x.as", "--qualifier", "L")
            Assert.AreEqual(2, listed.ExitCode)
            StringAssert.Contains(listed.Errors, """L"" is not supported")
            Dim unknown As RunResult = Run("save", "x.as", "--qualifier", "full")
            Assert.AreEqual(2, unknown.ExitCode)
            StringAssert.Contains(unknown.Errors, "unknown qualifier ""full""")
        End Sub

        ''' <summary>An option may come before or after the file.</summary>
        <TestMethod>
        Public Sub AnOptionMayComeBeforeOrAfterTheFile()
            For Each args As String() In New String()() {
                    New String() {"save", "x.as", "--robot", "sim", "--qualifier", "P"},
                    New String() {"save", "--robot", "sim", "x.as", "--qualifier", "P"},
                    New String() {"save", "--qualifier", "P", "--robot", "sim", "x.as"}}
                Dim parsed As ParsedArguments = ArgumentParser.Parse(args)
                Dim shown As String = String.Join(" ", args)
                Assert.AreEqual("save", parsed.Verb, shown)
                Assert.AreEqual("x.as", parsed.FilePath, shown)
                Assert.AreEqual("sim", parsed.Robot, shown)
                Assert.AreEqual("SAVE/P", parsed.Qualifier.CommandWord, shown)
            Next
        End Sub

        ''' <summary>Left out, --robot is Nothing and a save's qualifier is the bare SAVE.</summary>
        <TestMethod>
        Public Sub LeftOutOptionsHaveNoValue()
            Dim saved As ParsedArguments = ArgumentParser.Parse(New String() {"save", "x.as"})
            Assert.IsNull(saved.Robot)
            Assert.AreEqual("SAVE", saved.Qualifier.CommandWord)
            Dim loaded As ParsedArguments = ArgumentParser.Parse(New String() {"load", "x.as"})
            Assert.IsNull(loaded.Qualifier)
            Dim terminal As ParsedArguments = ArgumentParser.Parse(New String() {"term"})
            Assert.IsNull(terminal.FilePath)
        End Sub

        ''' <summary>A verb not yet wired is refused with a message saying so, exit 2 (FR-032).</summary>
        <TestMethod>
        Public Sub AnUnwiredVerbSaysSo()
            For Each args As String() In New String()() {
                    New String() {"term"},
                    New String() {"load", "x.as"},
                    New String() {"save", "x.as", "--qualifier", "FULL"}}
                Dim result As RunResult = Run(args)
                Assert.AreEqual(2, result.ExitCode, args(0))
                Assert.AreEqual("khi: " & args(0) & " is not implemented yet" & Environment.NewLine, result.Errors, args(0))
                Assert.AreEqual(0, result.Output.Length, args(0))
            Next
        End Sub

        Private Shared Function Pair(message As String, ParamArray args As String()) As KeyValuePair(Of String(), String)
            Return New KeyValuePair(Of String(), String)(args, message)
        End Function

        Private Shared Function Run(ParamArray args As String()) As RunResult
            Using input As New MemoryStream(), output As New MemoryStream(), errors As New StringWriter()
                Dim code As Integer = KhiApp.Run(args, input, output, errors, New Timeouts(TimeSpan.FromMilliseconds(200)))
                Return New RunResult(code, output.ToArray(), errors.ToString())
            End Using
        End Function

    End Class

End Namespace
