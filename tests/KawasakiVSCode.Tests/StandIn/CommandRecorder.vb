' File:        CommandRecorder.vb
' Project:     KawasakiVSCode.Tests
' Description: Turns client bytes into the command lines the controller would run, skipping telnet
'              negotiation and transfer frames, and lists those outside the allowed set (INV-2).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Text
Imports System.Text.RegularExpressions

Namespace StandIn

    ''' <summary>
    ''' Records every client line outside telnet negotiation and transfer frames as a command (INV-2). A line
    ''' is the bytes up to 0d 0a; when it holds an 18 (CAN), as KRterm's and khi's Enter on a LOAD or SAVE
    ''' line does (D1), the command is what follows the last 18. Empty lines are not commands.
    ''' </summary>
    Public NotInheritable Class CommandRecorder

        Private Const Iac As Byte = &HFF

        Private Shared ReadOnly LoadLine As New Regex("^load \S+$", RegexOptions.CultureInvariant)

        Private Shared ReadOnly SaveLine As New Regex("^SAVE(/P|/FULL)? \S+$", RegexOptions.CultureInvariant)

        Private ReadOnly _commands As New List(Of String)()

        Private ReadOnly _line As New List(Of Byte)()

        ' 0 text, 1 after IAC, 2 the option byte after WILL, WONT, DO or DONT, 3 inside a subnegotiation,
        ' 4 after IAC inside a subnegotiation, 5 inside a transfer frame (02 to 17).
        Private _state As Integer

        ''' <summary>The commands recorded so far, in order.</summary>
        Public ReadOnly Property Commands As IReadOnlyList(Of String)
            Get
                Return _commands.AsReadOnly()
            End Get
        End Property

        ''' <summary>Adds client bytes, in the order the client sent them.</summary>
        ''' <param name="bytes">The bytes.</param>
        Public Sub Add(bytes As Byte())
            Add(bytes, 0, bytes.Length)
        End Sub

        ''' <summary>Adds part of a buffer of client bytes, in the order the client sent them.</summary>
        ''' <param name="buffer">The buffer.</param>
        ''' <param name="offset">Where the bytes start.</param>
        ''' <param name="count">How many there are.</param>
        Public Sub Add(buffer As Byte(), offset As Integer, count As Integer)
            For index As Integer = offset To offset + count - 1
                Take(buffer(index))
            Next
        End Sub

        ''' <summary>The recorded commands outside the allowed set, in order.</summary>
        ''' <param name="extraAllowed">The answer the test typed at a load question and the lines it typed in khi term.</param>
        ''' <returns>The commands khi must never have sent.</returns>
        Public Function Disallowed(extraAllowed As IEnumerable(Of String)) As IReadOnlyList(Of String)
            Dim extra As New HashSet(Of String)(extraAllowed, StringComparer.Ordinal)
            Return _commands.Where(Function(command As String) Not IsAllowed(command, extra)).ToList().AsReadOnly()
        End Function

        Private Shared Function IsAllowed(command As String, extra As HashSet(Of String)) As Boolean
            Return command = "as" OrElse LoadLine.IsMatch(command) OrElse SaveLine.IsMatch(command) OrElse extra.Contains(command)
        End Function

        Private Sub Take(value As Byte)
            Select Case _state
                Case 0
                    If value = Iac Then
                        _state = 1
                    ElseIf value = &H2 Then
                        _state = 5
                    ElseIf value = &HA Then
                        EndLine()
                    Else
                        _line.Add(value)
                    End If
                Case 1
                    If value = Iac Then
                        _line.Add(value)
                        _state = 0
                    ElseIf value = &HFA Then
                        _state = 3
                    ElseIf value >= &HFB Then
                        _state = 2
                    Else
                        _state = 0
                    End If
                Case 2
                    _state = 0
                Case 3
                    If value = Iac Then _state = 4
                Case 4
                    _state = If(value = &HF0, 0, 3)
                Case 5
                    If value = &H17 Then _state = 0
            End Select
        End Sub

        Private Sub EndLine()
            Dim bytes As Byte() = _line.ToArray()
            _line.Clear()
            Dim length As Integer = bytes.Length
            If length > 0 AndAlso bytes(length - 1) = &HD Then length -= 1
            If length = 0 Then Return
            Dim start As Integer = Array.LastIndexOf(bytes, CByte(&H18), length - 1) + 1
            Dim command As String = Encoding.Latin1.GetString(bytes, start, length - start)
            If command.Length > 0 Then _commands.Add(command)
        End Sub

    End Class

End Namespace
