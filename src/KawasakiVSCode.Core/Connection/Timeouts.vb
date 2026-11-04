' File:        Timeouts.vb
' Project:     KawasakiVSCode.Core
' Description: One timeout per wait step (Article X, research R9), and how each step is named in a
'              timeout message (data-model.md).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Connection

    ''' <summary>
    ''' One timeout per wait step (Article X, research R9). Every wait on the controller has one; the only
    ''' waits without a limit are the terminal relay after login and the wait for the person's answer to a
    ''' load question, and neither is a step here.
    ''' </summary>
    Public NotInheritable Class Timeouts

        Private ReadOnly _values As Dictionary(Of WaitStep, TimeSpan)

        ''' <summary>The same timeout for every wait step, as tests use with short values.</summary>
        ''' <param name="value">The timeout for every step.</param>
        Public Sub New(value As TimeSpan)
            _values = New Dictionary(Of WaitStep, TimeSpan)()
            For Each item As WaitStep In [Enum].GetValues(Of WaitStep)()
                _values(item) = value
            Next
        End Sub

        Private Sub New(values As Dictionary(Of WaitStep, TimeSpan))
            _values = values
        End Sub

        ''' <summary>
        ''' The defaults: 10 seconds for every step (research R9). The longest controller reply in any capture
        ''' is 81 ms (capture (i) record 10, capture (l) record 2), and the longest gap between the controller's
        ''' records is 116 ms, during a SAVE (capture (g) record 16); three times either is under the 10-second
        ''' floor.
        ''' </summary>
        Public Shared ReadOnly Property Defaults As Timeouts = New Timeouts(TimeSpan.FromSeconds(10))

        ''' <summary>The timeout for one step.</summary>
        ''' <param name="item">The step.</param>
        ''' <returns>How long khi waits in that step.</returns>
        Public Function ValueFor(item As WaitStep) As TimeSpan
            Return _values(item)
        End Function

        ''' <summary>A copy with one step's timeout changed; this instance is left as it is.</summary>
        ''' <param name="item">The step.</param>
        ''' <param name="value">Its new timeout.</param>
        ''' <returns>The copy.</returns>
        Public Function WithStep(item As WaitStep, value As TimeSpan) As Timeouts
            Dim copy As New Dictionary(Of WaitStep, TimeSpan)(_values)
            copy(item) = value
            Return New Timeouts(copy)
        End Function

        ''' <summary>How a step is named in a timeout message, as data-model.md's table names it.</summary>
        ''' <param name="item">The step.</param>
        ''' <param name="detail">The address and port for Connect, the command for CommandReply, SAVE or LOAD for TypedTransfer; unused otherwise.</param>
        ''' <returns>The step's name, such as "the prompt after login".</returns>
        Public Shared Function Describe(item As WaitStep, detail As String) As String
            Select Case item
                Case WaitStep.Connect
                    Return "connecting to " & detail
                Case WaitStep.Negotiation
                    Return "telnet negotiation"
                Case WaitStep.LoginPrompt
                    Return "the login prompt"
                Case WaitStep.LoginReply
                    Return "the prompt after login"
                Case WaitStep.CommandReply
                    Return "the reply to " & detail
                Case WaitStep.TransferData
                    Return "transfer data"
                Case WaitStep.TransferEnd
                    Return "the end of the transfer"
                Case WaitStep.TypedTransfer
                    Return "a typed " & detail
                Case Else
                    Throw New ArgumentOutOfRangeException(NameOf(item))
            End Select
        End Function

    End Class

End Namespace
