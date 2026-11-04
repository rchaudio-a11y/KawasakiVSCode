' File:        Program.vb
' Project:     KawasakiVSCode.Cli
' Description: khi's entry point: cancels Ctrl+C, then calls KhiApp.Run with the console's streams and
'              the default timeouts (FR-041, research R8, R12).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Connection

''' <summary>khi's entry point. All behaviour is in KhiApp.Run, the route the tests drive (Article XIII).</summary>
Friend Module Program

    ''' <summary>
    ''' Cancels Ctrl+C, so it neither ends the process nor reaches the controller (FR-041), then runs the command
    ''' line. When input is the console, it is read as keys (ConsoleKeySource), so Ctrl+C is the byte 03 rather
    ''' than an end of input (research R8); redirected input is read raw.
    ''' </summary>
    ''' <param name="args">The arguments after the program name.</param>
    ''' <returns>The exit code.</returns>
    Public Function Main(args As String()) As Integer
        AddHandler Console.CancelKeyPress, AddressOf CancelControlC
        Using input As Stream = If(Console.IsInputRedirected, Console.OpenStandardInput(), New ConsoleKeySource()),
              output As Stream = Console.OpenStandardOutput()
            Return KhiApp.Run(args, input, output, Console.Error, Timeouts.Defaults)
        End Using
    End Function

    Private Sub CancelControlC(sender As Object, e As ConsoleCancelEventArgs)
        e.Cancel = True
    End Sub

End Module
