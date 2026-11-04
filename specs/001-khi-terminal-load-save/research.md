# Research: khi — terminal, LOAD and SAVE from VS Code

Phase 0 for [plan.md](plan.md). Each entry gives the decision, why, and what else was considered. Facts checked on this PC on 2026-11-03 say so.

## R1 Capture route

**Decision**: A logging relay, `tools/Start-CaptureRelay.ps1`. KRterm connects to the relay on a local port, and the relay forwards to K-ROSET at 127.0.0.1:9105, logging every byte in both directions with a timestamp.

**Rationale**:
- Checked here: no loopback adapter and no Npcap are installed, so capturing on the loopback interface is not possible without installing one.
- Checked here: KRterm's `C:\KawasakiRobot\KRterm\KRterm.ini` stores a `TcpPort` per connection (now 23), so KRterm can be pointed at the relay's port.
- The K-IDE manual (2.5.4) gives K-ROSET's first robot as 127.0.0.1:9105.

**Status**: Proved on 2026-11-03, together as the Architect ruled: the Architect ran K-ROSET and KRterm, and the agent ran the relay from port 9101 to 127.0.0.1:9105 ([quickstart.md P0](quickstart.md#p0-prove-the-capture-route)).
- **Through the relay**: KRterm's login reached `This is AS monitor terminal "AUX2"` and the `>` prompt.
- **The log**: 4 records in each direction, 7 bytes toward the controller and 50 from it.
- **Direct connection**: The Architect confirmed that KRterm connected straight to 9105 shows the same prompt.

The proof log was not checked in. Captures (a) to (l) follow in the same session. The relay has its own test, `tools/Test-CaptureRelay.ps1`.

**Alternatives considered**: Installing Npcap and capturing on loopback adds a driver to the PC and gives the same bytes. A relay written in VB would need a fourth project and an Article I justification; PowerShell is allowed for dev-time scripts (Article XV).

## R2 Protocol details come from the captures

**Decision**: Nothing about the wire is designed ahead of the captures. Each open question is answered by the capture listed, and the implementation copies what the capture shows.

| Question | Answered by capture |
|----------|---------------------|
| Telnet options the controller asks for and the replies KRterm gives | (a) connect and login |
| The login prompt, the `as` exchange, and the prompt after login | (a) |
| LOAD command form, transfer framing, progress text, completion marker | (b) LOAD of a small program |
| How a file larger than one transfer block is split, including the last partial block | (l) LOAD of a generated program of about 60 KB |
| How a rejected step appears, whether a line or step is named, the question text | (c) rejected LOAD |
| SAVE command form with `/P`, `/FULL` and none; framing; completion marker; the file KRterm wrote | (d) `SAVE/P`, (e) `SAVE/FULL`, (g) bare `SAVE` |
| A SAVE the controller refuses | (h) |
| LOAD of a name already in memory (P1033) | (i) |
| A typed SAVE and LOAD inside a session, and a typed LOAD of a missing file | (f), (j) |
| DELETE/P present, absent (P1038), refused, and any confirmation | (k), KRterm required, KIDE optional |
| Whether telnet doubles a 0xFF byte inside file content | (b) and (d), if any file holds one; AS files are expected to be ASCII |

**Rationale**: Article IV. A behaviour no capture shows is recorded as not implemented and reported to the Architect.

**Alternatives considered**: Writing the framing from memory or from other tools' source, which Article IV forbids.

## R3 Language, framework and solution

**Decision**: VB.NET on `net10.0`, with a `.slnx` solution. Each `.vbproj` is written by hand with `OptionStrict On`, `OptionExplicit On`, `OptionInfer Off`, `GenerateDocumentationFile true` and `LangVersion latest`.

**Rationale**:
- Checked here: SDK 10.0.401 ships VB templates for `classlib`, `console` and `mstest`.
- Checked here: `dotnet new sln` now writes `.slnx`.
- The VB `mstest` template sets `Nullable` and `ImplicitUsings`, which do nothing in VB, so the project files are written rather than generated.

**Alternatives considered**: A shared `Directory.Build.props` for the option settings. It is rejected because the gate checks "every touched project file".

## R4 XML documentation enforcement

**Decision**: A conformance test, `XmlDocCoverageTests`. It loads the Core and Cli assemblies, lists every public type, method, property, event and interface member, and fails for each one with no `<summary>` in the generated XML documentation file. Fire: delete one summary and watch the test go red.

**Rationale**: Checked here with a probe class library, `GenerateDocumentationFile` on and `Option Strict On`. A public method with no comment built with 0 warnings, and the XML file simply left it out. So in VB the compiler does not enforce the rule, whatever the hard rule says.

Constitution 2.0.1 makes this test the rule.

**Alternatives considered**: Turning warnings into errors does nothing when no warning exists. An analyzer package would be a third-party package for one check.

## R5 Test framework

**Decision**: MSTest, through `MSTest.TestFramework`, `MSTest.TestAdapter` and `Microsoft.NET.Test.Sdk` referenced directly. The first restore confirms no telemetry or coverage extension comes in with them; if one does, `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` is set for test runs.

**Rationale**: Checked here: restoring the template's `MSTest` 4.0.2 meta-package pulled in `Microsoft.Testing.Extensions.Telemetry` and `Microsoft.Testing.Extensions.CodeCoverage`. The three packages are Microsoft's and work with VB.

**Alternatives considered**: xUnit and NUnit both have VB templates, but neither is lighter, and MSTest is Microsoft's.

## R6 Command-line parsing

**Decision**: A hand-written `ArgumentParser` for three verbs and four options. Anything else is refused by name: an unknown verb, an unknown option, or an option on a verb that doesn't take it.

**Rationale**: The surface is small, and Article XV discourages packages.

**Alternatives considered**: `System.CommandLine` is a package and more machinery than three verbs need.

## R7 Reading `khi.robots.json`

**Decision**: `System.Text.Json`, which is part of .NET. The shape is in [contracts/robots-file.md](contracts/robots-file.md). The file is read from the current folder only.

**Rationale**: It is in the base library, and the spec's decision is the current folder.

## R8 Console input and output in `khi term`

**Decision**:
- **Interactive keys**: With the console interactive, set `Console.TreatControlCAsInput = True` and read keys with `Console.ReadKey(intercept:=True)`. Each key goes to the controller as its byte; Ctrl+C is 0x03. Ctrl+] (0x1D) ends the session and is never sent.
- **Redirected input**: When input is redirected, as in tests, read raw bytes from standard input. End of input ends the session, and a 0x1D byte is treated like Ctrl+].
- **Console closing**: Register `PosixSignal.SIGHUP`, which .NET raises on Windows when the console closes, to close the socket and exit zero.
- **Output**: The controller's bytes go to the raw standard-output stream, not through a text encoder.

