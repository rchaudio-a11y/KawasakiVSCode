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

The proof log was not checked in. Captures (a) to (q) follow in the same session. The relay has its own test, `tools/Test-CaptureRelay.ps1`.

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
| A SAVE the controller refuses or reports an error in | (h) |
| LOAD of a name already in memory | (i), and the extra `i-load-existing-changed` |
| A typed SAVE and LOAD inside a session, and a typed LOAD of a missing file | (f), (j) |
| DELETE/P present, absent, refused, and any confirmation | (k), KRterm; kept, though no 001 behaviour relies on it since `--replace` was dropped |
| A SAVE onto a file name that already exists: whether the old file's rename to `.bas` travels in the protocol | (m) |
| What the controller sends after the LOAD question is answered with `0`, and with `1` | (n), (o). `c-load-rejected-ctrl-c` is kept as a record only: khi never relays Ctrl+C |
| What KRterm sends at a question while file data is still to be sent | (p): the question comes with the controller's request for the next block, and KRterm answers it with the file's next data block, as it answers any request; in (c), (n) and (o), with no data left, that answer was the end-of-file block. After `1`, the controller ended with `E` and asked for no more of the file |
| What KRterm sends for an empty file | (q): a data block holding no bytes at the first request, then the end-of-file block at the next |
| What the controller asks for after `0` with file data left | (r) |
| What KRterm sends after a file's last full block | (s), a file of exactly 1,024 bytes: a data block holding no bytes, then the end-of-file block at the next request. A block shorter than 512 bytes, an empty one included, ends a file, as (b), (l), (q) and (r) also show |
| Whether telnet doubles a 0xFF byte inside file content | (b) and (d), if any file holds one; AS files are expected to be ASCII |

**The answers** (T046, 2026-11-04), each from its capture's confirmed note, observations as numbered there:

- **(a)**: KRterm speaks first, offering telnet WILL TERMINAL-TYPE, `ff fb 18`. The controller answers DO TERMINAL-TYPE, `ff fd 18`, with `login: ` in the same record, and asks for nothing more. It echoes each typed character; Enter is CR LF; after `as` come LF, `This is AS monitor terminal "AUX2"`, CR LF and `>` (observations 1 to 5).
- **(b) and (l)**: on Enter on a LOAD line, KRterm sends `18`, the line again and CR LF; the controller redraws the line and sends the `A` block naming the file. Controller blocks are `05 02`, a letter and `17`; KRterm answers each with `02`, the letter, `    0` and `17`. The file goes in `C` blocks of up to 512 file bytes, one per controller request, never more than one in flight, wherever the 512th byte falls. A block shorter than 512 bytes ends the file, and the next request gets the end-of-file block, `02 43 20 20 20 20 30 1a 17` (with (q) and (s), FR-016). The `E` block ends the transfer, and `File load completed. (<n> errors)` the LOAD ((b) 3 to 5; (l) 3 to 7).
- **(c)**: the rejected step is named by step, not by file line: `3 x = 1 +`, `^(P0118)Illegal variable type.`, `STEP syntax error.` and the question, carried with the controller's next `C` request (record 18). Closing the session at the question left K-ROSET serving no new connection until it was restarted (`c-load-rejected-reconnect`) (observations 5, 6 and 8).
- **(d), (e) and (g)**: `SAVE/P`, `SAVE/FULL` and a bare `SAVE` share one protocol: `18`, the line and CR LF; the `B` block naming the file, answered `B    0`; `Saving...(<name>)`; one `D` block per file line, of any length and unanswered; the `E` block, answered `E    0`; and `File save completed.`. The display lines between the blocks are not file content (R17) ((d) 2 to 5; (e) 2 to 4; (g) 2 to 5).
- **(f), (j) and (k)**: a typed SAVE and LOAD run the protocols of (d) and (b), KRterm adding a space before `=` in a SAVE line; a LOAD of a program in use draws `Cannot execute because program already in use.` and still ends `File load completed.` ((f) 2 to 6). For a missing file KRterm answers the `A` block with `02 41 2d 31 30 35 32 17`, `A-1052`, and the controller prints `(P2016)Cannot open the file.` and the prompt, with no `C` or `E` block ((j) 3 and 4). A DELETE/P line gets no `18`, and the controller asks `Are you sure ? (Yes:1, No:0) ` before anything else ((k) 2 and 3).
- **(h)**: a SAVE error is a line outside the blocks, `nosuch:Variable (or program) does not exist.`, and `File save completed.` follows it all the same; SAVE's completion line carries no error count, and KRterm kept the partial file (observations 3 to 6).
- **(i)**: on K-ROSET a plain LOAD of a name already in memory replaces the program, with no refusal and no question: (i) ran as a first LOAD does, and `i-load-existing-changed` showed the new content under `LIST/P` ((i) 1; `i-load-existing-changed` 2 and 3).
- **(m)**: a SAVE onto a file name that exists changes nothing in the protocol; KRterm renamed the old file to `capm.Bas` on the PC without asking (observations 2 to 4).
- **(n) and (o)**: the answer is the digit and Enter, sent as typed and echoed. After either, the controller sends CR LF and the `E` block, then `File load completed. (1 errors)`. `0` keeps the program with the step made a comment; `1` keeps nothing ((n) 3 to 6; (o) 3 to 5).
- **(p), (q), (r) and (s)**: as the table gives them. A question comes with the controller's request for the next block, which KRterm answers like any other; after `1` the controller ends with `E`, and after `0` it asks for the rest of the file ((p) 4 to 7; (q) 3 to 5; (r) 2 to 5; (s) 4 to 7).
- **The `ff` byte**: no capture's file held one, so the question stays open, and FR-042 refuses such a file before connecting.

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

