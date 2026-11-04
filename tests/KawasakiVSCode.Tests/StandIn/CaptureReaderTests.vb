' File:        CaptureReaderTests.vb
' Project:     KawasakiVSCode.Tests
' Description: CaptureReader reads a capture's exchange.txt into numbered records
'              (contracts/capture-format.md).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Linq
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace StandIn

    ''' <summary>Reading exchange.txt into records (contracts/capture-format.md).</summary>
    <TestClass>
    Public Class CaptureReaderTests

        ''' <summary>Capture (a) reads as eight records, numbered from 1, with its header lines skipped.</summary>
        <TestMethod>
        Public Sub CaptureAReadsAsEightRecords()
            Dim records As IReadOnlyList(Of CaptureRecord) = CaptureReader.Read("a-connect-login")
            Assert.AreEqual(8, records.Count)
            Assert.AreEqual(1, records(0).Number)
            Assert.AreEqual("C"c, records(0).Direction)
            Assert.AreEqual(0.0, records(0).Time)
            CollectionAssert.AreEqual(New Byte() {&HFF, &HFB, &H18}, records(0).Bytes)
            Assert.AreEqual(8, records(7).Number)
            Assert.AreEqual("S"c, records(7).Direction)
            Dim last As Byte() = records(7).Bytes
            CollectionAssert.AreEqual(New Byte() {&HD, &HA, &H3E}, last.Skip(last.Length - 3).ToArray())
        End Sub

        ''' <summary>Capture (e) reads as 121 records carrying 394,969 bytes from the controller.</summary>
        <TestMethod>
        Public Sub CaptureEReadsWhole()
            Dim records As IReadOnlyList(Of CaptureRecord) = CaptureReader.Read("e-save-full")
            Assert.AreEqual(121, records.Count)
            Dim fromController As Long = 0
            For Each record As CaptureRecord In records
                If record.Direction = "S"c Then fromController += record.Bytes.Length
            Next
            Assert.AreEqual(394969L, fromController)
        End Sub

    End Class

End Namespace
