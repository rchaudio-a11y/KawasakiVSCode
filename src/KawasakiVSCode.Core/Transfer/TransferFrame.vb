' File:        TransferFrame.vb
' Project:     KawasakiVSCode.Core
' Description: One transfer block, as the controller sends it or as khi answers it, with its letter, its
'              body and its bytes on the wire (captures (b), (j), (l); research R2).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Namespace Transfer

    ''' <summary>
    ''' One transfer block (capture (b), observation 4). The controller's blocks are 05 02, a letter, the body
    ''' and 17; khi's are 02, a letter, the body and 17. Only TransferFraming makes one (research R15).
    ''' </summary>
    Public NotInheritable Class TransferFrame

        Friend Sub New(letter As Char, body As Byte(), wire As Byte(), fileBytes As Integer)
            Me.Letter = letter
            Me.Body = body
            Me.Wire = wire
            Me.FileBytes = fileBytes
        End Sub

        ''' <summary>The block's letter, such as A, C or E.</summary>
        Public ReadOnly Property Letter As Char

        ''' <summary>The bytes between the letter and the closing 17, such as the file name in the controller's A block.</summary>
        Public ReadOnly Property Body As Byte()

        ''' <summary>The whole block as it goes on the wire.</summary>
        Public ReadOnly Property Wire As Byte()

        ''' <summary>How many of the body's bytes are file content: those of a data block; 0 for every other block.</summary>
        Public ReadOnly Property FileBytes As Integer

    End Class

End Namespace