**The first restore, 2026-11-04 (T007)**: the three packages resolved as `MSTest.TestFramework` 4.5.2, `MSTest.TestAdapter` 4.5.2 and `Microsoft.NET.Test.Sdk` 18.11.0. Referenced directly, they still bring in `Microsoft.Testing.Extensions.Telemetry` 2.5.2 and `Microsoft.ApplicationInsights` 2.23.0 (through `MSTest.TestAdapter`), and `Microsoft.CodeCoverage` 18.11.0 (through `Microsoft.NET.Test.Sdk`). So, as the decision says, test runs set `TESTINGPLATFORM_TELEMETRY_OPTOUT=1`: `KawasakiVSCode.runsettings` at the repository root sets it for the test host, the tests project points at that file through `RunSettingsFilePath`, and `TelemetryOptOutTests` checks that the value reaches the host. `DOTNET_CLI_TELEMETRY_OPTOUT=1` has to be set in the environment that runs `dotnet`, because the CLI reads it before any settings file. Code coverage runs only when asked for with `--collect`, which this project never does. Referencing the packages directly no longer keeps these out, so the plan's reason for avoiding the meta-package is weaker than it was; the Architect may rule on it.

**Alternatives considered**: xUnit and NUnit both have VB templates, but neither is lighter, and MSTest is Microsoft's.

## R6 Command-line parsing

**Decision**: A hand-written `ArgumentParser` for three verbs and two options, `--robot` and `--qualifier`. Anything else is refused by name: an unknown verb, an unknown option, or an option on a verb that doesn't take it. The wording of every refusal is in [contracts/cli.md](contracts/cli.md#synopsis).

**Rationale**: The surface is small, and Article XV discourages packages.

**Alternatives considered**: `System.CommandLine` is a package and more machinery than three verbs need.

## R7 Reading `khi.robots.json`

**Decision**: `System.Text.Json`, which is part of .NET. The shape is in [contracts/robots-file.md](contracts/robots-file.md). The file is read from the current folder only.

**Rationale**: It is in the base library, and the spec's decision is the current folder.

## R8 Console input and output in `khi term`

