' File:        PersonInput.vb
' Project:     KawasakiVSCode.Core
' Description: The one door for the person's keys: Ctrl+C (03) is dropped, Ctrl+] (1d) and the end of
'              input are reported, and a line typed up to Enter is returned with 0d 0a (FR-014, FR-021,
'              FR-041, research R8, R15).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO

Namespace Terminal

    ''' <summary>
    ''' The one door for what of the person's input may reach the controller (research R15). It reads raw bytes,
    ''' as from redirected input (research R8). A 03, Ctrl+C, is dropped wherever it comes: it is not sent and
    ''' ends nothing (FR-014, FR-041). A 1d, Ctrl+], and the end of input are reported, never sent. Enter is
    ''' 0d, 0a, or 0d 0a; a line goes to the controller with 0d 0a, as captures (n) and (o) show the answer.
    ''' </summary>
    Public NotInheritable Class PersonInput

        Private Const ControlC As Integer = &H3
        Private Const ControlBracket As Integer = &H1D
        Private Const Cr As Integer = &HD
        Private Const Lf As Integer = &HA

        Private ReadOnly _input As Stream
        Private _afterCr As Boolean
        Private _escaped As Boolean

        ''' <summary>Reads the person's input from a stream.</summary>
        ''' <param name="input">The console's input, or a test's bytes.</param>
        Public Sub New(input As Stream)
            _input = input
        End Sub

        ''' <summary>
        ''' Reads one line the person typed, up to Enter, dropping every 03. A 0a straight after the 0d that ended
        ''' the line before is the rest of that Enter, not a line.
        ''' </summary>
        ''' <returns>The line's bytes followed by 0d 0a; Nothing when Ctrl+] came or the input ended first (see Escaped).</returns>
        Public Function ReadLine() As Byte()
            Dim line As New List(Of Byte)()
            Do
                Dim value As Integer = _input.ReadByte()
                Dim afterCr As Boolean = _afterCr
                _afterCr = False
                Select Case value
                    Case -1
                        _escaped = False
                        Return Nothing
                    Case ControlBracket
                        _escaped = True
                        Return Nothing
                    Case ControlC
                        _afterCr = afterCr
                    Case Lf
                        If Not afterCr OrElse line.Count > 0 Then Return Finish(line)
                    Case Cr
                        _afterCr = True
                        Return Finish(line)
                    Case Else
                        line.Add(CByte(value))
                End Select
            Loop
        End Function

        ''' <summary>True when the last ReadLine ended at Ctrl+], False when it ended because the input did.</summary>
        Public ReadOnly Property Escaped As Boolean
            Get
                Return _escaped
            End Get
        End Property

        Private Shared Function Finish(line As List(Of Byte)) As Byte()
            line.Add(CByte(Cr))
            line.Add(CByte(Lf))
            Return line.ToArray()
        End Function

    End Class

End Namespace
