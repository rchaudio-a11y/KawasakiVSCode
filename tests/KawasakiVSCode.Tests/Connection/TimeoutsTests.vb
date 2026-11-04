' File:        TimeoutsTests.vb
' Project:     KawasakiVSCode.Tests
' Description: Timeouts.Defaults gives 10 s for every WaitStep, each step is named as data-model.md
'              names it, and a test can build short timeouts.
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports KawasakiVSCode.Core.Connection
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Connection

    ''' <summary>The timeout for each wait step, and how each step is named in messages (research R9).</summary>
    <TestClass>
    Public Class TimeoutsTests

        ''' <summary>Every one of the eight wait steps defaults to 10 seconds.</summary>
        <TestMethod>
        Public Sub EveryStepDefaultsToTenSeconds()
            Dim steps As WaitStep() = [Enum].GetValues(Of WaitStep)()
            Assert.AreEqual(8, steps.Length)
            For Each item As WaitStep In steps
                Assert.AreEqual(TimeSpan.FromSeconds(10), Timeouts.Defaults.ValueFor(item), item.ToString())
            Next
        End Sub

        ''' <summary>Each step is named in messages as data-model.md's table names it.</summary>
        <TestMethod>
        Public Sub EachStepIsNamedAsTheDataModelNamesIt()
            Assert.AreEqual("connecting to 127.0.0.1:9105", Timeouts.Describe(WaitStep.Connect, "127.0.0.1:9105"))
            Assert.AreEqual("telnet negotiation", Timeouts.Describe(WaitStep.Negotiation, Nothing))
            Assert.AreEqual("the login prompt", Timeouts.Describe(WaitStep.LoginPrompt, Nothing))
            Assert.AreEqual("the prompt after login", Timeouts.Describe(WaitStep.LoginReply, Nothing))
            Assert.AreEqual("the reply to load capb.as", Timeouts.Describe(WaitStep.CommandReply, "load capb.as"))
            Assert.AreEqual("transfer data", Timeouts.Describe(WaitStep.TransferData, Nothing))
            Assert.AreEqual("the end of the transfer", Timeouts.Describe(WaitStep.TransferEnd, Nothing))
            Assert.AreEqual("a typed SAVE", Timeouts.Describe(WaitStep.TypedTransfer, "SAVE"))
        End Sub

        ''' <summary>A test can build timeouts with short values, and change one step on its own.</summary>
        <TestMethod>
        Public Sub ATestCanBuildShortTimeouts()
            Dim shortOnes As New Timeouts(TimeSpan.FromMilliseconds(200))
            Assert.AreEqual(TimeSpan.FromMilliseconds(200), shortOnes.ValueFor(WaitStep.TransferData))
            Dim mixed As Timeouts = shortOnes.WithStep(WaitStep.TransferEnd, TimeSpan.FromSeconds(1))
            Assert.AreEqual(TimeSpan.FromSeconds(1), mixed.ValueFor(WaitStep.TransferEnd))
            Assert.AreEqual(TimeSpan.FromMilliseconds(200), mixed.ValueFor(WaitStep.TransferData))
            Assert.AreEqual(TimeSpan.FromMilliseconds(200), shortOnes.ValueFor(WaitStep.TransferEnd), "WithStep must not change the original")
        End Sub

    End Class

End Namespace
