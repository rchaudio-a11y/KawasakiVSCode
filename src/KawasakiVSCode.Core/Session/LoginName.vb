' File:        LoginName.vb
' Project:     KawasakiVSCode.Core
' Description: The login name khi gives at the controller's login prompt, in its one cited home
'              (Article IX, FR-009).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Session

    ''' <summary>
    ''' The login name, in its one cited home (Article IX, FR-009). AS Language Reference Manual 90209-1022
    ''' §2.6.1 (and 90209-1025 §2.6.1): enter "as" at the login message, and the > prompt follows. Capture (a)
    ''' records 3 to 8 show it: a, s, Enter, then the banner and the prompt.
    ''' </summary>
    Public NotInheritable Class LoginName

        Private Sub New()
        End Sub

        ''' <summary>The login name, as.</summary>
        Public Const Value As String = "as"

    End Class

End Namespace
