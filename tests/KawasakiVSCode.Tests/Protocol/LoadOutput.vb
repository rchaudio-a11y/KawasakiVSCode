' File:        LoadOutput.vb
' Project:     KawasakiVSCode.Tests
' Description: What khi load's stdout holds after a replay: the controller's text taken from the capture,
'              unaltered, then khi's own lines (contracts/cli.md, Output streams; INV-7).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Text
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Protocol

    ''' <summary>
    ''' The stdout a LOAD replay must give, built from the capture rather than written by hand (INV-3): the
    ''' controller's bytes from the LOAD command's echo on, with the transfer blocks taken out, then khi's
    ''' file-reference lines and result line, each on a line of its own.
    ''' </summary>
    Public NotInheritable Class LoadOutput

        Private Shared ReadOnly LineEnd As Byte() = {&HD, &HA}

        Private Sub New()
        End Sub

        ''' <summary>
        ''' The controller's text in a capture: the bytes of its S records in the ranges given, in order, with
        ''' every transfer block (05 02, a letter, the body, 17) taken out.
        ''' </summary>
        ''' <param name="capture">The capture's folder name.</param>
        ''' <param name="ranges">The records whose S bytes to take, numbered as the note numbers them.</param>
        ''' <returns>The text, byte for byte.</returns>
        Public Shared Function ControllerText(capture As String, ParamArray ranges As RecordRange()) As Byte()
            Dim sent As New List(Of Byte)()
            For Each record As CaptureRecord In CaptureReader.Read(capture)
                If record.Direction = "S"c AndAlso ranges.Any(Function(range As RecordRange) range.Contains(record.Number)) Then
                    sent.AddRange(record.Bytes)
                End If
            Next
            Dim text As New List(Of Byte)()
            Dim index As Integer = 0
            Do While index < sent.Count
                If sent(index) = &H5 AndAlso index + 1 < sent.Count AndAlso sent(index + 1) = &H2 Then
                    Dim closing As Integer = sent.IndexOf(CByte(&H17), index)
                    If closing < 0 Then Throw New InvalidOperationException("capture " & capture & ": a transfer block with no closing 17")
                    index = closing + 1
                Else
                    text.Add(sent(index))
                    index += 1
                End If
            Loop
            Return text.ToArray()
        End Function

        ''' <summary>The controller's text followed by khi's lines, each starting a line and ending 0d 0a.</summary>
        ''' <param name="controllerText">The controller's text, from ControllerText.</param>
        ''' <param name="khiLines">khi's file-reference lines, then its result line.</param>
        ''' <returns>The whole stdout.</returns>
        Public Shared Function Expected(controllerText As Byte(), ParamArray khiLines As String()) As Byte()
            Dim all As New List(Of Byte)(controllerText)
            If all.Count > 0 AndAlso all(all.Count - 1) <> &HA Then all.AddRange(LineEnd)
            For Each line As String In khiLines
                all.AddRange(Encoding.UTF8.GetBytes(line))
                all.AddRange(LineEnd)
            Next
            Return all.ToArray()
        End Function

        ''' <summary>Asserts stdout byte for byte, showing both as text when they differ.</summary>
        ''' <param name="expected">The stdout the run must give.</param>
        ''' <param name="actual">The stdout it gave.</param>
        Public Shared Sub AssertOutput(expected As Byte(), actual As Byte())
            Assert.AreEqual(Encoding.Latin1.GetString(expected), Encoding.Latin1.GetString(actual), "stdout")
        End Sub

    End Class

End Namespace
