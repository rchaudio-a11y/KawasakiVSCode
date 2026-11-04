' File:        PersonInput.vb
' Project:     KawasakiVSCode.Core
' Description: The one door for the person's keys: Ctrl+C (03) is dropped, Ctrl+] (1d) and the end of
'              input are reported, and a line typed up to Enter, Backspace erasing within it, is returned
'              with 0d 0a and echoed when asked (FR-014, FR-021, FR-041, research R8, R15).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO

Namespace Terminal

    ''' <summary>
    ''' The one door for what of the person's input may reach the controller (research R15). It reads bytes: the
    ''' console's keys as ConsoleKeySource gives them, or redirected input (research R8). A 03, Ctrl+C, is
    ''' dropped wherever it comes: it is not sent and ends nothing (FR-014, FR-041). A 1d, Ctrl+], and the end
    ''' of input are reported, never sent. In a line, 08, Backspace, erases the key before it. Enter is 0d, 0a,
    ''' or 0d 0a; a line goes to the controller with 0d 0a, as captures (n) and (o) show the answer. When the
    ''' keys come from the console, nothing else shows them, so the line is echoed as it is typed (Architect,
    ''' 2026-11-04).
    ''' </summary>
    Public NotInheritable Class PersonInput

        Private Const ControlC As Integer = &H3
        Private Const ControlBracket As Integer = &H1D
        Private Const Backspace As Integer = &H8
        Private Const Cr As Integer = &HD
        Private Const Lf As Integer = &HA

        ''' <summary>What erases one key on the screen: back, a space over it, back again.</summary>
        Private Shared ReadOnly EraseOnScreen As Byte() = {&H8, &H20, &H8}

        Private Shared ReadOnly LineEnd As Byte() = {&HD, &HA}

        Private ReadOnly _input As Stream
        Private ReadOnly _echo As Stream
        Private _afterCr As Boolean
        Private _escaped As Boolean

        ''' <summary>Reads the person's input from a stream, echoing nothing, as for redirected input.</summary>
        ''' <param name="input">The person's input.</param>
        Public Sub New(input As Stream)
            Me.New(input, Nothing)
        End Sub

        ''' <summary>Reads the person's input from a stream, echoing each line as it is typed.</summary>
        ''' <param name="input">The person's input.</param>
        ''' <param name="echo">Where the typed line is echoed, the console's output; Nothing to echo nothing.</param>
        Public Sub New(input As Stream, echo As Stream)
            _input = input
            _echo = echo
        End Sub

        ''' <summary>
        ''' Reads one line the person typed, up to Enter, dropping every 03 and erasing the key before each 08. A
        ''' 0a straight after the 0d that ended the line before is the rest of that Enter, not a line.
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
                    Case Backspace
                        If line.Count > 0 Then
                            line.RemoveAt(line.Count - 1)
                            Echo(EraseOnScreen)
                        End If
                    Case Lf
                        If Not afterCr OrElse line.Count > 0 Then Return Finish(line)
                    Case Cr
                        _afterCr = True
                        Return Finish(line)
                    Case Else
                        line.Add(CByte(value))
                        Echo(New Byte() {CByte(value)})
                End Select
            Loop
        End Function

        ''' <summary>True when the last ReadLine ended at Ctrl+], False when it ended because the input did.</summary>
        Public ReadOnly Property Escaped As Boolean
            Get
                Return _escaped
            End Get
        End Property

        Private Function Finish(line As List(Of Byte)) As Byte()
            Echo(LineEnd)
            line.AddRange(LineEnd)
            Return line.ToArray()
        End Function

        Private Sub Echo(bytes As Byte())
            If _echo Is Nothing Then Return
            _echo.Write(bytes, 0, bytes.Length)
            _echo.Flush()
        End Sub

    End Class

End Namespace