**Decision**:
- **Interactive keys**: With the console interactive, set `Console.TreatControlCAsInput = True` and read keys with `Console.ReadKey(intercept:=True)`. Each key goes to the controller as its byte, with two exceptions: Ctrl+C (0x03) is discarded, so `TreatControlCAsInput` is what keeps it from ending the process, and Ctrl+] (0x1D) ends the session. Neither is ever sent.
- **Redirected input**: When input is redirected, as in tests, read raw bytes from standard input. End of input ends the session, a 0x1D byte is treated like Ctrl+], and a 0x03 byte is discarded.
- **An answer during `khi load`**: At a controller question, `khi load` reads the person's answer the same way, keys with `Console.TreatControlCAsInput = True` when interactive and raw bytes when redirected, so a Ctrl+C arrives as 0x03 and is discarded (FR-021). It sends what was typed, with Enter as captures (n) and (o) show it, and reads on. End of input before an answer closes the session.
- **No shortcuts of khi's own**: khi does not emulate KRterm's shortcuts, such as Alt+V to paste; copy and paste are VS Code's terminal's (Architect, 2026-11-04).
- **Console closing**: Register `PosixSignal.SIGHUP`, which .NET raises on Windows when the console closes, to close the socket and exit zero.
- **Output**: The controller's bytes go to the raw standard-output stream, not through a text encoder.

- **Ctrl+C outside the console reads**: While `khi load` or `khi save` streams, and before `khi term` reads its first key, a Ctrl+C is cancelled through `Console.CancelKeyPress` with `e.Cancel = True`, so it neither ends the process nor reaches the controller (FR-041).

**Rationale**: Article VIII wants output unaltered, and FR-014 and FR-041 want Ctrl+C discarded and Ctrl+] local (Architect, 2026-11-04, reversing the earlier relay of Ctrl+C).

**To verify in acceptance**: VS Code's integrated terminal passes Ctrl+] and the other keys of manual 2.4.1 through to khi; the editor binds Ctrl+] only while a text editor has focus. The console-close signal reaches khi before VS Code ends the process. If keys are held back, check `terminal.integrated.sendKeybindingsToShell` and report what was needed, as the Architect ruled.

**Alternatives considered**: Reading lines gives up the controller's own line editing, its Ctrl+L and Ctrl+N recall, and character echo.

## R9 Timeouts

**Decision**:
- **Per wait**: Every timeout applies to one wait, not to a whole transfer, so a transfer that keeps moving never times out.
- **Values**: Each `WaitStep`'s value comes from the captures. It is three times the longest gap seen for that step, but never below 10 seconds, and it is recorded in `Timeouts` with the capture it came from.
- **Typed transfers**: Inside a typed transfer, every keystroke the person sends starts a new wait, and every wait is bounded except the wait for the person's answer to a controller question, which has no limit, as during `khi load` (Article X, v3.1.0; FR-015). The waits before the question and after the answer keep theirs.
- **A question during `khi load`**: The wait for the person's answer has no timeout (Article X, v3.0.0). The waits before the question and after the answer keep theirs.
- **Tests**: `Timeouts` is a parameter of `KhiApp.Run`; `Main` passes the defaults and tests pass short values. There is no person-facing timeout option.

**The table** (T017, measured 2026-11-04 over captures (a) to (o) and the three extras, then again with (p), (q) and (r), which change no row; (s) is measured when it is checked in). A gap is the time from the record before to the controller's record. Three times the longest gap, 348 ms, is far under the floor, so every step is 10 seconds.

| WaitStep | Value | Longest gap seen | Where |
|----------|-------|------------------|-------|
| `Connect` | 10 s | Not captured: the relay records bytes, not the connect | — |
| `Negotiation` | 10 s | 81 ms | (l) record 2 |
| `LoginPrompt` | 10 s | 81 ms; `login: ` comes in the same record as `ff fd 18` | (l) record 2 |
| `LoginReply` | 10 s | 36 ms | (a) record 8 |
| `CommandReply` | 10 s | 81 ms for the echo of a line sent whole; 64 ms after `18` and the line | (i) record 10; (b) record 34 |
| `TransferData` | 10 s | 39 ms after a LOAD block; 116 ms between the controller's records in a SAVE | (l) record 196; (g) record 16 |
| `TransferEnd` | 10 s | 34 ms after the `E` answer | (c-load-rejected-ctrl-c) record 46 |
| `TypedTransfer` | 10 s | 81 ms between the controller's records in the typed SAVE | (f) record 16 |

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

