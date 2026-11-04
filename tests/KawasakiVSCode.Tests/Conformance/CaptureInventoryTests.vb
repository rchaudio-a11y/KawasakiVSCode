' File:        CaptureInventoryTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Every capture the spec lists is checked in, reads, and has a confirmed note with the
'              required headings; every SAVE capture holds the file KRterm wrote (contracts/capture-format.md).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Linq
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Conformance

    ''' <summary>The capture inventory (FR-002, FR-003, contracts/capture-format.md).</summary>
    <TestClass>
    Public Class CaptureInventoryTests

        Private Shared ReadOnly RequiredHeadings As String() = {"What was done", "Against what", "Tools", "Route", "Date", "Payload regions", "Typed regions", "Observations", "Manual citations"}

        Private Shared ReadOnly SaveLetters As String() = {"d", "e", "f", "g", "h", "m"}

        ''' <summary>For each letter (a) to (s), a folder named for it exists.</summary>
        <TestMethod>
        Public Sub EveryLetterHasACapture()
            Dim names As String() = Folders().Select(Function(folder As String) Path.GetFileName(folder)).ToArray()
            Dim missing As New List(Of String)()
            For letter As Integer = AscW("a"c) To AscW("s"c)
                Dim prefix As String = ChrW(letter) & "-"
                If Not names.Any(Function(name As String) name.StartsWith(prefix, StringComparison.Ordinal)) Then missing.Add(ChrW(letter))
            Next
            Assert.AreEqual(0, missing.Count, "no capture folder for: " & String.Join(", ", missing))
        End Sub

        ''' <summary>Every capture folder has an exchange.txt that CaptureReader reads.</summary>
        <TestMethod>
        Public Sub EveryCaptureReads()
            For Each folder As String In Folders()
                Dim name As String = Path.GetFileName(folder)
                Assert.IsTrue(File.Exists(Path.Combine(folder, "exchange.txt")), name & " has no exchange.txt")
                Assert.IsTrue(CaptureReader.Read(name).Count > 0, name & " reads as no records")
            Next
        End Sub

        ' Fire demonstration (Article VII, T045), 2026-11-04: renamed the heading Tools to Tool in
        ' Captures/q-load-empty/note.md; this test failed with "q-load-empty: no heading Tools". Restored the note
        ' byte for byte; it passed.
        ''' <summary>Every note is confirmed by the Architect and has every heading contracts/capture-format.md requires.</summary>
        <TestMethod>
        Public Sub EveryNoteIsConfirmedWithEveryHeading()
            Dim failures As New List(Of String)()
            For Each folder As String In Folders()
                Dim name As String = Path.GetFileName(folder)
                Dim note As String = Path.Combine(folder, "note.md")
                If Not File.Exists(note) Then
                    failures.Add(name & ": no note.md")
                    Continue For
                End If
                Dim lines As String() = File.ReadAllLines(note)
                If Not lines.Any(Function(line As String) line.StartsWith("**Status**: Confirmed by the Architect", StringComparison.Ordinal)) Then
                    failures.Add(name & ": not confirmed by the Architect")
                End If
                Dim headings As List(Of String) = lines.Where(Function(line As String) line.StartsWith("## ", StringComparison.Ordinal)).Select(Function(line As String) line.Substring(3).Trim()).ToList()
                For Each heading As String In RequiredHeadings
                    If Not headings.Contains(heading) Then failures.Add(name & ": no heading " & heading)
                Next
            Next
            Assert.AreEqual(0, failures.Count, String.Join(Environment.NewLine, failures))
        End Sub

        ''' <summary>The SAVE captures (d), (e), (f), (g), (h) and (m) hold in files/ the file KRterm wrote.</summary>
        <TestMethod>
        Public Sub EverySaveCaptureHoldsTheFileKrtermWrote()
            For Each letter As String In SaveLetters
                Dim matching As String() = Folders().Where(Function(folder As String) Path.GetFileName(folder).StartsWith(letter & "-", StringComparison.Ordinal)).ToArray()
                Assert.AreEqual(1, matching.Length, "capture (" & letter & ")")
                Dim written As String = Path.Combine(matching(0), "files", "cap" & letter & ".as")
                Assert.IsTrue(File.Exists(written), "capture (" & letter & ") has no files/cap" & letter & ".as")
            Next
        End Sub

        Private Shared Function Folders() As String()
            Return Directory.GetDirectories(Path.Combine(RepositoryPaths.Root(), "tests", "KawasakiVSCode.Tests", "Captures"))
        End Function

    End Class

End Namespace
