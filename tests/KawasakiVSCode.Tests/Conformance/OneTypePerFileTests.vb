' File:        OneTypePerFileTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Every .vb file declares exactly one top-level Class, Module, Interface, Enum or
'              Structure (Article I).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Text.RegularExpressions
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Conformance

    ''' <summary>Every .vb file declares exactly one top-level type (Article I).</summary>
    <TestClass>
    Public Class OneTypePerFileTests

        Private Shared ReadOnly Attributes As New Regex("^(<[^>]*>\s*)+")
        Private Shared ReadOnly TypeStart As New Regex("^(?:(?:Public|Friend|Private|Protected|Partial|NotInheritable|MustInherit|Shadows)\s+)*(?:Class|Module|Interface|Enum|Structure)\s+\w", RegexOptions.IgnoreCase)
        Private Shared ReadOnly TypeEnd As New Regex("^End\s+(?:Class|Module|Interface|Enum|Structure)\b", RegexOptions.IgnoreCase)

        ' Fire demonstration (Article VII, T042), 2026-11-04: added a second class, InjectedSecond, to
        ' src/KawasakiVSCode.Core/Reporting/QuestionAnswer.vb; this test failed (1 failed of 1). Restored
        ' the file byte for byte; it passed (1 of 1).
        ''' <summary>Counts the top-level type declarations in each .vb file; each must have exactly one.</summary>
        <TestMethod>
        Public Sub EveryVbFileDeclaresOneTopLevelType()
            Dim sources As IReadOnlyList(Of String) = RepositoryPaths.Files(".vb")
            Assert.IsTrue(sources.Count > 0, "no .vb files found")
            Dim failures As New List(Of String)
            For Each source As String In sources
                Dim depth As Integer = 0
                Dim topLevel As Integer = 0
                For Each rawLine As String In File.ReadLines(source)
                    Dim line As String = Attributes.Replace(rawLine.Trim(), "")
                    If line.StartsWith("'", StringComparison.Ordinal) Then Continue For
                    If TypeEnd.IsMatch(line) Then
                        depth -= 1
                    ElseIf TypeStart.IsMatch(line) Then
                        If depth = 0 Then topLevel += 1
                        depth += 1
                    End If
                Next
                If topLevel <> 1 Then failures.Add(Path.GetFileName(source) & ": " & topLevel.ToString() & " top-level types")
            Next
            Assert.AreEqual(0, failures.Count, String.Join(Environment.NewLine, failures))
        End Sub

    End Class

End Namespace
