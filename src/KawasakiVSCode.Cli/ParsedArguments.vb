' File:        ParsedArguments.vb
' Project:     KawasakiVSCode.Cli
' Description: A command line ArgumentParser accepted: the verb, the file, --robot and the SAVE
'              qualifier (contracts/cli.md, Synopsis).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports KawasakiVSCode.Core.Commands

''' <summary>A command line ArgumentParser accepted (contracts/cli.md, Synopsis).</summary>
Public NotInheritable Class ParsedArguments

    ''' <summary>Makes the parsed command line.</summary>
    ''' <param name="verb">term, load or save.</param>
    ''' <param name="filePath">The file as the person gave it, or Nothing for term.</param>
    ''' <param name="robot">The --robot value, or Nothing when it was left out.</param>
    ''' <param name="qualifier">The SAVE qualifier for save, or Nothing for term and load.</param>
    Public Sub New(verb As String, filePath As String, robot As String, qualifier As SaveQualifier)
        Me.Verb = verb
        Me.FilePath = filePath
        Me.Robot = robot
        Me.Qualifier = qualifier
    End Sub

    ''' <summary>term, load or save.</summary>
    Public ReadOnly Property Verb As String

    ''' <summary>The file as the person gave it, or Nothing for term.</summary>
    Public ReadOnly Property FilePath As String

    ''' <summary>The --robot value, or Nothing when it was left out.</summary>
    Public ReadOnly Property Robot As String

    ''' <summary>The SAVE qualifier for save (the bare SAVE when --qualifier was left out), or Nothing for term and load.</summary>
    Public ReadOnly Property Qualifier As SaveQualifier

End Class
