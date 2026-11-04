' File:        OptionSettingsTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Every project file sets OptionStrict On, OptionExplicit On, OptionInfer Off and
'              GenerateDocumentationFile true (constitution hard rules, review gate 1).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Xml.Linq
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Conformance

    ''' <summary>Every project file sets the option settings the constitution's hard rules require.</summary>
    <TestClass>
    Public Class OptionSettingsTests

        ' Fire demonstration (Article VII, T042), 2026-11-04: removed <OptionInfer>Off</OptionInfer>
        ' from src/KawasakiVSCode.Core/KawasakiVSCode.Core.vbproj; this test failed (1 failed of 1).
        ' Restored the file byte for byte; it passed (1 of 1).
        ''' <summary>OptionStrict On, OptionExplicit On, OptionInfer Off and GenerateDocumentationFile true in each of the three project files.</summary>
        <TestMethod>
        Public Sub EveryProjectFileSetsTheOptions()
            Dim required As New Dictionary(Of String, String) From {
                {"OptionStrict", "On"},
                {"OptionExplicit", "On"},
                {"OptionInfer", "Off"},
                {"GenerateDocumentationFile", "true"}}
            Dim projects As IReadOnlyList(Of String) = RepositoryPaths.Files(".vbproj")
            Assert.AreEqual(3, projects.Count, "expected the three projects of Article I")
            Dim failures As New List(Of String)
            For Each project As String In projects
                Dim document As XDocument = XDocument.Load(project)
                For Each setting As KeyValuePair(Of String, String) In required
                    Dim value As String = Nothing
                    For Each element As XElement In document.Descendants(XName.Get(setting.Key))
                        value = element.Value.Trim()
                    Next
                    If Not String.Equals(value, setting.Value, StringComparison.OrdinalIgnoreCase) Then
                        failures.Add(Path.GetFileName(project) & ": " & setting.Key & " is '" & value & "', not '" & setting.Value & "'")
                    End If
                Next
            Next
            Assert.AreEqual(0, failures.Count, String.Join(Environment.NewLine, failures))
        End Sub

    End Class

End Namespace