**Rationale**: Article VIII wants output unaltered, and FR-014 wants Ctrl+C relayed and Ctrl+] local.

**To verify in acceptance**: VS Code's integrated terminal passes Ctrl+] and the other keys of manual 2.4.1 through to khi; the editor binds Ctrl+] only while a text editor has focus. The console-close signal reaches khi before VS Code ends the process. If keys are held back, check `terminal.integrated.sendKeybindingsToShell` and report what was needed, as the Architect ruled.

**Alternatives considered**: Reading lines gives up the controller's own line editing, its Ctrl+L and Ctrl+N recall, and character echo.

## R9 Timeouts

**Decision**:
- **Per wait**: Every timeout applies to one wait, not to a whole transfer, so a transfer that keeps moving never times out.
- **Values**: Each `WaitStep`'s value comes from the captures. It is three times the longest gap seen for that step, but never below 10 seconds, and it is recorded in `Timeouts` with the capture it came from.
- **Typed transfers**: Inside a typed transfer, every keystroke the person sends starts a new wait, so a controller question can be answered without running out the timer, and every wait stays bounded (Article X).
- **Tests**: `Timeouts` is a parameter of `KhiApp.Run`; `Main` passes the defaults and tests pass short values. There is no person-facing timeout option.

**Rationale**: Article X, plus the spec's edge case on typed transfers. Checked here: KRterm's own setting is `TimeoutSec=600`, which is far looser than khi needs.

