' File:        ResultLineTests.vb
' Project:     KawasakiVSCode.Tests
' Description: The one result line khi load and khi save end with, including the question part after
'              a load question (contracts/cli.md, FR-021, FR-028).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports KawasakiVSCode.Core.Reporting
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Reporting

    ''' <summary>The result line (contracts/cli.md, FR-028).</summary>
    <TestClass>
    Public Class ResultLineTests

        ''' <summary>A successful load names the robot, the file as given, the bytes and the programs.</summary>
        <TestMethod>
        Public Sub ASuccessfulLoad()
            Dim outcome As New TransferOutcome("load", True, "cell1", "C:\cells\cell1\pallet.as", 4182, 4182, New String() {"a1", "test"}, Nothing, ExitCode.Success)
            Assert.AreEqual("khi load ok: robot cell1, file ""C:\cells\cell1\pallet.as"", 4182 bytes, programs a1, test", ResultLine.Format(outcome))
        End Sub

        ''' <summary>A failed load with no programs reported reads programs none.</summary>
        <TestMethod>
        Public Sub AFailedLoadWithNoPrograms()
            Dim outcome As New TransferOutcome("load", False, "cell1", "pallet.as", 4182, 4182, New String() {}, Nothing, ExitCode.ConnectionFailed)
            Assert.AreEqual("khi load failed: robot cell1, file ""pallet.as"", 4182 bytes, programs none", ResultLine.Format(outcome))
        End Sub

        ''' <summary>After a question answered 0, the line names the answer in the controller's words and says the controller's copy differs.</summary>
        <TestMethod>
        Public Sub AQuestionAnsweredZero()
            Dim question As New QuestionAnswer(1, "0", "Change to comment and continue", True)
            Dim outcome As New TransferOutcome("load", False, "sim", "capn.as", 148, 148, New String() {"capn"}, question, ExitCode.ControllerError)
            Assert.AreEqual("khi load failed: robot sim, file ""capn.as"", 148 bytes, programs capn; completed with 1 error; answered 0 (Change to comment and continue); the controller's copy differs from the file", ResultLine.Format(outcome))
        End Sub

        ''' <summary>After a question answered 1, the line names the answer in the controller's words.</summary>
        <TestMethod>
        Public Sub AQuestionAnsweredOne()
            Dim question As New QuestionAnswer(1, "1", "Delete program and abort", False)
            Dim outcome As New TransferOutcome("load", False, "sim", "capo.as", 148, 148, New String() {"capo"}, question, ExitCode.ControllerError)
            Assert.AreEqual("khi load failed: robot sim, file ""capo.as"", 148 bytes, programs capo; completed with 1 error; answered 1 (Delete program and abort)", ResultLine.Format(outcome))
        End Sub

        ''' <summary>When fewer bytes were sent than the file holds, as after capture (p)'s answer 1, the line reads sent n of size bytes.</summary>
        <TestMethod>
        Public Sub PartOfTheFileSent()
            Dim question As New QuestionAnswer(1, "1", "Delete program and abort", False)
            Dim outcome As New TransferOutcome("load", False, "sim", "capp.as", 1024, 1815, New String() {"capp"}, question, ExitCode.ControllerError)
            Assert.AreEqual("khi load failed: robot sim, file ""capp.as"", sent 1024 of 1815 bytes, programs capp; completed with 1 error; answered 1 (Delete program and abort)", ResultLine.Format(outcome))
        End Sub
        ''' <summary>A count above one reads errors, and an answer matching no choice has no parenthesis.</summary>
        <TestMethod>
        Public Sub MoreErrorsAndAnUnmatchedAnswer()
            Dim question As New QuestionAnswer(3, "7", Nothing, False)
            Dim outcome As New TransferOutcome("load", False, "sim", "x.as", 10, 10, New String() {}, question, ExitCode.ControllerError)
            Assert.AreEqual("khi load failed: robot sim, file ""x.as"", 10 bytes, programs none; completed with 3 errors; answered 7", ResultLine.Format(outcome))
        End Sub

        ''' <summary>A successful save reads the same way.</summary>
        <TestMethod>
        Public Sub ASuccessfulSave()
            Dim outcome As New TransferOutcome("save", True, "sim", "backup\full.as", 88213, 88213, New String() {"main", "pick", "place"}, Nothing, ExitCode.Success)
            Assert.AreEqual("khi save ok: robot sim, file ""backup\full.as"", 88213 bytes, programs main, pick, place", ResultLine.Format(outcome))
        End Sub

    End Class

End Namespace
