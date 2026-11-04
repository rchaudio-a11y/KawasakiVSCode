' File:        XmlDocCoverageTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Every public type, method, property, event and interface member of KawasakiVSCode.Core
'              and khi has a <summary> in the generated XML documentation file (research R4,
'              constitution 2.0.1 hard rule).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Xml.Linq
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Conformance

    ''' <summary>Every public member of Core and khi has a summary in its generated XML documentation (research R4).</summary>
    <TestClass>
    Public Class XmlDocCoverageTests

        Private Shared ReadOnly AssemblyNames As String() = {"KawasakiVSCode.Core", "khi"}

        ' Fire demonstration (Article VII, T042), 2026-11-04: deleted the <summary> of RobotEntry.Port in
        ' src/KawasakiVSCode.Core/Robots/RobotEntry.vb; this test failed naming
        ' P:KawasakiVSCode.Core.Robots.RobotEntry.Port. Restored the file byte for byte; the first rerun
        ' still failed because the restored file kept its older timestamp and MSBuild did not recompile
        ' Core. After setting the file's timestamp to the present and rebuilding, it passed (1 of 1).
        ''' <summary>Lists every public type and member and fails for each with no summary in the XML documentation file.</summary>
        <TestMethod>
        Public Sub EveryPublicMemberHasASummary()
            Dim failures As New List(Of String)
            For Each name As String In AssemblyNames
                Dim dll As String = Path.Combine(AppContext.BaseDirectory, name & ".dll")
                Dim xml As String = Path.Combine(AppContext.BaseDirectory, name & ".xml")
                If Not File.Exists(dll) Then
                    failures.Add(name & ".dll is not in the test output")
                    Continue For
                End If
                If Not File.Exists(xml) Then
                    failures.Add(name & ".xml is not in the test output")
                    Continue For
                End If
                Dim documented As HashSet(Of String) = DocumentedMembers(xml)
                Dim loaded As Assembly = Assembly.LoadFrom(dll)
                For Each exported As Type In loaded.GetExportedTypes()
                    For Each id As String In MemberIds(exported)
                        If Not documented.Contains(id) Then failures.Add(name & ": " & id)
                    Next
                Next
            Next
            Assert.AreEqual(0, failures.Count, "public members with no <summary>:" & Environment.NewLine & String.Join(Environment.NewLine, failures))
        End Sub

        Private Shared Function DocumentedMembers(xml As String) As HashSet(Of String)
            Dim documented As New HashSet(Of String)(StringComparer.Ordinal)
            For Each member As XElement In XDocument.Load(xml).Descendants("member")
                Dim summary As XElement = member.Element("summary")
                If summary IsNot Nothing AndAlso summary.Value.Trim().Length > 0 Then documented.Add(member.Attribute("name").Value)
            Next
            Return documented
        End Function

        Private Shared Iterator Function MemberIds(exported As Type) As IEnumerable(Of String)
            Const Flags As BindingFlags = BindingFlags.Public Or BindingFlags.Instance Or BindingFlags.Static Or BindingFlags.DeclaredOnly
            Dim owner As String = TypeId(exported)
            Yield "T:" & owner
            If exported.IsEnum Then Return
            For Each constructor As ConstructorInfo In exported.GetConstructors(Flags)
                Yield "M:" & owner & ".#ctor" & ParameterList(constructor.GetParameters())
            Next
            For Each method As MethodInfo In exported.GetMethods(Flags)
                If method.IsSpecialName Then Continue For
                Yield "M:" & owner & "." & method.Name & ParameterList(method.GetParameters())
            Next
            For Each item As PropertyInfo In exported.GetProperties(Flags)
                Yield "P:" & owner & "." & item.Name & ParameterList(item.GetIndexParameters())
            Next
            For Each happening As EventInfo In exported.GetEvents(Flags)
                Yield "E:" & owner & "." & happening.Name
            Next
        End Function

        Private Shared Function TypeId(subject As Type) As String
            Return subject.FullName.Replace("+", ".")
        End Function

        Private Shared Function ParameterList(parameters As ParameterInfo()) As String
            If parameters.Length = 0 Then Return ""
            Return "(" & String.Join(",", parameters.Select(Function(parameter As ParameterInfo) ParameterId(parameter.ParameterType))) & ")"
        End Function

        Private Shared Function ParameterId(subject As Type) As String
            If subject.IsByRef Then Return ParameterId(subject.GetElementType()) & "@"
            If subject.IsArray Then Return ParameterId(subject.GetElementType()) & "[]"
            If subject.IsGenericParameter Then
                Return If(subject.DeclaringMethod IsNot Nothing, "``", "`") & subject.GenericParameterPosition.ToString()
            End If
            If subject.IsGenericType Then
                Dim definition As String = subject.GetGenericTypeDefinition().FullName
                definition = definition.Substring(0, definition.IndexOf("`"c))
                Return definition.Replace("+", ".") & "{" & String.Join(",", subject.GetGenericArguments().Select(Function(argument As Type) ParameterId(argument))) & "}"
            End If
            Return subject.FullName.Replace("+", ".")
        End Function

    End Class

End Namespace
