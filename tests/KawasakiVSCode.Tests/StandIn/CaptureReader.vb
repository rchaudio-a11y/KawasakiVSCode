' File:        CaptureReader.vb
' Project:     KawasakiVSCode.Tests
' Description: Reads a capture's exchange.txt from the test output folder into numbered records
'              (contracts/capture-format.md).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Globalization
Imports System.IO

Namespace StandIn

    ''' <summary>Reads exchange.txt (contracts/capture-format.md). The hexadecimal bytes are what counts; the preview is skipped.</summary>
    Public NotInheritable Class CaptureReader

        Private Sub New()
        End Sub

        ''' <summary>The folder of a capture in the test output folder, where the build copies Captures/.</summary>
        ''' <param name="capture">The capture's folder name, such as a-connect-login.</param>
        ''' <returns>The folder's full path.</returns>
        Public Shared Function Folder(capture As String) As String
            Return Path.Combine(AppContext.BaseDirectory, "Captures", capture)
        End Function

        ''' <summary>Reads a capture's records, numbered from 1, skipping lines that start with #.</summary>
        ''' <param name="capture">The capture's folder name, such as a-connect-login.</param>
        ''' <returns>The records in the order exchange.txt holds them.</returns>
        Public Shared Function Read(capture As String) As IReadOnlyList(Of CaptureRecord)
            Dim records As New List(Of CaptureRecord)()
            Dim lineNumber As Integer = 0
            For Each line As String In File.ReadLines(Path.Combine(Folder(capture), "exchange.txt"))
                lineNumber += 1
                If line.Length = 0 OrElse line.StartsWith("#", StringComparison.Ordinal) Then Continue For
                Dim tokens As String() = line.Split(" "c, StringSplitOptions.RemoveEmptyEntries)
                If tokens.Length < 3 OrElse (tokens(0) <> "C" AndAlso tokens(0) <> "S") Then
                    Throw New InvalidDataException(capture & " exchange.txt line " & lineNumber.ToString(CultureInfo.InvariantCulture) & " is not a record")
                End If
                Dim bytes As New List(Of Byte)()
                For index As Integer = 2 To tokens.Length - 1
                    If tokens(index).StartsWith("|", StringComparison.Ordinal) Then Exit For
                    bytes.Add(Convert.ToByte(tokens(index), 16))
                Next
                records.Add(New CaptureRecord(records.Count + 1, tokens(0)(0), Double.Parse(tokens(1), CultureInfo.InvariantCulture), bytes.ToArray()))
            Next
            Return records.AsReadOnly()
        End Function

    End Class

End Namespace
