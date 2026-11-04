' File:        RobotsFile.vb
' Project:     KawasakiVSCode.Core
' Description: Reads and checks khi.robots.json from one folder with System.Text.Json
'              (contracts/robots-file.md, research R7, Article IX).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Globalization
Imports System.IO
Imports System.Text.Json
Imports KawasakiVSCode.Core.Reporting

Namespace Robots

    ''' <summary>
    ''' khi.robots.json, read from one folder and checked against every rule in contracts/robots-file.md.
    ''' Every broken rule is refused with what is wrong and where, exit 2, before connecting.
    ''' </summary>
    Public NotInheritable Class RobotsFile

        ''' <summary>The file's name.</summary>
        Public Const FileName As String = "khi.robots.json"

        Private Const Prefix As String = "khi: " & FileName & ": "

        Private Shared ReadOnly TopKeys As String() = {"default", "robots"}

        Private Shared ReadOnly EntryKeys As String() = {"name", "address", "port", "simulator"}

        Private Sub New(robots As IReadOnlyList(Of RobotEntry), defaultName As String)
            Me.Robots = robots
            Me.DefaultName = defaultName
        End Sub

        ''' <summary>The entries, in the file's order.</summary>
        Public ReadOnly Property Robots As IReadOnlyList(Of RobotEntry)

        ''' <summary>The default entry's name as the file writes it, or Nothing when the file sets none.</summary>
        Public ReadOnly Property DefaultName As String

        ''' <summary>Reads khi.robots.json from a folder; khi passes the current folder only.</summary>
        ''' <param name="folder">The folder holding the file.</param>
        ''' <returns>The checked file.</returns>
        ''' <exception cref="KhiRefusal">The file is missing or breaks a rule; exit 2.</exception>
        Public Shared Function Load(folder As String) As RobotsFile
            Dim filePath As String = Path.Combine(folder, FileName)
            If Not File.Exists(filePath) Then Throw Refuse("khi: no " & FileName & " in " & folder)
            Dim text As String = File.ReadAllText(filePath)
            Dim document As JsonDocument
            Try
                document = JsonDocument.Parse(text)
            Catch ex As JsonException
                Throw Refuse(Prefix & "not valid JSON: " & ex.Message)
            End Try
            Using document
                Return Read(document.RootElement)
            End Using
        End Function

        Private Shared Function Read(root As JsonElement) As RobotsFile
            If root.ValueKind <> JsonValueKind.Object Then Throw Refuse(Prefix & "the file must hold one JSON object")
            CheckKeys(root, TopKeys, "")
            Dim list As JsonElement
            If Not root.TryGetProperty("robots", list) OrElse list.ValueKind <> JsonValueKind.Array OrElse list.GetArrayLength() = 0 Then
                Throw Refuse(Prefix & """robots"" must be a list with at least one entry")
            End If
            Dim entries As New List(Of RobotEntry)()
            Dim index As Integer = 0
            For Each item As JsonElement In list.EnumerateArray()
                Dim entry As RobotEntry = ReadEntry(item, index)
                For earlier As Integer = 0 To entries.Count - 1
                    If String.Equals(entries(earlier).Name, entry.Name, StringComparison.OrdinalIgnoreCase) Then
                        Throw Refuse(Prefix & Where(index) & ".name """ & entry.Name & """ repeats " & Where(earlier) & ".name, ignoring case")
                    End If
                Next
                entries.Add(entry)
                index += 1
            Next
            Dim defaultName As String = Nothing
            Dim defaultElement As JsonElement
            If root.TryGetProperty("default", defaultElement) Then
                If defaultElement.ValueKind <> JsonValueKind.String Then Throw Refuse(Prefix & """default"" must be a robot's name")
                defaultName = defaultElement.GetString()
                If Not entries.Exists(Function(entry As RobotEntry) String.Equals(entry.Name, defaultName, StringComparison.OrdinalIgnoreCase)) Then
                    Throw Refuse(Prefix & "default """ & defaultName & """ names no robot")
                End If
            End If
            Return New RobotsFile(entries.AsReadOnly(), defaultName)
        End Function

        Private Shared Function ReadEntry(item As JsonElement, index As Integer) As RobotEntry
            Dim at As String = Where(index)
            If item.ValueKind <> JsonValueKind.Object Then Throw Refuse(Prefix & at & " must be an object")
            CheckKeys(item, EntryKeys, at & ": ")
            Dim value As JsonElement
            Dim name As String = Nothing
            If item.TryGetProperty("name", value) AndAlso value.ValueKind = JsonValueKind.String Then name = value.GetString()
            If String.IsNullOrWhiteSpace(name) Then Throw Refuse(Prefix & at & ".name is required and must not be empty")
            Dim address As String = Nothing
            If item.TryGetProperty("address", value) AndAlso value.ValueKind = JsonValueKind.String Then address = value.GetString()
            If String.IsNullOrWhiteSpace(address) Then Throw Refuse(Prefix & at & ".address is required: a host name or IP address")
            Dim port As Integer
            If Not item.TryGetProperty("port", value) OrElse value.ValueKind <> JsonValueKind.Number OrElse Not value.TryGetInt32(port) OrElse port < 1 OrElse port > 65535 Then
                Throw Refuse(Prefix & at & ".port must be a whole number from 1 to 65535")
            End If
            Dim simulator As Boolean
            If item.TryGetProperty("simulator", value) AndAlso value.ValueKind = JsonValueKind.True Then
                simulator = True
            ElseIf item.TryGetProperty("simulator", value) AndAlso value.ValueKind = JsonValueKind.False Then
                simulator = False
            Else
                Throw Refuse(Prefix & at & ".simulator must be true or false")
            End If
            Return New RobotEntry(name, address, port, simulator)
        End Function

        Private Shared Sub CheckKeys(element As JsonElement, known As String(), at As String)
            For Each member As JsonProperty In element.EnumerateObject()
                If Array.IndexOf(known, member.Name) < 0 Then Throw Refuse(Prefix & at & "unknown key """ & member.Name & """")
            Next
        End Sub

        Private Shared Function Where(index As Integer) As String
            Return "robots[" & index.ToString(CultureInfo.InvariantCulture) & "]"
        End Function

        Private Shared Function Refuse(message As String) As KhiRefusal
            Return New KhiRefusal(ExitCode.RefusedBeforeConnecting, message)
        End Function

    End Class

End Namespace
