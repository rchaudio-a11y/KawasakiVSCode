' File:        ConsoleKeySource.vb
' Project:     KawasakiVSCode.Cli
' Description: The console's keys as a stream of bytes, Ctrl+C taken as input rather than as a signal, for
'              when input is not redirected (research R8, T084, T087).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO

''' <summary>
''' The console's keys, read with Console.ReadKey and Ctrl+C taken as input, presented as a read-only stream of
''' the bytes KeyToBytes gives (research R8). Main uses it when input is not redirected. In VS Code's terminal,
''' reading the console as a stream turned Ctrl+C at a load question into the end of input, and khi closed the
''' session at the question (Architect's check, 2026-11-04); read as keys, Ctrl+C is the byte 03, which
''' PersonInput drops. Brought forward from User Story 4. It echoes nothing; PersonInput echoes the line it
''' collects.
''' </summary>
Public NotInheritable Class ConsoleKeySource
    Inherits Stream

    Private ReadOnly _previous As Boolean
    Private _key As Byte() = Array.Empty(Of Byte)()
    Private _next As Integer

    ''' <summary>Takes Ctrl+C as input for as long as this source is open.</summary>
    Public Sub New()
        _previous = Console.TreatControlCAsInput
        Console.TreatControlCAsInput = True
    End Sub

    ''' <summary>The bytes a key gives: Enter 0d 0a, Ctrl+C 03, Ctrl+] 1d, and any other key its ASCII byte (research R8).</summary>
    ''' <param name="key">The key, as Console.ReadKey reports it.</param>
    ''' <returns>The bytes; none for a key with no ASCII byte, such as an arrow.</returns>
    Public Shared Function KeyToBytes(key As ConsoleKeyInfo) As Byte()
        Dim control As Boolean = (key.Modifiers And ConsoleModifiers.Control) <> 0
        If key.Key = ConsoleKey.Enter Then Return New Byte() {&HD, &HA}
        If control AndAlso key.Key = ConsoleKey.C Then Return New Byte() {&H3}
        If control AndAlso key.Key = ConsoleKey.Oem6 Then Return New Byte() {&H1D}
        Dim code As Integer = AscW(key.KeyChar)
        If code > 0 AndAlso code < 128 Then Return New Byte() {CByte(code)}
        Return Array.Empty(Of Byte)()
    End Function

    ''' <summary>Reads the bytes of the next keys, waiting for a key when none is left.</summary>
    ''' <param name="buffer">Where the bytes go.</param>
    ''' <param name="offset">Where in the buffer they start.</param>
    ''' <param name="count">The most bytes to read.</param>
    ''' <returns>The number of bytes read, at least one; a console has no end of input.</returns>
    Public Overrides Function Read(buffer As Byte(), offset As Integer, count As Integer) As Integer
        Do While _next >= _key.Length
            _key = KeyToBytes(Console.ReadKey(intercept:=True))
            _next = 0
        Loop
        Dim taken As Integer = Math.Min(count, _key.Length - _next)
        Array.Copy(_key, _next, buffer, offset, taken)
        _next += taken
        Return taken
    End Function

    ''' <summary>True: the keys can be read.</summary>
    Public Overrides ReadOnly Property CanRead As Boolean
        Get
            Return True
        End Get
    End Property

    ''' <summary>False: keys cannot be sought.</summary>
    Public Overrides ReadOnly Property CanSeek As Boolean
        Get
            Return False
        End Get
    End Property

    ''' <summary>False: nothing is written to the keys.</summary>
    Public Overrides ReadOnly Property CanWrite As Boolean
        Get
            Return False
        End Get
    End Property

    ''' <summary>Not supported: the keys have no length.</summary>
    Public Overrides ReadOnly Property Length As Long
        Get
            Throw New NotSupportedException()
        End Get
    End Property

    ''' <summary>Not supported: the keys have no position.</summary>
    Public Overrides Property Position As Long
        Get
            Throw New NotSupportedException()
        End Get
        Set(value As Long)
            Throw New NotSupportedException()
        End Set
    End Property

    ''' <summary>Does nothing: nothing is written.</summary>
    Public Overrides Sub Flush()
    End Sub

    ''' <summary>Not supported.</summary>
    ''' <param name="offset">Unused.</param>
    ''' <param name="origin">Unused.</param>
    ''' <returns>Never returns.</returns>
    Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
        Throw New NotSupportedException()
    End Function

    ''' <summary>Not supported.</summary>
    ''' <param name="value">Unused.</param>
    Public Overrides Sub SetLength(value As Long)
        Throw New NotSupportedException()
    End Sub

    ''' <summary>Not supported.</summary>
    ''' <param name="buffer">Unused.</param>
    ''' <param name="offset">Unused.</param>
    ''' <param name="count">Unused.</param>
    Public Overrides Sub Write(buffer As Byte(), offset As Integer, count As Integer)
        Throw New NotSupportedException()
    End Sub

    ''' <summary>Gives Ctrl+C back to the console as it was before.</summary>
    ''' <param name="disposing">True when called from Dispose.</param>
    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then Console.TreatControlCAsInput = _previous
        MyBase.Dispose(disposing)
    End Sub

End Class
