' File:        TelemetryOptOutTests.vb
' Project:     KawasakiVSCode.Tests
' Description: The test host runs with telemetry opted out, through KawasakiVSCode.runsettings
'              (research R5).
' Author:      RCH Automation LLC
' Created:     2026-11-04

Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Conformance

    ''' <summary>The test host runs with telemetry opted out (research R5).</summary>
    <TestClass>
    Public Class TelemetryOptOutTests

        ' Fire demonstration (Article VII), 2026-11-04, run with the variable set nowhere but the settings
        ' file: it passed, so the value came from KawasakiVSCode.runsettings. Changed the value there to
        ' true (still an opt-out, so no run went without one); this test failed (1 failed of 1). Restored the
        ' file byte for byte; it passed (1 of 1).
        ''' <summary>TESTINGPLATFORM_TELEMETRY_OPTOUT is 1 in the test host, as KawasakiVSCode.runsettings sets it.</summary>
        <TestMethod>
        Public Sub TheTestHostRunsWithTelemetryOptedOut()
            Assert.AreEqual("1", Environment.GetEnvironmentVariable("TESTINGPLATFORM_TELEMETRY_OPTOUT"),
                            "KawasakiVSCode.runsettings did not reach the test host")
        End Sub

    End Class

End Namespace
