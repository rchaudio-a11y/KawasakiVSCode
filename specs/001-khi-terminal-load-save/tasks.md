---

description: "Task list for 001: khi, terminal, LOAD and SAVE from VS Code"
---

# Tasks: khi — terminal, LOAD and SAVE from VS Code

**Input**: Design documents from `specs/001-khi-terminal-load-save/`: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md); constitution v3.1.0.

**Prerequisites**: Captures (a) to (o) and the three extras are checked in with confirmed notes. Captures (p) to (s) are checked in with confirmed notes (T044), so gate G0 is open.

**Tests**: Required. Article VII puts every implementation behind a failing test, Article V runs protocol tests against the captures over real sockets, Article XIII adds a test through the `khi` entry point for every guard, and the spec's invariants are tests.

**Order inside every story**: write the test, run it, confirm Red, report the Red to the Architect, then implement and confirm Green. A guard is trusted only after its fire demonstration: inject the defect, watch the test go red, revert, confirm green, and record the date, defect, red output and green output in the test file's comment block ([contracts/test-guards.md](contracts/test-guards.md)). A test that passes on code that does not exist yet is not Red.

**Organization**: Tasks are grouped by user story, so each story can be built and tested on its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel: different files, no dependency on an unfinished task.
- **[Story]**: The user story the task belongs to (US1 to US6 in spec.md).
- Every task names the file it touches.

## Path conventions

