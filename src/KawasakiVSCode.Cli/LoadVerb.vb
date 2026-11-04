' File:        LoadVerb.vb
' Project:     KawasakiVSCode.Cli
' Description: Wires khi load: the refusals before connecting, then connect, log in and LoadOperation,
'              then khi's own lines, the FR-020 message and the result line (FR-016 to FR-021, FR-028,
'              FR-042, FR-044).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Text
Imports KawasakiVSCode.Core.Commands
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Core.Reporting
Imports KawasakiVSCode.Core.Robots
Imports KawasakiVSCode.Core.Session
Imports KawasakiVSCode.Core.Terminal
Imports KawasakiVSCode.Core.Transfer

''' <summary>
''' khi load (contracts/cli.md). Before connecting, each by its own door: a missing or unreadable file (FR-019), a byte a
''' block cannot carry (TransferFraming, FR-042), a name outside ASCII (ControllerCommand, FR-044), and the
''' robot (RobotSelection, Article IX). Then the LOAD itself (LoadOperation), and on stdout a file-reference
''' line for each error line (FR-018) and the result line (FR-028).
''' </summary>
Public NotInheritable Class LoadVerb

    Private Const NotFinished As String = "khi: the load did not finish and the controller's memory may have changed"

    Private Shared ReadOnly LineEnd As Byte() = {&HD, &HA}

    Private Sub New()
    End Sub

    ''' <summary>Runs khi load.</summary>
    ''' <param name="parsed">The parsed command line, verb load.</param>
    ''' <param name="input">The person's input, read only at a question.</param>
    ''' <param name="output">stdout, raw bytes.</param>
    ''' <param name="errors">stderr.</param>
    ''' <param name="timeouts">The timeout for each wait step.</param>
    ''' <returns>The exit code: 0 only on (0 errors) with no question asked (FR-017).</returns>
    ''' <exception cref="KhiRefusal">A refusal before connecting, exit 2: a missing or unreadable file is refused like any other, since the whole file is read before connecting.</exception>
    Public Shared Function Run(parsed As ParsedArguments, input As Stream, output As Stream, errors As TextWriter, timeouts As Timeouts) As Integer
        Dim filePath As String = parsed.FilePath
        If Not File.Exists(filePath) Then Throw New KhiRefusal(ExitCode.RefusedBeforeConnecting, "khi: " & filePath & " does not exist")
        Dim content As Byte()
        Try
            content = File.ReadAllBytes(filePath)
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
            Throw New KhiRefusal(ExitCode.RefusedBeforeConnecting, "khi: cannot read " & filePath & ": " & ex.Message)
        End Try
        TransferFraming.CheckSendable(filePath, content)
        Dim command As ControllerCommand = ControllerCommand.Load(filePath)
        Dim robot As RobotEntry = RobotSelection.Resolve(RobotsFile.Load(Environment.CurrentDirectory), parsed.Robot)

        Dim operation As LoadOperation = Nothing
        Dim failure As KhiRefusal = Nothing
        Try
            Using connection As ControllerConnection = ControllerConnection.Connect(robot, timeouts)
                LoginSequence.Run(connection)
                operation = New LoadOperation(connection, content, New PersonInput(input), output)
                operation.Run(command)
            End Using
        Catch refusal As KhiRefusal
            failure = refusal
        End Try

        If failure IsNot Nothing Then
            errors.WriteLine(failure.Message)
            If operation IsNot Nothing AndAlso operation.CommandSent Then errors.WriteLine(NotFinished)
        ElseIf operation.NoAnswer Then
            errors.WriteLine("khi: no answer to the controller's question; robot " & robot.Name & " may still be waiting on it and may need a restart")
        End If
        errors.Flush()

        Dim lines As New List(Of String)()
        Dim outcome As TransferOutcome
        If operation Is Nothing Then
            outcome = New TransferOutcome("load", False, robot.Name, filePath, 0, content.Length, New List(Of String)().AsReadOnly(), Nothing, failure.ExitCode)
        Else
            For Each line As String In operation.ErrorLines
                lines.Add(filePath & ": " & line)
            Next
            outcome = operation.Outcome(robot.Name, filePath, failure)
        End If
        lines.Add(ResultLine.Format(outcome))
        WriteLines(output, lines, operation Is Nothing OrElse operation.AtLineStart)
        Return CInt(outcome.ExitCode)
    End Function

    ''' <summary>Writes khi's own lines to stdout, each on a line of its own, ending 0d 0a.</summary>
    Private Shared Sub WriteLines(output As Stream, lines As IEnumerable(Of String), atLineStart As Boolean)
        Dim bytes As New List(Of Byte)()
        If Not atLineStart Then bytes.AddRange(LineEnd)
        For Each line As String In lines
            bytes.AddRange(Encoding.UTF8.GetBytes(line))
            bytes.AddRange(LineEnd)
        Next
        output.Write(bytes.ToArray(), 0, bytes.Count)
        output.Flush()
    End Sub

End Class
