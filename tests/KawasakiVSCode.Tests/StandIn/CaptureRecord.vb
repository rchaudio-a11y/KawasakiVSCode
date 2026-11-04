' File:        CaptureRecord.vb
' Project:     KawasakiVSCode.Tests
' Description: One record of a capture's exchange.txt: its number, direction, time and bytes
'              (contracts/capture-format.md).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace StandIn

    ''' <summary>One chunk of bytes as the relay read it (contracts/capture-format.md, data-model.md CaptureRecord).</summary>
    Public NotInheritable Class CaptureRecord

        ''' <summary>Makes a record.</summary>
        ''' <param name="number">The record's number, counted from 1 in the order exchange.txt holds the records.</param>
        ''' <param name="direction">C toward the controller, S from it.</param>
        ''' <param name="time">Seconds since the first byte.</param>
        ''' <param name="bytes">The bytes, as the relay read them in one go.</param>
        Public Sub New(number As Integer, direction As Char, time As Double, bytes As Byte())
            Me.Number = number
            Me.Direction = direction
            Me.Time = time
            Me.Bytes = bytes
        End Sub

        ''' <summary>The record's number, from 1, as the capture notes count them.</summary>
        Public ReadOnly Property Number As Integer

        ''' <summary>C toward the controller, S from it.</summary>
        Public ReadOnly Property Direction As Char

        ''' <summary>Seconds since the first byte, to the millisecond.</summary>
        Public ReadOnly Property Time As Double

        ''' <summary>The bytes.</summary>
        Public ReadOnly Property Bytes As Byte()

    End Class

End Namespace
