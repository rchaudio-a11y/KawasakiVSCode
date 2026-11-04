' File:        HeaderBlockTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Every .vb file opens with the header block: file name, project, description, author
'              RCH Automation LLC and created date (constitution hard rules).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Text.RegularExpressions
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Conformance

    ''' <summary>Every .vb file opens with the header block the constitution's hard rules require.</summary>
    <TestClass>
    Public Class HeaderBlockTests

        ' Fire demonstration (Article VII, T042), 2026-11-04: removed the Author line from
        ' src/KawasakiVSCode.Core/Reporting/ExitCode.vb; this test failed (1 failed of 1). Restored the
        ' file byte for byte; it passed (1 of 1).
        ''' <summary>The opening comment block names the file, the project, a description, the author and a created date.</summary>
        <TestMethod>
        Public Sub EveryVbFileOpensWithTheHeaderBlock()
            Dim sources As IReadOnlyList(Of String) = RepositoryPaths.Files(".vb")
            Assert.IsTrue(sources.Count > 0, "no .vb files found")
            Dim failures As New List(Of String)
            For Each source As String In sources
                Dim header As New List(Of String)
                For Each line As String In File.ReadLines(source)
                    If Not line.StartsWith("'", StringComparison.Ordinal) Then Exit For
                    header.Add(line)
                Next
                Dim block As String = String.Join(vbLf, header)
                Dim name As String = Path.GetFileName(source)
                If Not Regex.IsMatch(block, "^' File:\s+" & Regex.Escape(name) & "\s*$", RegexOptions.Multiline) Then failures.Add(name & ": no File line naming the file")
                If Not Regex.IsMatch(block, "^' Project:\s+\S", RegexOptions.Multiline) Then failures.Add(name & ": no Project line")
                If Not Regex.IsMatch(block, "^' Description:\s+\S", RegexOptions.Multiline) Then failures.Add(name & ": no Description line")
                If Not Regex.IsMatch(block, "^' Author:\s+RCH Automation LLC\s*$", RegexOptions.Multiline) Then failures.Add(name & ": no Author line for RCH Automation LLC")
                If Not Regex.IsMatch(block, "^' Created:\s+\d{4}-\d{2}-\d{2}\s*$", RegexOptions.Multiline) Then failures.Add(name & ": no Created date")
            Next
            Assert.AreEqual(0, failures.Count, String.Join(Environment.NewLine, failures))
        End Sub

    End Class

End Namespace
