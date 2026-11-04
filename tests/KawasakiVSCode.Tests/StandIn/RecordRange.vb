' File:        RecordRange.vb
' Project:     KawasakiVSCode.Tests
' Description: A run of capture records, first to last, as a slice or a typed region names them (D4).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace StandIn

    ''' <summary>A run of capture records, first to last inclusive, numbered as the capture notes number them (D4).</summary>
    Public NotInheritable Class RecordRange

        ''' <summary>Makes a range.</summary>
        ''' <param name="first">The first record.</param>
        ''' <param name="last">The last record, not before the first.</param>
        Public Sub New(first As Integer, last As Integer)
            If first < 1 OrElse last < first Then Throw New ArgumentOutOfRangeException(NameOf(last), "a range runs from record 1 or later to a record not before its first")
            Me.First = first
            Me.Last = last
        End Sub

        ''' <summary>The first record.</summary>
        Public ReadOnly Property First As Integer

        ''' <summary>The last record.</summary>
        Public ReadOnly Property Last As Integer

        ''' <summary>Whether a record lies in the range.</summary>
        ''' <param name="number">The record's number.</param>
        ''' <returns>True when it does.</returns>
        Public Function Contains(number As Integer) As Boolean
            Return number >= First AndAlso number <= Last
        End Function

    End Class

End Namespace
