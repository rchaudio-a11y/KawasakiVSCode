' File:        RepositoryPaths.vb
' Project:     KawasakiVSCode.Tests
' Description: Finds the repository root and lists the project and source files the conformance
'              tests read.
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO

Namespace Conformance

    ''' <summary>
    ''' Finds the repository root and lists the project and source files the conformance tests read.
    ''' </summary>
    Public NotInheritable Class RepositoryPaths

        Private Sub New()
        End Sub

        ''' <summary>The folder holding KawasakiVSCode.slnx, found by walking up from the test assembly's folder.</summary>
        ''' <returns>The repository root's full path.</returns>
        Public Shared Function Root() As String
            Dim folder As DirectoryInfo = New DirectoryInfo(AppContext.BaseDirectory)
            Do While folder IsNot Nothing
                If File.Exists(Path.Combine(folder.FullName, "KawasakiVSCode.slnx")) Then Return folder.FullName
                folder = folder.Parent
            Loop
            Throw New InvalidOperationException("No KawasakiVSCode.slnx above " & AppContext.BaseDirectory)
        End Function

        ''' <summary>Every file with the given extension under src/ and tests/, skipping bin/ and obj/.</summary>
        ''' <param name="extension">The extension with its dot, such as ".vb".</param>
        ''' <returns>The full paths, in no particular order.</returns>
        Public Shared Function Files(extension As String) As IReadOnlyList(Of String)
            Dim found As New List(Of String)
            For Each top As String In New String() {"src", "tests"}
                Dim folder As String = Path.Combine(Root(), top)
                If Not Directory.Exists(folder) Then Continue For
                For Each candidate As String In Directory.EnumerateFiles(folder, "*" & extension, SearchOption.AllDirectories)
                    If Not String.Equals(Path.GetExtension(candidate), extension, StringComparison.OrdinalIgnoreCase) Then Continue For
                    If IsBuildOutput(candidate) Then Continue For
                    found.Add(candidate)
                Next
            Next
            Return found
        End Function

        Private Shared Function IsBuildOutput(candidate As String) As Boolean
            For Each part As String In candidate.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                If String.Equals(part, "bin", StringComparison.OrdinalIgnoreCase) Then Return True
                If String.Equals(part, "obj", StringComparison.OrdinalIgnoreCase) Then Return True
            Next
            Return False
        End Function

    End Class

End Namespace
