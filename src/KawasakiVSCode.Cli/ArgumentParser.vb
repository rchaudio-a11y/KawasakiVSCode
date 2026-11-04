' File:        ArgumentParser.vb
' Project:     KawasakiVSCode.Cli
' Description: The hand-written parser for khi's three verbs and two options; anything else is refused
'              by name before connecting (contracts/cli.md, research R6).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports KawasakiVSCode.Core.Commands
Imports KawasakiVSCode.Core.Reporting

''' <summary>
''' Parses khi's command line (contracts/cli.md, Synopsis; research R6). An option may come before or after
''' the file and may appear once. Every refusal is exit 2, before connecting.
''' </summary>
Public NotInheritable Class ArgumentParser

    ''' <summary>The usage, as khi prints it when no verb is given.</summary>
    Public Shared ReadOnly Property Usage As String =
        "usage: khi term [--robot <name>]" & Environment.NewLine &
        "       khi load <file> [--robot <name>]" & Environment.NewLine &
        "       khi save <file> [--qualifier <q>] [--robot <name>]"

    Private Const RobotOption As String = "--robot"

    Private Const QualifierOption As String = "--qualifier"

    Private Sub New()
    End Sub

    ''' <summary>Parses the arguments after the program name.</summary>
    ''' <param name="args">The arguments.</param>
    ''' <returns>The parsed command line.</returns>
    ''' <exception cref="KhiRefusal">The command line is malformed or names an unsupported qualifier; exit 2.</exception>
    Public Shared Function Parse(args As String()) As ParsedArguments
        If args.Length = 0 Then Throw Refuse("khi: no verb given" & Environment.NewLine & Usage)
        Dim verb As String = args(0)
        If verb <> "term" AndAlso verb <> "load" AndAlso verb <> "save" Then
            Throw Refuse("khi: unknown verb " & verb)
        End If
        Dim takesFile As Boolean = verb <> "term"
        Dim filePath As String = Nothing
        Dim robot As String = Nothing
        Dim qualifierText As String = Nothing
        Dim seen As New HashSet(Of String)(StringComparer.Ordinal)
        Dim index As Integer = 1
        Do While index < args.Length
            Dim item As String = args(index)
            If item.StartsWith("-", StringComparison.Ordinal) Then
                If item <> RobotOption AndAlso item <> QualifierOption Then Throw Refuse("khi: unknown option " & item)
                If item = QualifierOption AndAlso verb <> "save" Then Throw Refuse("khi: khi " & verb & " does not take " & item)
                If Not seen.Add(item) Then Throw Refuse("khi: " & item & " is given twice")
                If index + 1 >= args.Length OrElse args(index + 1).StartsWith("-", StringComparison.Ordinal) Then
                    Throw Refuse("khi: " & item & " needs a value")
                End If
                If item = RobotOption Then robot = args(index + 1) Else qualifierText = args(index + 1)
                index += 2
            Else
                If Not takesFile OrElse filePath IsNot Nothing Then Throw Refuse("khi: unexpected argument " & item)
                filePath = item
                index += 1
            End If
        Loop
        If takesFile AndAlso filePath Is Nothing Then Throw Refuse("khi: khi " & verb & " needs a <file>")
        Dim qualifier As SaveQualifier = Nothing
        If verb = "save" Then qualifier = SaveQualifier.Parse(qualifierText)
        Return New ParsedArguments(verb, filePath, robot, qualifier)
    End Function

    Private Shared Function Refuse(message As String) As KhiRefusal
        Return New KhiRefusal(ExitCode.RefusedBeforeConnecting, message)
    End Function

End Class
