' File:        KhiWorkspace.vb
' Project:     KawasakiVSCode.Tests
' Description: A temporary folder a test runs khi in: the files it copies there, and a khi.robots.json
'              whose one robot, sim, is the default and points at a port the test gives (research R12).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Globalization
Imports System.IO
Imports System.Threading.Tasks
Imports KawasakiVSCode.Cli
Imports KawasakiVSCode.Core.Connection
Imports KawasakiVSCode.Tests.StandIn
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Route

    ''' <summary>
    ''' A temporary folder for one test. Its khi.robots.json names one robot, sim, the default, at 127.0.0.1
    ''' and the port given, so khi reads it from the current folder as it does in a program repository
    ''' (contracts/robots-file.md). Runs go through KhiApp.Run, the code Main runs, with this folder as the
    ''' current folder (research R12). Dispose restores the current folder and deletes this one.
    ''' </summary>
    Public NotInheritable Class KhiWorkspace
        Implements IDisposable

        Private Shared ReadOnly RunLimit As TimeSpan = TimeSpan.FromSeconds(30)

        Private ReadOnly _previousFolder As String

        ''' <summary>Makes the folder and its robots file.</summary>
        ''' <param name="port">The port robot sim is at: a stand-in's, or one nobody listens on.</param>
        Public Sub New(port As Integer)
            Folder = Path.Combine(Path.GetTempPath(), "khi-test-" & Guid.NewGuid().ToString("N"))
            Directory.CreateDirectory(Folder)
            File.WriteAllText(Path.Combine(Folder, "khi.robots.json"),
                              "{ ""default"": ""sim"", ""robots"": [ { ""name"": ""sim"", ""address"": ""127.0.0.1"", ""port"": " &
                              port.ToString(CultureInfo.InvariantCulture) & ", ""simulator"": true } ] }")
            _previousFolder = Environment.CurrentDirectory
        End Sub

        ''' <summary>The folder's full path.</summary>
        Public ReadOnly Property Folder As String

        ''' <summary>Copies a file from a capture's files/ folder into the workspace, under the same name.</summary>
        ''' <param name="capture">The capture's folder name.</param>
        ''' <param name="name">The file's name in files/.</param>
        ''' <returns>The copy's full path.</returns>
        Public Function CopyFromCapture(capture As String, name As String) As String
            Dim target As String = Path.Combine(Folder, name)
            File.Copy(Path.Combine(CaptureReader.Folder(capture), "files", name), target)
            Return target
        End Function

        ''' <summary>Copies a fixture from Fixtures/ into the workspace, under the same name.</summary>
        ''' <param name="fixture">The fixture's folder under Fixtures/.</param>
        ''' <param name="name">The file's name in that folder.</param>
        ''' <returns>The copy's full path.</returns>
        Public Function CopyFixture(fixture As String, name As String) As String
            Dim target As String = Path.Combine(Folder, name)
            File.Copy(FixturePath(fixture, name), target)
            Return target
        End Function

        ''' <summary>The full path of a fixture in the test output folder, where the build copies Fixtures/.</summary>
        ''' <param name="fixture">The fixture's folder under Fixtures/.</param>
        ''' <param name="name">The file's name in that folder.</param>
        ''' <returns>The path.</returns>
        Public Shared Function FixturePath(fixture As String, name As String) As String
            Return Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture, name)
        End Function

        ''' <summary>Runs khi to its end, failing the test if it has not ended within 30 seconds (INV-5).</summary>
        ''' <param name="timeouts">The timeout for each wait step.</param>
        ''' <param name="input">The person's input.</param>
        ''' <param name="args">The command line after the program name.</param>
        ''' <returns>What the run gave.</returns>
        Public Function Run(timeouts As Timeouts, input As Stream, ParamArray args As String()) As RunResult
            Return Finish(Start(timeouts, input, args))
        End Function

        ''' <summary>Starts khi on its own task, so the test can type while it runs.</summary>
        ''' <param name="timeouts">The timeout for each wait step.</param>
        ''' <param name="input">The person's input; a pipe when the test types later.</param>
        ''' <param name="args">The command line after the program name.</param>
        ''' <returns>The running khi.</returns>
        Public Function Start(timeouts As Timeouts, input As Stream, ParamArray args As String()) As Task(Of RunResult)
            Return Start(timeouts, input, New MemoryStream(), args)
        End Function

        ''' <summary>
        ''' Starts khi on its own task with a stdout the test gives, so the test can see what khi has printed while
        ''' it waits, such as the controller's question.
        ''' </summary>
        ''' <param name="timeouts">The timeout for each wait step.</param>
        ''' <param name="input">The person's input; a pipe when the test types later.</param>
        ''' <param name="output">stdout; read it only while khi waits on the person, or once it has ended.</param>
        ''' <param name="args">The command line after the program name.</param>
        ''' <returns>The running khi.</returns>
        Public Function Start(timeouts As Timeouts, input As Stream, output As MemoryStream, ParamArray args As String()) As Task(Of RunResult)
            Environment.CurrentDirectory = Folder
            Return Task.Run(Function() As RunResult
                                Using errors As New StringWriter()
                                    Dim code As Integer = KhiApp.Run(args, input, output, errors, timeouts)
                                    Return New RunResult(code, output.ToArray(), errors.ToString())
                                End Using
                            End Function)
        End Function

        ''' <summary>Waits for a started khi to end, failing the test if it has not ended within 30 seconds.</summary>
        ''' <param name="running">The running khi.</param>
        ''' <returns>What the run gave.</returns>
        Public Shared Function Finish(running As Task(Of RunResult)) As RunResult
            If Not running.Wait(RunLimit) Then Assert.Fail("khi had not ended after 30 s: a wait without a timeout")
            Return running.Result
        End Function

        ''' <summary>Restores the current folder and deletes the workspace.</summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            Environment.CurrentDirectory = _previousFolder
            Try
                Directory.Delete(Folder, recursive:=True)
            Catch ex As IOException
                ' A file still held open is left for the system's temporary-file clean-up.
            End Try
        End Sub

    End Class

End Namespace
