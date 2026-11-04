' File:        TransferFraming.vb
' Project:     KawasakiVSCode.Core
' Description: The one door for transfer framing: it reads the controller's blocks out of its bytes, with
'              the text between them, and builds the blocks khi answers with (captures (b), (l), (q), (s);
'              research R2, R15).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports System.Globalization
Imports KawasakiVSCode.Core.Reporting

Namespace Transfer

    ''' <summary>
    ''' The one door for the framing bytes khi reads and builds (research R15). Reading: the controller's
    ''' bytes, as they arrive, are split into its blocks, 05 02, a letter, the body and 17, and the text
    ''' between them (capture (b), observation 4). Building: KRterm's answers, 02, the letter, four spaces, 0
    ''' and 17; the data block, 02 43 20 20 20 20 30, up to 512 file bytes and 17; and the end-of-file block,
    ''' 02 43 20 20 20 20 30 1a 17 (captures (b), (l), (q), (s)). The framing is not in 90209-1022; the bytes
    ''' are the captured ones.
    ''' </summary>
    Public NotInheritable Class TransferFraming

        ''' <summary>The most file bytes one data block carries (capture (l), observation 3).</summary>
        Public Const BlockSize As Integer = 512

        Private Const Enq As Byte = &H5
        Private Const Stx As Byte = &H2
        Private Const Etb As Byte = &H17
        Private Const Substitute As Byte = &H1A
        Private Const Iac As Byte = &HFF

        ''' <summary>The field after the letter in every answer khi sends: four spaces and 0.</summary>
        Private Shared ReadOnly Field As Byte() = {&H20, &H20, &H20, &H20, &H30}

        Private ReadOnly _pending As New List(Of Byte)()

        ''' <summary>Makes a reader for one transfer's controller bytes.</summary>
        Public Sub New()
        End Sub

        ''' <summary>Adds the controller's bytes, in the order they arrived.</summary>
        ''' <param name="bytes">The bytes.</param>
        Public Sub Add(bytes As Byte())
            _pending.AddRange(bytes)
        End Sub

        ''' <summary>
        ''' Takes the controller's text that comes before its next block, as far as it has arrived. A 05 at the
        ''' end, which may start a block, is kept back until the next bytes show whether it does.
        ''' </summary>
        ''' <returns>The text, possibly empty.</returns>
        Public Function TakeText() As Byte()
            Dim count As Integer = 0
            Do While count < _pending.Count
                If _pending(count) = Enq Then
                    If count + 1 >= _pending.Count OrElse _pending(count + 1) = Stx Then Exit Do
                End If
                count += 1
            Loop
            Dim text As Byte() = _pending.GetRange(0, count).ToArray()
            _pending.RemoveRange(0, count)
            Return text
        End Function

        ''' <summary>Takes the controller's block at the head of what has arrived, once its closing 17 has come.</summary>
        ''' <returns>The block, or Nothing when no whole block is at the head.</returns>
        Public Function TakeFrame() As TransferFrame
            If _pending.Count < 4 OrElse _pending(0) <> Enq OrElse _pending(1) <> Stx Then Return Nothing
            Dim closing As Integer = _pending.IndexOf(Etb, 3)
            If closing < 0 Then Return Nothing
            Dim wire As Byte() = _pending.GetRange(0, closing + 1).ToArray()
            _pending.RemoveRange(0, closing + 1)
            Return New TransferFrame(ChrW(wire(2)), wire.Skip(3).Take(wire.Length - 4).ToArray(), wire, 0)
        End Function

        ''' <summary>KRterm's answer to a controller block: 02, the letter, four spaces, 0 and 17, as A    0 and E    0 in capture (b).</summary>
        ''' <param name="letter">The letter of the block answered.</param>
        ''' <returns>The answer.</returns>
        Public Shared Function Answer(letter As Char) As TransferFrame
            Return Build(letter, Field, 0)
        End Function

        ''' <summary>A data block: 02 43 20 20 20 20 30, up to 512 file bytes, and 17 (captures (b), (l), (q), (s)).</summary>
        ''' <param name="content">The file's bytes.</param>
        ''' <param name="offset">Where the block's bytes start in the file.</param>
        ''' <returns>The block, carrying up to BlockSize bytes from the offset, none when the offset is at the end.</returns>
        Public Shared Function DataBlock(content As Byte(), offset As Integer) As TransferFrame
            Dim count As Integer = Math.Min(BlockSize, content.Length - offset)
            Dim body As New List(Of Byte)(Field)
            body.AddRange(content.Skip(offset).Take(count))
            Return Build("C"c, body.ToArray(), count)
        End Function

        ''' <summary>The end-of-file block, 02 43 20 20 20 20 30 1a 17, sent at the request after the file's last data block (captures (b), (l), (q), (s)).</summary>
        Public Shared ReadOnly Property EndOfFile As TransferFrame
            Get
                Dim body As New List(Of Byte)(Field)
                body.Add(Substitute)
                Return Build("C"c, body.ToArray(), 0)
            End Get
        End Property

        ''' <summary>
        ''' Refuses a file holding a byte a block cannot carry, before connecting: 17 ends a transfer block, and no
        ''' capture shows how telnet would carry ff (FR-042). This is a check on the transport, not validation of
        ''' AS source (Article II).
        ''' </summary>
        ''' <param name="filePath">The file as the person gave it, named in the refusal.</param>
        ''' <param name="content">The file's bytes.</param>
        ''' <exception cref="KhiRefusal">The file holds ff or 17; exit 2, naming the first one and its offset from 0.</exception>
        Public Shared Sub CheckSendable(filePath As String, content As Byte())
            For offset As Integer = 0 To content.Length - 1
                If content(offset) = Iac OrElse content(offset) = Etb Then
                    Throw New KhiRefusal(ExitCode.RefusedBeforeConnecting,
                                         "khi: " & filePath & " holds byte " & content(offset).ToString("x2", CultureInfo.InvariantCulture) &
                                         " at offset " & offset.ToString(CultureInfo.InvariantCulture) & "; it cannot be sent")
                End If
            Next
        End Sub

        Private Shared Function Build(letter As Char, body As Byte(), fileBytes As Integer) As TransferFrame
            Dim wire As New List(Of Byte)(body.Length + 3)
            wire.Add(Stx)
            wire.Add(CByte(AscW(letter)))
            wire.AddRange(body)
            wire.Add(Etb)
            Return New TransferFrame(letter, body, wire.ToArray(), fileBytes)
        End Function

    End Class

End Namespace
