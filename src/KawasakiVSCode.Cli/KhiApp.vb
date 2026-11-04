' File:        KhiApp.vb
' Project:     KawasakiVSCode.Cli
' Description: khi's one production route: parses the command line, runs the verb, and turns every
'              KhiRefusal into its message on stderr and its exit code (Article XIII, research R12).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Core.Reporting

''' <summary>
''' khi's one production route (Article XIII). Main calls Run with the console's streams; tests call it
''' with their own (research R12).
''' </summary>
Public NotInheritable Class KhiApp

    Private Sub New()
    End Sub

    ''' <summary>Runs one command line.</summary>
    ''' <param name="args">The arguments after the program name.</param>
    ''' <param name="input">The person's input: the console, or a test's bytes.</param>
    ''' <param name="output">stdout, raw bytes: the controller's bytes unaltered, and khi's file-reference and result lines.</param>
    ''' <param name="errors">stderr: khi's own messages.</param>
    ''' <param name="timeouts">The timeout for each wait step.</param>
    ''' <returns>The exit code (contracts/cli.md, Exit codes).</returns>
    Public Shared Function Run(args As String(), input As Stream, output As Stream, errors As TextWriter, timeouts As Timeouts) As Integer
        Try
            Dim parsed As ParsedArguments = ArgumentParser.Parse(args)
            If parsed.Verb = "load" Then Return LoadVerb.Run(parsed, input, output, errors, timeouts)
            Throw New KhiRefusal(ExitCode.RefusedBeforeConnecting, "khi: " & parsed.Verb & " is not implemented yet")
        Catch refusal As KhiRefusal
            errors.WriteLine(refusal.Message)
            errors.Flush()
            Return CInt(refusal.ExitCode)
        End Try
    End Function

End Class