**Alternatives considered**: A fixed 30 seconds for everything risks timing out a slow `SAVE/FULL`. A `--timeout` option would be a person-facing setting the spec rules out.

## R10 Saving through a temporary file

**Decision**: Write to `<target folder>\.<name>.<8 random hex>.khi-tmp`. On completion, use `File.Move(temp, target, overwrite:=True)`, a single rename within one folder. On any failure, delete the temporary file. A rename that fails, for example because the target is held open, is a failure with the target untouched.

**Rationale**: Article VI. A rename in the same folder replaces the target in one step.

**Alternatives considered**: `File.Replace` with a backup file, which is the `.bas`-style copy the spec rules out.

## R11 Problem matcher

**Decision**: Two patterns in each load task, anchored on a path that ends in an extension, with `fileLocation: ["autoDetect", "${workspaceFolder}"]`. The line form uses `kind: location` and the file-only form uses `kind: file`. The regular expressions are in [contracts/tasks-template.md](contracts/tasks-template.md).

**Rationale**: A path anchor keeps the matcher off khi's own result line and off ordinary controller text. Windows file names cannot contain `:`, so the drive letter is the only colon before the separator.

## R12 Production-route tests

**Decision**: Route tests call `KhiApp.Run(args, input, output, error, timeouts)`, the same code `Main` runs. A small set also runs the built `khi.exe` as a process and checks exit codes for refusals and a full load and save against the stand-in.

**Rationale**: Article XIII. In-process runs keep the timeout tests fast, and the process runs prove `Main` really wires to `KhiApp.Run`.

## R13 Line endings in the repository

**Decision**: The first task adds `.gitattributes` with `tests/KawasakiVSCode.Tests/Captures/** -text` and `tests/KawasakiVSCode.Tests/Fixtures/** -text`, before any capture or fixture is added.

**Rationale**: Checked here: Git for Windows sets `core.autocrlf=true` system-wide, which would rewrite line endings in captured files and in INV-1's mixed-line-ending fixture. `-text` stops the conversion and keeps diffs readable.

## R14 Reading `.PROGRAM` header lines

**Decision**: `ProgramHeaderReader` reads a name from a line that begins with `.PROGRAM` and a space. The name runs up to the first `(`, space or `;`, as in the manual's listings: `.PROGRAM aa()`, `.PROGRAM palletize`, `.PROGRAM emergency()   ;comment`, `.PROGRAM relative.test`. The exact form is confirmed against the files KRterm wrote in captures (d) and (e) before the reader is built. It looks at the start of each line and at nothing else in it.

**Rationale**: Article II's exception covers "those lines and nothing else", and INV-15 guards it.

## R15 One door per rule

**Decision**:

| Rule | The one door |
|------|--------------|
| Which robot (Article IX) | `RobotSelection.Resolve` |
| Supported qualifiers | `SaveQualifier.Parse` |
| What khi may send as a command (Article III) | `ControllerCommand`; `ControllerConnection.Send` takes only a `ControllerCommand`, framing bytes from `TransferFraming`, or pass-through content |
| Which names `--replace` deletes (Article II) | `ProgramHeaderReader` |
| Every wait has a timeout (Article X) | `ControllerConnection.ReadUntil(step, ...)`; only `TerminalRelay` reads without a limit, and only after login |
| Replacing a saved file (Article VI) | `SaveTarget` |

**Rationale**: Article XII. Once code exists, claims such as "only `TerminalRelay` calls the unbounded read" are proved in review with CodeMem `references`, not text search.

## R16 Exit codes

**Decision**:

| Code | Meaning |
|------|---------|
| 0 | Success, or a terminal session the person ended |
| 1 | The controller reported an error or refusal, or asked a question khi may not answer |
| 2 | khi refused before connecting: usage, unknown robot, unsupported qualifier, unwired option, missing file or folder |
| 3 | A wait timed out |
| 4 | The connection failed, was refused, or was closed by the controller |
| 5 | A local file step failed, for example the target could not be replaced |

**Rationale**: The spec asks only for zero and non-zero. Distinct codes let tests tell the failure paths apart, and let a task's caller do the same.