- `src/KawasakiVSCode.Core/` holds every behaviour, `src/KawasakiVSCode.Cli/` parses and wires, `tests/KawasakiVSCode.Tests/` holds the captures, the stand-in and the tests ([plan.md, Project Structure](plan.md#project-structure)).
- One type per `.vb` file. Every `.vb` file opens with the header block (file name, project, description, author `RCH Automation LLC`, created date), and every public member has an XML `<summary>`.
- Namespaces follow folders: `KawasakiVSCode.Core.Connection`, `KawasakiVSCode.Tests.Protocol`, and so on.
- Every protocol test carries `<Capture("<capture folder>")>` (T023) naming the capture it replays.
- Enter is `0d 0a` (CR LF), as capture (a) shows. Every timeout is 10 s by default (T015, research R9).
- "Record *n*" means the *n*-th record of that capture's `exchange.txt`, numbered as its note numbers them.

## Decisions these tasks encode

The Architect accepted D1, D2 and D4, added a fallback to D3 and a test to D5, and ruled on U1 to U4 (2026-11-04).

- **D1 The command bytes**: `khi load` and `khi save` send the command line, wait for its echo, then send `18`, the line again and `0d 0a`, as every KRterm LOAD and SAVE capture shows (for example (i) records 9 to 12, (d) records 9 to 12). The words are cased as captured: `load <name>` ((b), (i), (l), (n), (o)); `SAVE <name>`, `SAVE/P <name>` and `SAVE/FULL <name>` ((g), (d), (e)). data-model.md's `LOAD <bare name>` changes to `load <bare name>` (T029).
- **D2 Error lines during a LOAD**: a line the controller prints during a LOAD that is not a captured normal LOAD line is an error line, and khi adds `<file>: <line>` for it (FR-018). The normal lines are the echo and redraw of the `load` line, `Loading...(<file>)`, `Program  <name>()` and `File load completed. (<n> errors)`. The question line counts as an error line.
- **D3 Recognising the LOAD question**: a line of the captured form `(<digit>:<label>, <digit>:<label>)` in the controller's text after a `C` request ((c) record 18, (n) and (o) record 16). **Fallback**: any other unrecognised line followed by the controller going quiet for that wait's timeout, with no `>` prompt after it, is handed to the person the same way, instead of timing out (FR-021). A line followed by the `>` prompt is a refusal, and the load fails (capture (j)). Either way, the wait for the person's answer has no limit.
- **D4 Regions and slices**: each test gives its payload and typed regions as record and byte numbers quoted from the capture's note. A test may replay records 1 to 8 (the login) followed by a later range of the same capture, such as (f) records 19 to 30.
- **D5 Captures (h) and (m) replay only through `khi term`**: their SAVE lines name a program (`=nosuch`, `=capb`), which `khi save` never sends. So INV-19 and INV-4's controller-error case run through a typed SAVE whose test input is KRterm's typed bytes. A separate test (T071) shows `khi save` itself puts every line outside the file data through `SaveLineRule`.

**Cases no capture covers, as the Architect ruled** (each with its test):

- **U1, a question while file data is still to be sent**: the question comes with the controller's request for the next block, and khi answers it as KRterm does, with the file's next block, as it answers any request (FR-016). It then shows the line, hands it to the person, and sends nothing more of the file until the controller asks again (FR-021, T056). The captures overrule the earlier end-of-file-early ruling (Architect, 2026-11-04).
- **U5, an empty file to load**: khi sends what capture (q) shows KRterm sending (spec edge case, T057).
- **U2, a `ff` or `17` byte in a file to load**: refused before connecting, naming the file, the byte and its offset from 0, exit 2 (FR-042, T059). `17` (ETB) ends a transfer block. The scan is not Article II validation. Two further `ff` cases stay not implemented, and documented:
  - **In SAVE data**: `SaveLineRule` treats a `D` block holding `ff` like an unknown line, so the save fails with the target untouched (FR-040, T069, T074).
  - **In a file for a LOAD typed in `khi term`**: a known gap, since AS files are ASCII.
- **U3, a LOAD or SAVE line typed in `khi term`**: relayed as typed, with no `18`; acceptance shows whether the controller starts the transfer (FR-013, T085, T098).
- **U4, a `khi save` target with no extension**: refused by name before connecting, exit 2 (FR-043, T072).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: The solution, its three projects, and nothing else (Article I).

- [x] T001 Mark `tests/KawasakiVSCode.Tests/Captures/**` and `tests/KawasakiVSCode.Tests/Fixtures/**` `-text` in `.gitattributes` (research R13); done before the first capture
- [x] T002 Create `.gitignore` at the repository root ignoring `bin/`, `obj/`, `.vs/`, `TestResults/` and `*.user`
- [x] T003 [P] Write `src/KawasakiVSCode.Core/KawasakiVSCode.Core.vbproj` by hand (research R3): `Microsoft.NET.Sdk`, `TargetFramework` `net10.0`, `RootNamespace` `KawasakiVSCode.Core`, `OptionStrict` `On`, `OptionExplicit` `On`, `OptionInfer` `Off`, `GenerateDocumentationFile` `true`, `LangVersion` `latest`; no `Nullable`, no `ImplicitUsings`, no package reference
- [x] T004 [P] Write `src/KawasakiVSCode.Cli/KawasakiVSCode.Cli.vbproj` the same way, plus `OutputType` `Exe`, `AssemblyName` `khi`, `RootNamespace` `KawasakiVSCode.Cli` and a project reference to Core
- [x] T005 [P] Write `tests/KawasakiVSCode.Tests/KawasakiVSCode.Tests.vbproj` the same way, plus `IsPackable` `false`, package references `MSTest.TestFramework`, `MSTest.TestAdapter` and `Microsoft.NET.Test.Sdk` (not the `MSTest` meta-package, research R5), project references to Core and Cli, and `Captures\**` and `Fixtures\**` copied to the output folder with `CopyToOutputDirectory` `PreserveNewest`
- [x] T006 Create `KawasakiVSCode.slnx` at the repository root with `dotnet new sln -n KawasakiVSCode`, then `dotnet sln KawasakiVSCode.slnx add` the three project files
- [x] T007 Run `dotnet restore KawasakiVSCode.slnx` and `dotnet list tests/KawasakiVSCode.Tests/KawasakiVSCode.Tests.vbproj package --include-transitive`; confirm neither `Microsoft.Testing.Extensions.Telemetry` nor `Microsoft.Testing.Extensions.CodeCoverage` comes in (research R5); if one does, record it in `specs/001-khi-terminal-load-save/research.md` R5 and set `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` for test runs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The conformance guards, the stand-in, the connection and login, the commands, the robots file, the result line and the entry point. Every verb needs them.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

### Conformance guards

These pass on an empty code base, which is not Red. Each is shown red by its fire demonstration in T042, once code exists.

- [x] T008 [P] Write `tests/KawasakiVSCode.Tests/Conformance/RepositoryPaths.vb`: finds the repository root by walking up from the test assembly's folder to the one holding `KawasakiVSCode.slnx`, and lists every `.vbproj` and `.vb` file under `src/` and `tests/`, skipping `bin/` and `obj/`
- [x] T009 [P] Write `tests/KawasakiVSCode.Tests/Conformance/OptionSettingsTests.vb`: every project file sets `OptionStrict` `On`, `OptionExplicit` `On`, `OptionInfer` `Off` and `GenerateDocumentationFile` `true` (review gate 1)
- [x] T010 [P] Write `tests/KawasakiVSCode.Tests/Conformance/HeaderBlockTests.vb`: every `.vb` file opens with a comment block giving its file name, the project, a description, the author `RCH Automation LLC` and a created date
- [x] T011 [P] Write `tests/KawasakiVSCode.Tests/Conformance/OneTypePerFileTests.vb`: every `.vb` file declares exactly one top-level `Class`, `Module`, `Interface`, `Enum` or `Structure` (Article I)
- [x] T012 [P] Write `tests/KawasakiVSCode.Tests/Conformance/XmlDocCoverageTests.vb` (research R4): load `KawasakiVSCode.Core.dll` and `khi.dll` from the test output folder, list every public type and its public methods, properties, events and interface members, and fail for each with no `<summary>` in `KawasakiVSCode.Core.xml` or `khi.xml`

### Exit codes and refusals

- [x] T013 [P] Write `tests/KawasakiVSCode.Tests/Reporting/ExitCodeTests.vb`: `ExitCode` values Success 0, ControllerError 1, RefusedBeforeConnecting 2, TimedOut 3, ConnectionFailed 4, LocalFileFailed 5 ([contracts/cli.md, Exit codes](contracts/cli.md#exit-codes)); and a `KhiRefusal` built with an exit code and a message carries both unchanged
- [x] T014 Implement `src/KawasakiVSCode.Core/Reporting/ExitCode.vb` (the enum) and `src/KawasakiVSCode.Core/Reporting/KhiRefusal.vb` (an exception carrying an `ExitCode` and the message khi prints on stderr)

### Timeouts

- [x] T015 [P] Write `tests/KawasakiVSCode.Tests/Connection/TimeoutsTests.vb`: `Timeouts.Defaults` gives 10 s for every `WaitStep` (`Connect`, `Negotiation`, `LoginPrompt`, `LoginReply`, `CommandReply`, `TransferData`, `TransferEnd`, `TypedTransfer`); each step's message wording is data-model.md's ("connecting to `<address>:<port>`", "telnet negotiation", "the login prompt", "the prompt after login", "the reply to `<command>`", "transfer data", "the end of the transfer", "a typed `<SAVE or LOAD>`"); a test can build `Timeouts` with short values
- [x] T016 Implement `src/KawasakiVSCode.Core/Connection/WaitStep.vb` and `src/KawasakiVSCode.Core/Connection/Timeouts.vb`. Each default's XML doc cites research R9: the longest controller reply in any capture is 81 ms ((i) record 10, (l) record 2), three times that is under the 10-second floor
- [x] T017 [P] Record in `specs/001-khi-terminal-load-save/research.md` R9 the timeout table: every `WaitStep`, its 10 s value, and the capture and record of the longest reply seen

### The stand-in (Article V)

- [x] T018 [P] Write `tests/KawasakiVSCode.Tests/StandIn/CaptureReaderTests.vb`:
  - `Captures/a-connect-login/exchange.txt` reads as 8 records: record 1 is `C` `ff fb 18` at 0.000, and record 8 is `S` ending `0d 0a 3e`.
  - Lines starting `#` are skipped.
  - `Captures/e-save-full/exchange.txt` reads as 121 records carrying 394,969 bytes from the controller.
- [x] T019 Implement `tests/KawasakiVSCode.Tests/StandIn/CaptureRecord.vb` and `tests/KawasakiVSCode.Tests/StandIn/CaptureReader.vb` for the `exchange.txt` format of [contracts/capture-format.md](contracts/capture-format.md#exchangetxt)
- [x] T020 Write `tests/KawasakiVSCode.Tests/StandIn/ControllerStandInTests.vb` with a raw `TcpClient`:
  - Sending capture (a)'s `C` bytes, in any chunking, receives capture (a)'s `S` bytes byte for byte (User Story 1 scenario 2).
  - A wrong client byte fails the stand-in, which reports the record, the expected bytes and the received bytes.
  - `StopAt(record, Silent)` stops sending and holds the connection open. `StopAt(record, Close)` closes it.
  - A slice, records 1 to 8 and then a later range, plays as one capture (D4).
  - Client bytes sent after the capture's last record, or after a `Silent` stop, are recorded and not compared.
- [x] T021 Implement `tests/KawasakiVSCode.Tests/StandIn/ControllerStandIn.vb`, a `TcpListener` on 127.0.0.1 with port 0 that serves one client:
  - **Replay**: each `S` record's exact bytes are sent once the client has sent everything the capture shows before it.
  - **Checking the client**: client bytes outside payload and typed regions must equal the captured `C` bytes.
  - **Payload regions**: each region the test names (record and first byte) runs to its block's closing `17` and is compared with the fixture the test gives, so a fixture of another length than the captured file still fits.
  - **Typed regions**: their bytes come from the test's own input.
  - **After the end**: client bytes after the capture's last record, or after a `Silent` stop, are recorded and not compared.
  - **No other source**: no member takes controller bytes from anywhere but a capture (INV-3).
- [x] T022 Write `tests/KawasakiVSCode.Tests/StandIn/CommandRecorderTests.vb`, then implement `tests/KawasakiVSCode.Tests/StandIn/CommandRecorder.vb` (INV-2):
  - **Recording**: every client line outside payload regions is recorded as a command: the bytes up to `0d 0a`, with a leading `18` removed.
  - **The allowed set**: the login `as`, `load <name>`, `SAVE <name>`, `SAVE/P <name>`, `SAVE/FULL <name>`, the answer the test typed at a load question, and lines the test typed in `khi term`.
  - **Enforcement**: `ControllerStandIn.Dispose` fails the test if any recorded command is outside the allowed set.
- [x] T023 [P] Implement `tests/KawasakiVSCode.Tests/StandIn/CaptureAttribute.vb`: `<Capture("<folder>")>` names the capture a protocol test replays (INV-3)

### Connection and login

- [x] T024 Write `tests/KawasakiVSCode.Tests/Connection/ControllerConnectionTests.vb` against the stand-in:
  - Connecting to a port nobody listens on gives exit 4 with `khi: cannot connect to robot <name> at <address>:<port>: <system reason>`.
  - `ReadUntil` against a silent stand-in gives exit 3 with `khi: timed out waiting for <step> (robot <name>)`.
  - A stand-in that closes gives exit 4 with `khi: robot <name> closed the connection` ([contracts/cli.md](contracts/cli.md#timeout-and-connection-messages-stderr)).
- [x] T025 Implement `src/KawasakiVSCode.Core/Connection/ControllerConnection.vb`:
  - `Connect(robot, timeouts)`, and `ReadUntil(step, until)` with that step's timeout on every read.
  - `Send(command As ControllerCommand)`, `SendFrame(frame As TransferFrame)` and `PassThrough(bytes)` are the only ways bytes leave khi (research R15).
  - `ReadUnbounded` is used only by `TerminalRelay`, and while a question during `khi load` or during a typed transfer waits for the person (Article X).
  - *Built 2026-11-04 with `Connect`, `Negotiate` (the telnet offer of capture (a)), `Send` and `ReadUntil`. `SendFrame`, `PassThrough` and `ReadUnbounded` come with their first callers in User Story 2: `TransferFraming` (T060), `PersonInput` (T062) and the question's wait in `LoadOperation` (T063).*
- [x] T026 Write `tests/KawasakiVSCode.Tests/Protocol/LoginTests.vb` `<Capture("a-connect-login")>`:
  - khi sends `ff fb 18` first, and nothing in reply to `ff fd 18`.
  - At `login: ` it sends `61 73 0d 0a`.
  - It returns at the `0d 0a 3e` after `This is AS monitor terminal "AUX2"`.
  - INV-5: with the stand-in silent at record 2, exit 3 names "telnet negotiation". With it silent at record 8, exit 3 names "the prompt after login".
- [x] T027 Implement `src/KawasakiVSCode.Core/Connection/TelnetNegotiator.vb`, `src/KawasakiVSCode.Core/Session/LoginName.vb` (the constant `as`, its XML doc citing 90209-1022 §2.6.1 and capture (a), Article IX) and `src/KawasakiVSCode.Core/Session/LoginSequence.vb`

### Commands

- [x] T028 Write `tests/KawasakiVSCode.Tests/Commands/ControllerCommandTests.vb`:
  - `ControllerCommand.Load("C:\cells\capb.as")` reads `load capb.as`: the bare name (FR-027).
  - `ControllerCommand.Save` gives `SAVE capd.as`, `SAVE/P capd.as` and `SAVE/FULL capd.as`.
  - Against the stand-in replaying (i) records 1 to 12, `Send` sends the line, waits for its echo, then sends `18`, the line and `0d 0a`, and returns at `>` + line + `0d 0a` (D1).
  - A bare name outside ASCII is refused by name, exit 2; a folder outside ASCII is not (FR-044; Architect, 2026-11-04). Fire: accept every character.
- [x] T029 Implement `src/KawasakiVSCode.Core/Commands/ControllerCommand.vb` and `ControllerConnection.Send(command)` (D1): the line, `ReadUntil(CommandReply)` for its echo, then `18` + line + `0d 0a`, then `ReadUntil(CommandReply)` for `>` + line + `0d 0a`, as `specs/001-khi-terminal-load-save/data-model.md`'s `ControllerCommand` table gives them
- [x] T030 [P] Write `tests/KawasakiVSCode.Tests/Commands/SaveQualifierTests.vb` (FR-024, FR-025):
  - No qualifier gives `SAVE`, `P` gives `SAVE/P`, and `FULL` gives `SAVE/FULL`, as the manual writes them.
  - `L`, `R`, `S`, `A`, `SYS`, `ROB`, `ELOG`, `OPLOG`, `ALLLOG` and `STG` are refused by name as not supported.
  - Anything else is refused by name as unknown.
- [x] T031 Implement `src/KawasakiVSCode.Core/Commands/SaveQualifier.vb` with `SaveQualifier.Parse`, the one door (research R15)

### Robots (Article IX)

- [x] T032 [P] Write `tests/KawasakiVSCode.Tests/Robots/RobotsFileTests.vb` ([contracts/robots-file.md](contracts/robots-file.md)):
  - A valid file reads.
  - A missing file gives `khi: no khi.robots.json in <folder>`.
  - Each broken rule gives `khi: khi.robots.json: <what is wrong and where>` with exit 2:
    - `robots` missing or empty.
    - A name broken: "Required and non-empty; unique within the file, ignoring case".
    - An address broken: "Required: a host name or IP address".
    - A port broken: "Required, 1 to 65535".
    - A simulator flag broken: "Required; read and type-checked, but no behaviour depends on it in this feature".
    - A default broken: "`Default`, when present, must name an entry".
    - Any unknown key, refused by name.
- [x] T033 Implement `src/KawasakiVSCode.Core/Robots/RobotEntry.vb` and `src/KawasakiVSCode.Core/Robots/RobotsFile.vb` with `System.Text.Json` (research R7), reading `khi.robots.json` from the current folder only
- [x] T034 Write `tests/KawasakiVSCode.Tests/Robots/RobotSelectionTests.vb`, where every refusal is exit 2 before connecting:
  - `--robot <name>` picks that entry, matched ignoring case as names are unique ignoring case.
  - With no `--robot`, the default is used.
  - With neither, khi refuses with `khi: no --robot given and no default set; robots are: <names>`.
  - An unknown name is refused with `khi: unknown robot <name>; robots are: <names>`.
- [x] T035 Implement `src/KawasakiVSCode.Core/Robots/RobotSelection.vb` with `RobotSelection.Resolve`, the one door (research R15)

### The result line

- [x] T036 [P] Write `tests/KawasakiVSCode.Tests/Reporting/ResultLineTests.vb` ([contracts/cli.md](contracts/cli.md#the-result-line-khi-load-khi-save)):
  - The form is `khi <verb> <ok|failed>: robot <name>, file "<path>", <n> bytes, programs <a, b | none>`, with the path as the person gave it, bytes of file content only, and `none` when the controller reported no program.
  - The question part reads `; completed with 1 error; answered 0 (Change to comment and continue); the controller's copy differs from the file`, or `; completed with 1 error; answered 1 (Delete program and abort)`.
  - The count reads `<n> errors` when it is above one.
  - When the bytes sent differ from the file's size, they read `sent <n> of <size> bytes`, as `sent 1024 of 1815 bytes` after capture (p) (Architect, 2026-11-04; reopened for it).
  - There is no parenthesis when the answer matches no choice.
- [x] T037 Implement `src/KawasakiVSCode.Core/Reporting/TransferOutcome.vb`, `src/KawasakiVSCode.Core/Reporting/QuestionAnswer.vb` and `src/KawasakiVSCode.Core/Reporting/ResultLine.vb` (data-model TransferOutcome)

### The entry point (Article XIII)

- [x] T038 Write `tests/KawasakiVSCode.Tests/Route/KhiAppArgumentTests.vb` through `KhiApp.Run(args, input, output, error, timeouts)`:
  - No verb, an unknown verb, an unknown option, an option the verb does not take, a repeated option, a missing `<file>`, an extra argument and an unsupported qualifier are each refused by name, with exit 2 and no connection ([contracts/cli.md, Synopsis](contracts/cli.md#synopsis)).
  - An option may come before or after `<file>`.
  - A verb not yet wired is refused with `khi: <verb> is not implemented yet` and exit 2 (FR-032).
- [x] T039 Implement `src/KawasakiVSCode.Cli/ParsedArguments.vb`, `src/KawasakiVSCode.Cli/ArgumentParser.vb` (research R6) and `src/KawasakiVSCode.Cli/KhiApp.vb`; `KhiApp.Run` turns every `KhiRefusal` into its message on stderr and its exit code
- [x] T040 Write `tests/KawasakiVSCode.Tests/Route/KhiExeTests.vb`: run the built `khi.exe` from the Cli project's output folder with no arguments; exit code 2 and the usage refusal on stderr (research R12)
- [x] T041 Implement `src/KawasakiVSCode.Cli/Program.vb`: `Main` cancels Ctrl+C through `Console.CancelKeyPress` with `e.Cancel = True` (FR-041, research R8), then calls `KhiApp.Run` with the console streams and `Timeouts.Defaults`
- [x] T042 Do the fire demonstrations of T009 to T012 and record each in its test file in `tests/KawasakiVSCode.Tests/Conformance/`:
  - T009: remove `OptionInfer` `Off` from Core's project file.
  - T010: drop the author line from one file.
  - T011: add a second class to one file.
  - T012: delete one `<summary>`.

**Checkpoint**: The foundation is ready. `khi` parses, refuses and exits; the stand-in replays captures; login works against capture (a).

---

## Phase 3: User Story 1 - Record the real sessions first (Priority: P1)

**Goal**: The real KRterm ↔ K-ROSET sessions are checked in, with notes, before any protocol code.

**Independent Test**: Every exchange FR-002, FR-003 and FR-036 list is present under `tests/KawasakiVSCode.Tests/Captures/` with its note, and the plan names the route it proved.

- [x] T043 [US1] Take captures (a) to (o) through the relay, and the extras `c-load-rejected-ctrl-c` (record only), `c-load-rejected-reconnect` and `i-load-existing-changed`. Check them in under `tests/KawasakiVSCode.Tests/Captures/` with notes the Architect confirmed (FR-002 to FR-004, FR-036; quickstart P0, P1)
- [x] T044 [US1] With the Architect, take captures (p), (q), (r) and (s) through the relay (FR-003, quickstart P1 steps 6 to 9), and check them in with confirmed notes. All four were taken and confirmed on 2026-11-04. Their inputs are generated:
  - (p): `tests/KawasakiVSCode.Tests/Captures/p-load-long-error/files/capp.as`, longer than 512 bytes, with capture (c)'s syntax error at step 3, in the first block. The question is answered with `1`.
  - (q): `tests/KawasakiVSCode.Tests/Captures/q-load-empty/files/capq.as`, 0 bytes.
  - (r): `tests/KawasakiVSCode.Tests/Captures/r-load-long-error-0/files/capr.as`, of (p)'s kind under the name `capr`. The question is answered with `0`.
  - (s): `tests/KawasakiVSCode.Tests/Captures/s-load-1024/files/caps.as`, exactly 1,024 bytes, with no error.
- [x] T045 [US1] Write `tests/KawasakiVSCode.Tests/Conformance/CaptureInventoryTests.vb` ([contracts/capture-format.md](contracts/capture-format.md#notemd)):
  - For each letter (a) to (s), a folder `<letter>-*` exists.
  - Every capture folder has an `exchange.txt` that `CaptureReader` reads.
  - Every `note.md` has a `**Status**:` line reading `Confirmed by the Architect`, and the headings What was done, Against what, Tools, Route, Date, Payload regions, Typed regions, Observations and Manual citations.
  - The SAVE captures (d), (e), (f), (g), (h) and (m) hold in `files/` the file KRterm wrote.
  - Fire: rename one heading in one note.
- [x] T046 [P] [US1] Write in `specs/001-khi-terminal-load-save/research.md` R2 the answer each capture gave, citing its note:
  - (a): the telnet options and the login.
  - (b) and (l): the LOAD framing and the 512-byte block.
  - (c): the rejected step is named by step, not by line.
  - (d), (e) and (g): one `D` block per line.
  - (h): a SAVE error is a line, and "completed" follows it.
  - (i): LOAD replaces a program on K-ROSET.
  - (m): KRterm renames the old file on the PC, and nothing travels in the protocol.
  - (n) and (o): the two answers.
  - (p) and (q): what KRterm sends at a question with file data left, and for an empty file.
  - (r) and (s): what the controller asks for after `0` with file data left, and what KRterm sends after a file's last full block.

**Checkpoint**: User Story 1 is complete; the captures are the ground every later story replays.

---

## Phase 4: User Story 2 - Load a program file into the controller (Priority: P1) 🎯 MVP

**Goal**: `khi load <file>` sends the file byte for byte and reports, in the controller's words, whether it loaded.

**Independent Test**: Replay the LOAD captures against `khi load`. The stand-in receives the file unchanged, the output carries the controller's text exactly, and the exit code follows the controller's result.

### Tests for User Story 2 ⚠️

> Write these first, run them, confirm Red, and report it before T060.

- [ ] T047 [P] [US2] Write `tests/KawasakiVSCode.Tests/Protocol/LoadProtocolTests.vb` `<Capture("b-load-small")>` (User Story 2 scenario 1, FR-016, FR-017, FR-028). Setup: `capb.as` copied from the capture's `files/` into a temporary folder, with a `khi.robots.json` there naming the stand-in. Then `khi load capb.as`:
  - Sends the command (D1).
  - Answers the `A` block naming `capb.as` with `02 41 20 20 20 20 30 17`.
  - Sends one `C` block, `02 43 20 20 20 20 30`, the 103 file bytes and `17`, then `02 43 20 20 20 20 30 1a 17`.
  - Answers the `E` block with `02 45 20 20 20 20 30 17`.
  - Copies the controller's bytes to stdout unaltered, ending with `khi load ok: robot <name>, file "capb.as", 103 bytes, programs capb`.
  - Exits 0.
  - **No connection on a refusal** (T038's check, asserted here because `khi load` is the first verb that connects): in the same folder, with the stand-in listening, each command line T038 refuses gives exit 2 and the stand-in accepts no connection (`ControllerStandIn` reports whether it accepted one). Fire: connect before parsing the command line.
- [ ] T048 [P] [US2] Write INV-1 in `tests/KawasakiVSCode.Tests/Protocol/LoadContentTests.vb` `<Capture("b-load-small")>`. Create the fixture `tests/KawasakiVSCode.Tests/Fixtures/inv1-mixed/capb.as`: under 512 bytes, with trailing spaces, tabs, and CR LF, LF and CR line ends. The payload region (record 37, bytes 8 to 110) is compared with the fixture, and the stand-in receives it unchanged. Fire: trim trailing whitespace before sending
- [ ] T049 [P] [US2] Write `tests/KawasakiVSCode.Tests/Protocol/LargeLoadTests.vb` `<Capture("l-load-large")>`:
  - `capl.as` goes as 120 `C` blocks of 512 file bytes and one of 1 byte, each sent only after the controller's next `C` request, then the end-of-file block.
  - The 121 payloads (odd records 15 to 255) equal `files/capl.as`.
  - `<Capture("s-load-1024")>`, with `files/caps.as`, as a slice of records 1 to 8 and 30 to 55 (D4): khi sends two full blocks, then an empty block, then the end-of-file block at the next request, as records 46 to 52 show.
  - Exit 0.
- [ ] T050 [P] [US2] Write INV-12 in `tests/KawasakiVSCode.Tests/Protocol/LoadPathTests.vb` `<Capture("b-load-small")>`: `capb.as` given by its full path; the recorded command is `load capb.as`. Fire: send the full path
- [ ] T051 [P] [US2] Write `tests/KawasakiVSCode.Tests/Protocol/LoadReplacesTests.vb` `<Capture("i-load-existing-changed")>`, records 1 to 20: `khi load capi.as` exits 0 with `programs capb` (User Story 2 scenario 4)
- [ ] T052 [P] [US2] Write `tests/KawasakiVSCode.Tests/Protocol/LoadInUseTests.vb` `<Capture("f-typed-save-load")>`, slice of records 1 to 8 and 19 to 30 (D4). `khi load capb.as` (User Story 2 scenario 5, FR-017, FR-018):
  - Prints `Cannot execute because program already in use.` verbatim.
  - Adds `capb.as: Cannot execute because program already in use.`
  - Ends with `khi load failed: …`.
  - Exits 1.
- [ ] T053 [P] [US2] Write INV-7 and INV-11 in `tests/KawasakiVSCode.Tests/Protocol/LoadQuestionTests.vb` `<Capture("n-load-question-0")>` and `<Capture("o-load-question-1")>`, with short timeouts:
  - Up to the question, the replay runs as captured.
  - Nothing is sent while the test's input stays empty for longer than every timeout, and a `03` in the input sends nothing.
  - Input `0` and CR LF (or `1`) sends `30` (or `31`) and `0d 0a`, and the replay runs to its end.
  - The output carries the controller's text exactly, a `capn.as: <line>` line for each error line (D2), and the result line with the question part (FR-021).
  - Exit 1 after either answer.
  - Input ending before an answer gives exit 1 and `khi: no answer to the controller's question; robot <name> may still be waiting on it and may need a restart`.
  - Fires: send `1` at the question before the test types (INV-11); reword the controller's text (INV-7).
- [ ] T054 [P] [US2] Write `tests/KawasakiVSCode.Tests/Route/LoadRouteTests.vb` through `KhiApp.Run`:
  - A missing file is refused by name before connecting, exit 2 (FR-019).
  - A file name outside ASCII is refused by name before connecting, exit 2 (FR-044).
  - With nothing listening, exit 4.
  - INV-5: capture (b) silent at record 36 gives exit 3 naming "transfer data".
  - Capture (b) closed at record 38 gives exit 4, the FR-020 message `the load did not finish and the controller's memory may have changed`, and the result line.
  - A `03` in the input during the transfer ends nothing (FR-041).
- [ ] T055 [P] [US2] Write the D3 fallback in `tests/KawasakiVSCode.Tests/Protocol/LoadQuietTests.vb` `<Capture("f-typed-save-load")>`, slice of records 1 to 8 and 19 to 27, the stand-in `Silent` at record 28, with short timeouts:
  - After `Cannot execute because program already in use.` (record 26) and khi's end-of-file block (record 27), the controller goes quiet.
  - When the `TransferEnd` timeout runs out, khi does not exit 3: it hands the line to the person and keeps waiting, for longer than every timeout.
  - Input `1` and CR LF sends `31 0d 0a`, recorded after the stop.
  - Input then ending gives exit 1 and the no-answer message (FR-021).
  - A controller that goes quiet after normal lines only still exits 3: capture (b) silent at record 36, as T054 shows.
  - Fire: time out after an unrecognised line, as after a normal one.
- [ ] T056 [P] [US2] Write U1 in `tests/KawasakiVSCode.Tests/Protocol/LoadEarlyQuestionTests.vb` `<Capture("p-load-long-error")>`, with `files/capp.as`. Write the test from the capture's note once T044 has taken it:
  - The replay is a slice, records 1 to 8 and 11 to 25, leaving out the Architect's stray Ctrl+V (records 9 and 10) (D4).
  - The question arrives in record 18 with the controller's request for the next block; khi answers it with the file's second block, bytes 513 to 1,024 (record 19), before any answer is typed, shows the line, and waits for the person.
  - Input `1` and CR LF sends `31 0d 0a`, and the replay runs to its end; no more of the file is sent.
  - The result line reads `sent 1024 of 1815 bytes` and names `answered 1 (Delete program and abort)`.
  - Exit 1.
  - `<Capture("r-load-long-error-0")>`, with `files/capr.as`: after input `0` and CR LF, khi answers each further request as (r) shows. Write it from the capture's note once T044 has taken it.
  - Fire: send the end-of-file block in place of the next data block at the question, as the overruled reading had it.
- [ ] T057 [P] [US2] Write U5 in `tests/KawasakiVSCode.Tests/Protocol/LoadEmptyFileTests.vb` `<Capture("q-load-empty")>`, with `files/capq.as` (0 bytes). Write the test from the capture's note once T044 has taken it: khi sends exactly what (q) shows KRterm sending for an empty file, the result line counts 0 bytes, and the exit code follows the controller's completion line (FR-017).
- [ ] T058 [P] [US2] Write H1's rule in `tests/KawasakiVSCode.Tests/Transfer/LoadLineRuleTests.vb`, from capture (j)'s controller text:
  - `(P2016)Cannot open the file.` followed by `0d 0a 3e` (records 14 to 16) is a refusal ending at the prompt: the load fails with those words, and the line is never handed to the person (FR-021).
  - (n)'s choice line (record 16) is a question, and `LabelFor("0")` is `Change to comment and continue`.
  - (f)'s `Cannot execute because program already in use.` with no prompt after it is an error line, which the fallback may hand to the person.
  - The normal lines of (b) are normal.
  - Fire: treat a line followed by `>` as fallback-eligible.
  - (j) is a typed LOAD answered with `A-1052`, which `khi load` never sends because it refuses a missing file before connecting. So its text is checked here, on the rule, rather than in a replay through `khi load`.
- [ ] T059 [P] [US2] Write U2 in `tests/KawasakiVSCode.Tests/Route/LoadByteRefusalTests.vb` through `KhiApp.Run`, with the fixtures `tests/KawasakiVSCode.Tests/Fixtures/u2-ff/capb.as`, which holds `ff` at offset 37, and `tests/KawasakiVSCode.Tests/Fixtures/u2-etb/capb.as`, which holds `17` at offset 52:
  - `khi load capb.as` exits 2 with `khi: capb.as holds byte ff at offset 37; it cannot be sent`, and with `khi: capb.as holds byte 17 at offset 52; it cannot be sent` (FR-042, contracts/cli.md).
  - No connection reaches the listening stand-in.
  - Fire: send the file anyway.

### Implementation for User Story 2

- [ ] T060 [US2] Implement `src/KawasakiVSCode.Core/Transfer/TransferFrame.vb` and `src/KawasakiVSCode.Core/Transfer/TransferFraming.vb`, the one door for framing bytes (captures (b), (j), (l)):
  - **Reading**: controller frames, `05 02 <letter> <body> 17`, and the text between them.
  - **Building KRterm's answers**:
    - `02 <letter> 20 20 20 20 30 17`.
    - The data block `02 43 20 20 20 20 30 <up to 512 file bytes> 17`.
    - The end-of-file block `02 43 20 20 20 20 30 1a 17`.
    - The missing-file answer `02 41 2d 31 30 35 32 17`.
- [ ] T061 [US2] Implement `src/KawasakiVSCode.Core/Transfer/LoadLineRule.vb` (D2, D3):
  - The normal LOAD lines are the echo and redraw of the `load` line, `Loading...(<file>)`, `Program  <name>()` with two spaces, and `File load completed. (<n> errors)`. Every other line is an error line.
  - An error line followed by the `>` prompt is a refusal (H1).
  - A question is a line `(<digit>:<label>, <digit>:<label>)`.
  - `LabelFor(answer)` returns the text after `<answer>:`, up to the next choice or `)`.
- [ ] T062 [US2] Implement `src/KawasakiVSCode.Core/Terminal/PersonInput.vb`, the one door for the person's keys:
  - Reads the input stream and drops every `03`, Ctrl+C (FR-014, FR-041).
  - Reports `1d`, Ctrl+], and end of input.
  - Returns a line typed up to Enter as its bytes followed by `0d 0a`.
- [ ] T063 [US2] Implement `src/KawasakiVSCode.Core/Transfer/LoadOperation.vb`:
  - **Start**: the command (D1). The `A` block naming the bare name is answered.
  - **Blocks**: one `C` block per controller `C` request, up to 512 file bytes each; a block shorter than 512, an empty one included, is the last, and the next request gets the end-of-file block (FR-016; captures (b), (l), (q), (r), (s)).
  - **End**: on the `E` block, `E    0`, then `File load completed. (<n> errors)` and `>`.
  - **Output**: the controller's text goes to stdout unaltered.
  - **A question** (D3) waits on `PersonInput` without limit, sends the typed answer, and reads on.
  - **A question with file data left** (U1): the request it comes with is answered like any other, with the next block; then the question goes to the person, and no block is sent until the controller asks again (capture (p)).
  - **An empty file** (U5): what capture (q) shows.
  - **A refusal** (H1): an error line followed by the `>` prompt fails the load.
  - **The fallback** (D3): when a wait that follows an error line runs out with no `>` prompt, the line goes to the person as a question instead of timing out.
  - **Waits**: every other wait is bounded by its `WaitStep`.
- [ ] T064 [US2] Implement `src/KawasakiVSCode.Cli/LoadVerb.vb` and wire it in `src/KawasakiVSCode.Cli/KhiApp.vb`:
  - **Before connecting**: refuse a missing file (FR-019); refuse a file holding `ff` or `17`, naming the file, the byte and the offset from 0 of the first one (FR-042, U2); build the `ControllerCommand`, which refuses a name outside ASCII (FR-044); and resolve the robot.
  - **Run**: connect, log in, and run `LoadOperation`.
  - **Error lines**: print `<file>: <line>` for each error line (FR-018).
  - **Exit**: 0 only on `(0 errors)` with no question asked (FR-017).
  - **On a dropped connection or a timeout**: print the FR-020 message.
  - **Result line**: end with it (FR-028), with the question part after a question (FR-021).
- [ ] T065 [US2] Run every User Story 2 test green, and record the fire demonstrations of T048, T050, T053, T055 and T056 in their test files in `tests/KawasakiVSCode.Tests/Protocol/`, of T058 in `tests/KawasakiVSCode.Tests/Transfer/`, and of T059 in `tests/KawasakiVSCode.Tests/Route/`

**Checkpoint**: `khi load` works against every LOAD capture; this is the MVP.

---

## Phase 5: User Story 3 - Save controller data to a file (Priority: P1)

**Goal**: `khi save` copies controller data into a file, and a failed save never harms the file already there.

**Independent Test**: Replay the SAVE captures against `khi save`, whole and cut short. The target holds the controller's data after a complete transfer, and is byte-identical to before after an incomplete one.

### Tests for User Story 3 ⚠️

> Write these first, run them, confirm Red, and report it before T073.

- [ ] T066 [P] [US3] Write `tests/KawasakiVSCode.Tests/Protocol/SaveProtocolTests.vb` `<Capture("d-save-p")>`, `<Capture("e-save-full")>` and `<Capture("g-save-bare")>` (User Story 3 scenario 1). Commands: `khi save capd.as --qualifier P`, `khi save cape.as --qualifier FULL` and `khi save capg.as`. Each:
  - Sends the command (D1).
  - Answers `B` with `02 42 20 20 20 20 30 17` and `E` with `02 45 20 20 20 20 30 17`.
  - Writes the target byte-identical to the capture's `files/` file: 62,578, 372,831 and 311,587 bytes.
  - Leaves no temporary file.
  - Prints, for (d), `khi save ok: robot <name>, file "capd.as", 62578 bytes, programs capb, capl, home`.
  - Exits 0.
- [ ] T067 [P] [US3] Write `tests/KawasakiVSCode.Tests/Protocol/SaveReplaceTests.vb` `<Capture("d-save-p")>`: with an existing `capd.as` in the folder, the target is replaced, and no `.bas` or other file is left (FR-026, User Story 3 scenarios 2 and 5)
- [ ] T068 [P] [US3] Write INV-4 in `tests/KawasakiVSCode.Tests/Protocol/SaveSafetyTests.vb` `<Capture("d-save-p")>`, with an existing target holding known bytes:
  - The stand-in closing at record 16 gives exit 4.
  - The stand-in silent at record 16 gives exit 3 naming "transfer data".
  - Each time, the target is byte-identical to before and no temporary file remains.
  - Fire: write straight to the target.
- [ ] T069 [P] [US3] Write `tests/KawasakiVSCode.Tests/Transfer/SaveLineRuleTests.vb` (research R17): every line outside the `D` blocks in captures (d), (e), (f), (g) and (m) is normal; `nosuch:Variable (or program) does not exist.` from capture (h) is not; a `D` block whose payload holds `ff` is treated as unknown (FR-040). Fire: accept any line
- [ ] T070 [P] [US3] Write `tests/KawasakiVSCode.Tests/Route/SaveRouteTests.vb` through `KhiApp.Run`:
  - An unsupported qualifier is refused by name before connecting (FR-024).
  - A target name outside ASCII is refused by name before connecting, exit 2 (FR-044).
  - A missing target folder is refused before connecting, naming the path.
  - A target held open by another process gives exit 5 after the replay of (d), with the target byte-identical and no temporary file (research R10).
  - A `03` in the input during the transfer ends nothing (FR-041).
- [ ] T071 [P] [US3] Write D5 in `tests/KawasakiVSCode.Tests/Protocol/SaveCheckedLinesTests.vb` `<Capture("d-save-p")>`, `<Capture("e-save-full")>` and `<Capture("g-save-bare")>`: `khi save`'s outcome (`SaveLines`, data-model) lists every line outside the file data, each marked normal by `SaveLineRule`. For (d) these are the SAVE line's redraw, `Saving...(capd.as)`, `Network Configuration data` twice, `Program capb`, `Program capl`, `Program home` and `File save completed.`. Fire: skip `SaveLineRule` in `SaveOperation`; the list comes back empty
- [ ] T072 [P] [US3] Write U4 in `tests/KawasakiVSCode.Tests/Route/SaveNameRefusalTests.vb` through `KhiApp.Run`:
  - `khi save capd --qualifier P` exits 2 with `khi: capd has no extension; name the file with one` (FR-043, contracts/cli.md).
  - No connection reaches the listening stand-in.
  - Fire: send `SAVE/P capd`.

### Implementation for User Story 3

- [ ] T073 [US3] Implement `src/KawasakiVSCode.Core/Transfer/SaveTarget.vb` (research R10, data-model SaveTarget):
  - `TempPath` is `.<name>.<8 random hex>.khi-tmp`, in the target's folder.
  - `Commit` is `File.Move(temp, target, overwrite:=True)`.
  - `Abandon` deletes the temporary file.
  - A failed rename leaves the target untouched and is exit 5.
- [ ] T074 [US3] Implement `src/KawasakiVSCode.Core/Transfer/SaveLineRule.vb` (FR-040, research R17), the one door. A `D` block whose payload holds `ff` is unknown. Its normal lines:
  - The echo and redraw of the SAVE line.
  - `Saving...(<file>)` and `Network Configuration data`.
  - The section labels R17 lists, `CONDITON DATA` spelled so.
  - `Program <name>`.
  - `File save completed.` and `>`.
- [ ] T075 [US3] Implement `src/KawasakiVSCode.Core/Transfer/SaveOperation.vb`:
  - **Start**: the command (D1). The `B` block is answered.
  - **Data**: each `D` block's payload, the bytes between `05 02 44` and `17`, goes to the temporary file.
  - **Text outside the blocks** goes to stdout and through `SaveLineRule`. Every line and its verdict goes into the outcome's `SaveLines` (D5), and unknown lines are kept.
  - **End**: on `E`, `E    0`, then `File save completed.` and `>`.
  - **Failure**: with any unknown line, the save fails once the transfer has ended.
  - **Waits**: every wait is bounded by its `WaitStep`.
- [ ] T076 [US3] Implement `src/KawasakiVSCode.Cli/SaveVerb.vb` and wire it in `src/KawasakiVSCode.Cli/KhiApp.vb`:
  - **Before connecting**: the qualifier goes through `SaveQualifier.Parse`; a missing target folder is refused; a target with no extension is refused by name (FR-043, U4); and the `ControllerCommand` is built, which refuses a name outside ASCII (FR-044).
  - **Commit**: only on completion with no unknown line (FR-022, FR-023, FR-040).
  - **Exit**: 1 on unknown lines, 3 on a timeout, 4 on a dropped connection, 5 on a failed replace.
  - **Result line**: `programs` from the `Program <name>` lines, and the bytes written (FR-028).
- [ ] T077 [US3] Run every User Story 3 test green, and record the fire demonstrations of T068, T069, T071 and T072 in their test files in `tests/KawasakiVSCode.Tests/Protocol/`, `tests/KawasakiVSCode.Tests/Transfer/` and `tests/KawasakiVSCode.Tests/Route/`

**Checkpoint**: `khi load` and `khi save` both work, each on its own.

---

## Phase 6: User Story 4 - Work at the controller's prompt (Priority: P2)

**Goal**: `khi term` is a terminal at the AS prompt, including SAVE and LOAD typed against files in the current folder.

**Independent Test**: Run `khi term` against the captures with the test typing KRterm's bytes, and against a silent stand-in. Only the typed commands are sent, a typed transfer uses the current folder, and an idle session stays open.

**Depends on**: User Story 2 (`TransferFraming`, `LoadOperation`'s block loop, `PersonInput`) and User Story 3 (`SaveOperation`, `SaveTarget`, `SaveLineRule`).

### Tests for User Story 4 ⚠️

> Write these first, run them, confirm Red, and report it before T087.

- [ ] T078 [P] [US4] Write `tests/KawasakiVSCode.Tests/Protocol/TerminalTests.vb` `<Capture("a-connect-login")>`:
  - `khi term` logs in, writes the controller's bytes to stdout unaltered, and sends the test's typed bytes unchanged, with no command of its own.
  - INV-6: silent after login for longer than every transfer timeout, the session stays open.
  - The stand-in closing gives exit 4 and `khi: robot <name> closed the connection`.
  - A `1d` in the input gives exit 0, and the `1d` is never sent.
  - INV-14: a `03` in the input is never sent and ends nothing.
  - INV-5: silent at record 2 gives exit 3.
  - Fires: apply the transfer timeout to the relay (INV-6); relay Ctrl+C (INV-14).
- [ ] T079 [P] [US4] Write `tests/KawasakiVSCode.Tests/Protocol/TypedLoadTests.vb`:
  - `<Capture("b-load-small")>`: the test types the `C` records among capture (b)'s records 9 to 33. khi recognises the `A` block from the framing (FR-012), supplies `capb.as` from the current folder unchanged, and hands back the prompt (User Story 4 scenario 3).
  - `<Capture("j-load-missing")>`: with no `nosuch.as`, khi answers `02 41 2d 31 30 35 32 17`, and the session continues.
- [ ] T080 [P] [US4] Write `tests/KawasakiVSCode.Tests/Protocol/TypedSaveTests.vb`, each capture typed:
  - `<Capture("d-save-p")>`: `capd.as` is written in the current folder through `SaveTarget`, equal to `files/capd.as`.
  - `<Capture("m-save-existing")>`, with `files/before/capm.as` in the folder: `capm.as` is replaced, equal to `files/capm.as`, with no `.bas` left (FR-026).
  - `<Capture("f-typed-save-load")>`: `capf.as` is written, the refused LOAD's text is shown, and the session continues (FR-011).
- [ ] T081 [P] [US4] Write INV-19, and INV-4's controller-error case, in `tests/KawasakiVSCode.Tests/Protocol/TypedSaveErrorTests.vb` `<Capture("h-save-refused")>` typed (D5):
  - An existing `caph.as` stays byte-identical.
  - `nosuch:Variable (or program) does not exist.` is shown.
  - No temporary file remains.
  - The session continues.
  - Fire: accept any line outside the file data.
- [ ] T082 [P] [US4] Write INV-10 in `tests/KawasakiVSCode.Tests/Protocol/TypedTransferSafetyTests.vb` `<Capture("d-save-p")>` typed:
  - The stand-in silent at record 16 gives exit 3; closed there, exit 4.
  - The target is byte-identical to before, with no temporary file.
  - Fire: apply the terminal's unlimited wait to the typed transfer.
- [ ] T083 [P] [US4] Write `tests/KawasakiVSCode.Tests/Protocol/TypedQuestionTests.vb` `<Capture("n-load-question-0")>` typed: at the question, with nothing typed, khi stays open past every transfer timeout (FR-015, Article X, v3.1.0); then the keystrokes `0` and Enter reach the controller unchanged, and the session returns to the prompt (User Story 4 scenario 9). Fire: apply the `TypedTransfer` timeout to the wait at the question
- [ ] T084 [P] [US4] Write `tests/KawasakiVSCode.Tests/Cli/ConsoleKeySourceTests.vb` on `ConsoleKeySource.KeyToBytes`: Enter gives `0d 0a`, Ctrl+C `03`, Ctrl+] `1d`, and a printable key its ASCII byte (research R8)
- [ ] T085 [P] [US4] Write U3 in `tests/KawasakiVSCode.Tests/Protocol/TypedLineAsTypedTests.vb` `<Capture("a-connect-login")>`:
  - After the prompt, the test types `load capb.as` and Enter, and then `SAVE/P capd.as` and Enter.
  - The stand-in records exactly `6c 6f 61 64 20 63 61 70 62 2e 61 73 0d 0a` and then the SAVE line's bytes and `0d 0a`, with no `18` and no repeated line (FR-013). These are client bytes after the capture's last record, so they are recorded, not compared.
  - Fire: send KRterm's `18` and the repeated line before Enter.
- [ ] T086 [P] [US4] Write the typed SAVE khi cannot write in `tests/KawasakiVSCode.Tests/Protocol/TypedSaveCannotWriteTests.vb` `<Capture("d-save-p")>` typed, with a folder named `capd.as` where the target would go:
  - The save cannot be committed: khi reports the path, closes the session, and exits 5 (spec edge case "A typed SAVE khi cannot write").
  - No temporary file remains.
  - Fire: return to the prompt as if the save had worked.

### Implementation for User Story 4

- [ ] T087 [US4] Implement `src/KawasakiVSCode.Cli/ConsoleKeySource.vb` (research R8):
  - **Interactive**: `Console.TreatControlCAsInput = True` and `Console.ReadKey(intercept:=True)`, mapped by `KeyToBytes` and presented as a `Stream`.
  - **Redirected**: input read raw from standard input.
- [ ] T088 [US4] Implement `src/KawasakiVSCode.Core/Terminal/TypedTransfer.vb`:
  - **On an `A` block**: the LOAD PC end, from `LoadOperation`'s block loop, against the named file in the current folder; `A-1052` when the file is missing.
  - **On a `B` block**: the SAVE PC end, from `SaveOperation`, into a `SaveTarget` for the announced name, under `SaveLineRule`.
  - **Waits**: every wait is bounded by `TypedTransfer`, and each keystroke restarts it, except the wait at a controller question, which has no limit (FR-015, Article X, v3.1.0). A question is recognised as `LoadOperation` recognises it (D3).
  - **Keystrokes** keep being relayed (FR-013).
  - **A save khi cannot write**: reported with its path, the session closed, exit 5, and no temporary file left (T086).
- [ ] T089 [US4] Implement `src/KawasakiVSCode.Core/Terminal/TerminalRelay.vb` and `src/KawasakiVSCode.Core/Terminal/TerminalSession.vb` (FR-010 to FR-015):
  - **The one unbounded read**: `TerminalRelay` is the only unbounded read of the controller besides the wait at a question during `khi load` or a typed transfer (Article X).
  - **Relaying**: `PersonInput` goes to the controller unchanged, a typed LOAD or SAVE line included, with nothing added (FR-013, U3); the controller's bytes go to stdout unaltered.
  - **Transfers**: `A` and `B` blocks are handed to `TypedTransfer`.
  - **Ending**: Ctrl+], the console closing or input ending gives exit 0; the controller closing gives exit 4.
- [ ] T090 [US4] Implement `src/KawasakiVSCode.Cli/TermVerb.vb` and wire it in `src/KawasakiVSCode.Cli/KhiApp.vb`; register `PosixSignal.SIGHUP` to close the socket and exit 0 when the console closes (research R8)
- [ ] T091 [US4] Run every User Story 4 test green, and record the fire demonstrations of T078, T081, T082, T083, T085 and T086 in their test files in `tests/KawasakiVSCode.Tests/Protocol/`

**Checkpoint**: All three verbs work.

---

## Phase 7: User Story 5 - Choose the controller by name (Priority: P2)

**Goal**: Every verb picks its controller by name or the workspace default, and refuses anything else before connecting.

**Independent Test**: Run each verb through the `khi` entry point with a named robot, an unknown name, a default and no default, and confirm the target or the refusal.

These are Article XIII reachability tests over doors already proved Red-first in T032 to T035.

- [ ] T092 [P] [US5] Write `tests/KawasakiVSCode.Tests/Route/RobotRouteTests.vb` through `KhiApp.Run`, for each verb, with two robots pointing at two stand-ins:
  - `--robot <name>` reaches that robot's stand-in (scenario 1).
  - With no `--robot`, the default is used (scenario 2).
  - No default and no `--robot`: refused listing the names, no connection, exit 2 (scenario 3).
  - An unknown name is refused by name, exit 2 (scenario 4).
  - A missing, unreadable or malformed robots file is refused naming the path (edge case).
- [ ] T093 [US5] Extend `tests/KawasakiVSCode.Tests/Route/KhiExeTests.vb` through the built `khi.exe` (research R12, FR-033):
  - An unknown robot gives exit 2.
  - A full `khi load` against the stand-in replaying (b) gives exit 0.
  - A full `khi save` replaying (d) gives exit 0.
- [ ] T094 [US5] Correct in `src/KawasakiVSCode.Cli/KhiApp.vb`, `src/KawasakiVSCode.Cli/LoadVerb.vb`, `src/KawasakiVSCode.Cli/SaveVerb.vb` and `src/KawasakiVSCode.Cli/TermVerb.vb` any wiring T092 and T093 show missing, keeping `RobotSelection.Resolve` the one door

**Checkpoint**: Every verb reaches only the robot the person named or the default.

---

## Phase 8: User Story 6 - Run it all from VS Code (Priority: P3)

**Goal**: A program repository copies the tasks template and runs khi from named tasks, with rejected programs in the Problems list.

**Independent Test**: The acceptance in INV-8, in a scratch repository with K-ROSET running (quickstart P3).

- [ ] T095 [P] [US6] Write `tests/KawasakiVSCode.Tests/Conformance/TasksTemplateTests.vb` ([contracts/tasks-template.md](contracts/tasks-template.md), FR-029, FR-030). `templates/tasks.json` holds exactly three tasks:
  - *khi: Load current file*: `khi load "${file}"`, both problem matchers, and `presentation` with `"reveal": "always"` and `"focus": true`.
  - *khi: Save controller to file*: `khi save "${input:khiSaveFile}" ${input:khiSaveQualifier}`, with no matcher.
  - *khi: Open terminal*: `khi term`.

  Its inputs and matchers:
  - The `khiSaveQualifier` `pickString` offers only full (`--qualifier FULL`), programs only (`--qualifier P`) and programs and data (nothing).
  - The patterns match `C:\cells\a.as:12: text` and `capo.as: (P0118)Illegal variable type.`.
  - They match neither `khi load failed: robot sim, file "capo.as", 148 bytes, programs capo` nor `Loading...(capo.as)`.
- [ ] T096 [US6] Create `templates/tasks.json` as [contracts/tasks-template.md](contracts/tasks-template.md) describes
- [ ] T097 [P] [US6] Write the README section in `README.md` (FR-031): publish khi with `dotnet publish src/KawasakiVSCode.Cli -c Release -o <folder on the PATH>`, copy `templates/tasks.json` into a program repository's `.vscode/`, and write its `khi.robots.json` ([contracts/robots-file.md](contracts/robots-file.md))
- [ ] T098 [US6] Run the acceptance by hand with the Architect against K-ROSET in a scratch repository, following `specs/001-khi-terminal-load-save/quickstart.md` P3, steps 1 to 7. Include:
  - The typed `SAVE x.as`, which also settles U3.
  - Ctrl+C with nothing selected, which should do nothing.
  - `terminal.integrated.sendKeybindingsToShell`, if VS Code holds back Ctrl+].

  Record the stamped lines two full saves differ by (INV-8, SC-009) in `specs/001-khi-terminal-load-save/quickstart.md`, P3 step 4.

**Checkpoint**: The feature works from VS Code.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: The suite-wide guards, the review gates and the final run.

- [ ] T099 [P] Write INV-3 in `tests/KawasakiVSCode.Tests/Conformance/NotAGuesserTests.vb`: every test method in the `KawasakiVSCode.Tests.Protocol` namespace carries `<Capture>` naming an existing capture folder. Fire: add a protocol test with no `<Capture>` and a reply written by hand
- [ ] T100 [P] Write `tests/KawasakiVSCode.Tests/Conformance/NoAddressInCodeTests.vb` (Article IX, FR-009): no `.vb` file under `src/` holds an IPv4 literal or `localhost`; the only address khi uses comes from `khi.robots.json`. Fire: put `127.0.0.1` in one Core file
- [ ] T101 Do INV-2's fire demonstration and record it in `tests/KawasakiVSCode.Tests/StandIn/CommandRecorderTests.vb`: make `src/KawasakiVSCode.Core/Transfer/LoadOperation.vb` send `ERESET` before LOAD, watch the stand-in fail the load tests on the recorded command, and revert
- [ ] T102 Run CodeMem `extract` on `KawasakiVSCode.slnx`, check `map_status` is current, and prove each claim with `references` or `type_usages`, stating the count ([contracts/test-guards.md](contracts/test-guards.md#structural-claims-proved-in-review), research R15):
  - Only `TerminalRelay`, and the wait at a question during `khi load` or a typed transfer, call the unbounded read.
  - `ControllerConnection.Send`, `SendFrame` and `PassThrough` are the only ways bytes leave khi.
  - `RobotSelection.Resolve`, `SaveQualifier.Parse`, `SaveLineRule`, `LoadLineRule`, `TransferFraming` and `PersonInput` are the only places their rules are applied.
- [ ] T103 [P] Check every command, qualifier and controller message khi relies on cites its manual number and section beside the capture that proves it (FR-006, Article XVI). Check the XML docs of `src/KawasakiVSCode.Core/Commands/ControllerCommand.vb`, `SaveQualifier.vb`, `Session/LoginName.vb`, `Transfer/LoadLineRule.vb` and `Transfer/SaveLineRule.vb`
- [ ] T104 INV-9: walk the ten review gates of constitution v3.1.0, each with its evidence, and record the result under the Constitution Check in `specs/001-khi-terminal-load-save/plan.md`
- [ ] T105 Run quickstart P2: `dotnet build KawasakiVSCode.slnx` and `dotnet test KawasakiVSCode.slnx` all green, and every guard in [contracts/test-guards.md](contracts/test-guards.md) with its fire record in its test file

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: none. T001 is done.
- **Foundational (Phase 2)**: depends on Setup; blocks every story. Its connection, login and command tasks (T024 to T029) send or await controller bytes, so under gate G0 they wait for T044, the captures (p) and (q).
- **User Story 1 (Phase 3)**: the captures (T043) are done. T045 and T046 need only Foundational.
- **User Story 2 (Phase 4)** and **User Story 3 (Phase 5)**: depend on Foundational only, and can run in parallel.
- **User Story 4 (Phase 6)**: depends on User Stories 2 and 3, whose framing, operations and save safety it reuses.
- **User Story 5 (Phase 7)**: depends on the verbs it routes through (Phases 4 to 6).
- **User Story 6 (Phase 8)**: depends on Phases 4 to 6. T098 also needs K-ROSET and the Architect.
- **Polish (Phase 9)**: depends on everything above.

### Within each story

- Test tasks first; confirm Red and report it; then implementation; then green with the fire records.
- Framing and rules (`TransferFraming`, `LoadLineRule`, `SaveLineRule`, `SaveTarget`, `PersonInput`) come before the operations that use them, and the operations before the verbs.

### Task dependencies worth naming

- T044 before T024 to T029 and every later task that sends or awaits controller bytes (gate G0).
- T019 before T020 and T021. T021 before every protocol test.
- T025 before T027 and T029.
- T035 and T039 before every verb task (T064, T076, T090).
- T060 to T063 before T088; T073 to T075 before T088.

---

## Parallel Examples

### User Story 2

```text
Task: "T047 LoadProtocolTests in tests/KawasakiVSCode.Tests/Protocol/LoadProtocolTests.vb"
Task: "T048 INV-1 in tests/KawasakiVSCode.Tests/Protocol/LoadContentTests.vb"
Task: "T049 LargeLoadTests in tests/KawasakiVSCode.Tests/Protocol/LargeLoadTests.vb"
Task: "T050 INV-12 in tests/KawasakiVSCode.Tests/Protocol/LoadPathTests.vb"
Task: "T051 LoadReplacesTests in tests/KawasakiVSCode.Tests/Protocol/LoadReplacesTests.vb"
Task: "T052 LoadInUseTests in tests/KawasakiVSCode.Tests/Protocol/LoadInUseTests.vb"
Task: "T053 INV-7 and INV-11 in tests/KawasakiVSCode.Tests/Protocol/LoadQuestionTests.vb"
Task: "T054 LoadRouteTests in tests/KawasakiVSCode.Tests/Route/LoadRouteTests.vb"
Task: "T055 the D3 fallback in tests/KawasakiVSCode.Tests/Protocol/LoadQuietTests.vb"
Task: "T056 U1 in tests/KawasakiVSCode.Tests/Protocol/LoadEarlyQuestionTests.vb"
Task: "T057 U5 in tests/KawasakiVSCode.Tests/Protocol/LoadEmptyFileTests.vb"
Task: "T058 the refusal rule in tests/KawasakiVSCode.Tests/Transfer/LoadLineRuleTests.vb"
Task: "T059 U2 in tests/KawasakiVSCode.Tests/Route/LoadByteRefusalTests.vb"
```

### User Story 3, alongside User Story 2

```text
Task: "T066 SaveProtocolTests in tests/KawasakiVSCode.Tests/Protocol/SaveProtocolTests.vb"
Task: "T068 INV-4 in tests/KawasakiVSCode.Tests/Protocol/SaveSafetyTests.vb"
Task: "T069 SaveLineRuleTests in tests/KawasakiVSCode.Tests/Transfer/SaveLineRuleTests.vb"
Task: "T070 SaveRouteTests in tests/KawasakiVSCode.Tests/Route/SaveRouteTests.vb"
Task: "T071 D5 in tests/KawasakiVSCode.Tests/Protocol/SaveCheckedLinesTests.vb"
Task: "T072 U4 in tests/KawasakiVSCode.Tests/Route/SaveNameRefusalTests.vb"
```

### Foundational

```text
Task: "T009 to T012 conformance guards in tests/KawasakiVSCode.Tests/Conformance/"
Task: "T013 ExitCodeTests, T015 TimeoutsTests, T018 CaptureReaderTests, T030 SaveQualifierTests, T032 RobotsFileTests, T036 ResultLineTests"
```

---

## Implementation Strategy

### MVP first (User Story 2 on the foundation)

1. Phase 1, Setup.
2. Phase 2, Foundational: the stand-in and login against capture (a) prove the ground.
3. Phase 3, User Story 1: the inventory test (T045) and research R2 (T046). The captures themselves are done.
4. Phase 4, User Story 2: `khi load` against every LOAD capture.
5. **Stop and validate**: run quickstart P2. `khi load` is usable from a command line.

### Incremental delivery

1. Add User Story 3: `khi save`, with its safety guards.
2. Add User Story 4: `khi term` and typed transfers.
3. Add User Story 5's route tests and User Story 6's template, README and acceptance.
4. Polish: INV-2, INV-3, the CodeMem proofs, the citations and the review gates.

---

## Notes

- Tasks marked **[P]** touch different files and depend on nothing unfinished.
- Report every Red to the Architect before implementing (Article VII). Diagnose-Before-Fix applies to every failure found on the way.
- Propose before building anything that touches more than one file in a way this list does not describe, changes a signature, or makes an architectural choice. D1 to D5 and U1 to U4 are settled (2026-11-04).
- Commit only when the Architect asks.