**Withdrawn on 2026-11-04** with `--replace`: nothing in this feature reads a file's content. Kept for a later feature. The SAVE captures show headers of the form `.PROGRAM capb() #0`, which the rule below already reads correctly.

**Decision**: `ProgramHeaderReader` reads a name from a line that begins with `.PROGRAM` and a space. The name runs up to the first `(`, space or `;`, as in the manual's listings: `.PROGRAM aa()`, `.PROGRAM palletize`, `.PROGRAM emergency()   ;comment`, `.PROGRAM relative.test`. The exact form is confirmed against the files KRterm wrote in captures (d) and (e) before the reader is built. It looks at the start of each line and at nothing else in it.

**Rationale**: Article II's exception covers "those lines and nothing else", and INV-15 guards it.

## R15 One door per rule

**Decision**:

| Rule | The one door |
|------|--------------|
| Which robot (Article IX) | `RobotSelection.Resolve` |
| Supported qualifiers | `SaveQualifier.Parse` |
| What khi may send as a command (Article III) | `ControllerCommand`; `ControllerConnection.Send` takes only a `ControllerCommand`. Otherwise bytes leave khi only as the one telnet offer `ControllerConnection.Negotiate` sends (capture (a)), framing bytes from `TransferFraming`, or pass-through content |
| Whether a line outside the file data is normal during a SAVE (FR-040) | `SaveLineRule` |
| Whether a line during a LOAD is normal, an error, a question or a refusal ending at the prompt (FR-018, FR-021) | `LoadLineRule` |
| The framing bytes khi reads and builds | `TransferFraming` |
| What of the person's input reaches the controller, Ctrl+C dropped and Ctrl+] ending the session (FR-014, FR-041) | `PersonInput` |
| Every wait has a timeout (Article X) | `ControllerConnection.ReadUntil(step, ...)`; only `TerminalRelay` after login, and the wait for the person's answer to a question during `khi load` or a typed transfer, read without a limit |
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

## R17 Normal lines during a SAVE

**Decision**: During a SAVE, the controller prints text outside the `D` blocks that carry the file. A line there is normal only if it is one of these, as the SAVE captures show them on a good save; `SaveLineRule` is the one door for the list (FR-040):

| Line | Where it comes | Captures |
|------|----------------|----------|
| The echo of the SAVE command and the redraw of the line before the `B` block | before the `B` block | (d), (e), (f), (g), (h) |
| `Saving...(<file>)` | after the `B` block | all |
| `Network Configuration data` | before each `.NETCONF` and `.NETCONF2` line | all |
| `ROBOT DATA`, `OPERATION INFORMATION DATA`, `SYSTEM DATA`, `AUX DATA`, `INTERFACE PANEL DATA`, `INTERFACE PANEL PAGE TITLE`, `INTERFACE PANEL COLOR DATA`, `SIGNAL_NAME`, `STORAGE DATA`, `MAINTENANCE DATA` | before their sections | (e), and (g) up to `SIGNAL_NAME` |
| `CONDITON DATA`, spelled so | before `.CONDITION` | (g) |
| `Program <name>` | after each `.PROGRAM` line | (d), (e), (f), (g) |
| `File save completed.`, then `>` | after the `E` block | all |

Any other line fails the SAVE: khi prints it verbatim, reads on to the end of the transfer, and leaves the target untouched (Article VI). In capture (h), `nosuch:Variable (or program) does not exist.` is such a line.

**Rationale**: The Architect's ruling of 2026-11-04. SAVE's completion line carries no error count, and capture (h) ends `File save completed.` after an error, so a line a good SAVE never prints is the only sign of failure.

**Consequence**: A SAVE whose data holds a section no capture shows, such as variables, prints a label no capture holds, and fails until a capture adds that label. This is Article IV: a line no capture shows is not known to be normal.

**A `ff` byte in the data**: no capture shows one, so `SaveLineRule` treats a `D` block holding one like an unknown line, and the save fails with the target untouched (FR-040). This is not implemented beyond failing safely.

**Alternatives considered**: Matching error text such as `does not exist` would miss any error worded otherwise. Trusting `File save completed.` is what capture (h) disproves.
