' File:        PersonInputTests.vb
' Project:     KawasakiVSCode.Tests
' Description: PersonInput, the one door for the person's keys: the line typed at a question, Backspace
'              erasing within it, Ctrl+C dropped, and the line echoed when the keys come from the console
'              (FR-021, FR-041, research R8; Architect, 2026-11-04).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Terminal
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Terminal

    ''' <summary>
    ''' The line PersonInput collects at a question. When the keys come from the console, nothing else echoes
    ''' them, so PersonInput echoes the line as it is typed; the answer still goes to the controller as one
    ''' line at Enter (Architect, 2026-11-04, after the Ctrl+C check in VS Code's terminal).
    ''' </summary>
    <TestClass>
    Public Class PersonInputTests

        ' Red before the change, 2026-11-04: "Element at index 0 do not match. Expected: 48, Actual: 49", the 08
        ' kept in the line. Fire demonstration (Article VII), the same day: made ReadLine keep 08 as an ordinary
        ' key; this test failed the same way. Restored the file; it passed.
        ''' <summary>Backspace erases the key before it, so the line sent is the one the person ended with.</summary>
        <TestMethod>
        Public Sub BackspaceErasesTheKeyBeforeIt()
            Dim person As New PersonInput(New MemoryStream(New Byte() {&H31, &H8, &H30, &HD}))
            CollectionAssert.AreEqual(New Byte() {&H30, &HD, &HA}, person.ReadLine())
        End Sub

        ''' <summary>Backspace on an empty line does nothing.</summary>
        <TestMethod>
        Public Sub BackspaceOnAnEmptyLineDoesNothing()
            Dim person As New PersonInput(New MemoryStream(New Byte() {&H8, &H31, &HD}))
            CollectionAssert.AreEqual(New Byte() {&H31, &HD, &HA}, person.ReadLine())
        End Sub

        ' Fire demonstration (Article VII), 2026-11-04: made PersonInput echo nothing; this test failed with
        ' "CollectionAssert.AreEqual failed. Different number of elements.". Restored the file; it passed.
        ''' <summary>With an echo given, each key of the line is echoed as typed, and Enter as 0d 0a.</summary>
        <TestMethod>
        Public Sub TheLineIsEchoedAsTyped()
            Using echo As New MemoryStream()
                Dim person As New PersonInput(New MemoryStream(New Byte() {&H31, &HD}), echo)
                CollectionAssert.AreEqual(New Byte() {&H31, &HD, &HA}, person.ReadLine())
                CollectionAssert.AreEqual(New Byte() {&H31, &HD, &HA}, echo.ToArray())
            End Using
        End Sub

        ''' <summary>Backspace is echoed as 08 20 08, which erases the key on the screen; on an empty line it echoes nothing.</summary>
        <TestMethod>
        Public Sub BackspaceIsEchoedAsAnErase()
            Using echo As New MemoryStream()
                Dim person As New PersonInput(New MemoryStream(New Byte() {&H8, &H31, &H8, &H30, &HD}), echo)
                CollectionAssert.AreEqual(New Byte() {&H30, &HD, &HA}, person.ReadLine())
                CollectionAssert.AreEqual(New Byte() {&H31, &H8, &H20, &H8, &H30, &HD, &HA}, echo.ToArray())
            End Using
        End Sub

        ''' <summary>Ctrl+C is dropped and not echoed; the line goes on (FR-041).</summary>
        <TestMethod>
        Public Sub ControlCIsNeitherSentNorEchoed()
            Using echo As New MemoryStream()
                Dim person As New PersonInput(New MemoryStream(New Byte() {&H31, &H3, &HD}), echo)
                CollectionAssert.AreEqual(New Byte() {&H31, &HD, &HA}, person.ReadLine())
                CollectionAssert.AreEqual(New Byte() {&H31, &HD, &HA}, echo.ToArray())
            End Using
        End Sub

        ''' <summary>Ctrl+] ends the line with no answer and is not echoed.</summary>
        <TestMethod>
        Public Sub ControlBracketIsNoAnswerAndNotEchoed()
            Using echo As New MemoryStream()
                Dim person As New PersonInput(New MemoryStream(New Byte() {&H31, &H1D, &H30, &HD}), echo)
                Assert.IsNull(person.ReadLine())
                Assert.IsTrue(person.Escaped)
                CollectionAssert.AreEqual(New Byte() {&H31}, echo.ToArray())
            End Using
        End Sub

    End Class

End Namespace
