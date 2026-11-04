' File:        LoadLineVerdict.vb
' Project:     KawasakiVSCode.Core
' Description: What LoadLineRule says a line during a LOAD is: normal, an error line, a question, or a
'              refusal ending at the prompt (D2, D3, H1, FR-018, FR-021).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Transfer

    ''' <summary>What a line the controller prints during a LOAD is, as LoadLineRule decides it.</summary>
    Public Enum LoadLineVerdict

        ''' <summary>A line a good LOAD prints, in the captures; khi adds nothing for it.</summary>
        Normal

        ''' <summary>Any other line: khi adds a file-reference line for it (D2, FR-018), and hands it to the person if the controller then goes quiet (D3).</summary>
        ErrorLine

        ''' <summary>A line of the captured form (digit:label, digit:label): handed to the person at once (FR-021). It also gets a file-reference line.</summary>
        Question

        ''' <summary>An error line or question followed by the > prompt: the load fails with the controller's words, and nothing goes to the person (H1, capture (j)).</summary>
        Refusal

    End Enum

End Namespace
