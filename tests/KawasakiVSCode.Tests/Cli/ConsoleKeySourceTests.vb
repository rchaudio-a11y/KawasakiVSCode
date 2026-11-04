' File:        ConsoleKeySourceTests.vb
' Project:     KawasakiVSCode.Tests
' Description: ConsoleKeySource.KeyToBytes: the bytes each console key gives when the console is read as
'              keys, Ctrl+C taken as input (research R8, T084).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports KawasakiVSCode.Cli
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Cli

    ''' <summary>
    ''' The key mapping of research R8. Brought forward from User Story 4 on 2026-11-04: in VS Code's terminal,
    ''' with the console read as a stream, Ctrl+C at a load question ended the read with no bytes, and khi took
    ''' it as the end of input. Read as keys, Ctrl+C is the byte 03, which PersonInput drops.
    ''' </summary>
    <TestClass>
    Public Class ConsoleKeySourceTests

        ''' <summary>Enter gives 0d 0a, as capture (a) shows Enter.</summary>
        <TestMethod>
        Public Sub EnterGivesCrLf()
            CollectionAssert.AreEqual(New Byte() {&HD, &HA}, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo(ChrW(13), ConsoleKey.Enter, False, False, False)))
        End Sub

        ' Fire demonstration (Article VII), 2026-11-04: made KeyToBytes give nothing for Ctrl+C; this test failed
        ' with "CollectionAssert.AreEqual failed. Different number of elements.". Restored the file; it passed.
        ''' <summary>Ctrl+C gives 03, whatever character the console reports with it.</summary>
        <TestMethod>
        Public Sub ControlCGives03()
            CollectionAssert.AreEqual(New Byte() {&H3}, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo(ChrW(3), ConsoleKey.C, False, False, True)))
            CollectionAssert.AreEqual(New Byte() {&H3}, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo(ChrW(0), ConsoleKey.C, False, False, True)))
        End Sub

        ''' <summary>Ctrl+] gives 1d.</summary>
        <TestMethod>
        Public Sub ControlBracketGives1d()
            CollectionAssert.AreEqual(New Byte() {&H1D}, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo(ChrW(&H1D), ConsoleKey.Oem6, False, False, True)))
            CollectionAssert.AreEqual(New Byte() {&H1D}, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo(ChrW(0), ConsoleKey.Oem6, False, False, True)))
        End Sub

        ''' <summary>A printable key gives its ASCII byte, and Backspace gives 08.</summary>
        <TestMethod>
        Public Sub APrintableKeyGivesItsByte()
            CollectionAssert.AreEqual(New Byte() {&H31}, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo("1"c, ConsoleKey.D1, False, False, False)))
            CollectionAssert.AreEqual(New Byte() {&H41}, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo("A"c, ConsoleKey.A, True, False, False)))
            CollectionAssert.AreEqual(New Byte() {&H8}, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo(ChrW(8), ConsoleKey.Backspace, False, False, False)))
        End Sub

        ''' <summary>A key with no ASCII byte, such as an arrow or a character outside ASCII, gives nothing.</summary>
        <TestMethod>
        Public Sub AKeyWithNoAsciiByteGivesNothing()
            Assert.AreEqual(0, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo(ChrW(0), ConsoleKey.LeftArrow, False, False, False)).Length)
            Assert.AreEqual(0, ConsoleKeySource.KeyToBytes(New ConsoleKeyInfo(ChrW(&HE9), ConsoleKey.E, False, False, False)).Length)
        End Sub

    End Class

End Namespace
