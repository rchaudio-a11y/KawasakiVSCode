# Implementation Plan: khi — terminal, LOAD and SAVE from VS Code

**Branch**: `001-khi-terminal-load-save` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-khi-terminal-load-save/spec.md`, governed by constitution v2.0.1.

## Summary

khi is a VB.NET command-line tool that does a Kawasaki terminal's three jobs from VS Code: `khi term`, `khi load` (with an optional `--replace`) and `khi save`. It talks telnet to a controller's AS prompt over TCP.

The work runs in a fixed order. First the real sessions are recorded through a logging relay (KRterm → relay → K-ROSET), and those recordings are checked in. Only then is protocol code written, test-first, against an in-process stand-in that replays the recordings byte for byte over real sockets. KawasakiVSCode.Core holds every behaviour; KawasakiVSCode.Cli (assembly `khi`) parses arguments and wires; KawasakiVSCode.Tests holds the captures, the stand-in and the tests. A tasks template and a README section carry it into a program repository.

Two research findings shape the plan. The capture route is the relay, which the Architect and the agent prove together before taking captures (a) to (l) in the same session ([research R1](research.md#r1-capture-route)). The VB compiler does not enforce XML documentation, so a test does, as constitution 2.0.1 now requires ([research R4](research.md#r4-xml-documentation-enforcement)).

## Technical Context

**Language/Version**: VB.NET (`LangVersion` latest) on .NET 10 (LTS), `net10.0`; SDK 10.0.401 is installed here.

**Primary Dependencies**: The .NET base library only in Core and Cli (`System.Net.Sockets`, `System.Text.Json`, `System.IO`). Tests: `MSTest.TestFramework`, `MSTest.TestAdapter`, `Microsoft.NET.Test.Sdk`, justified under [Complexity Tracking](#complexity-tracking).

**Storage**: Files only. `khi.robots.json` is read from the current folder. AS files are read byte for byte, and saves go to a temporary file beside the target, replaced only on completion. Captures live under `tests/KawasakiVSCode.Tests/Captures/`.

**Testing**: MSTest. Protocol tests run over real TCP sockets against the in-process stand-in. Production-route tests go through `KhiApp.Run`, the code `Main` calls, and a few through the built `khi.exe`. Conformance tests cover the hard rules the compiler cannot. Acceptance is by hand in VS Code against K-ROSET.

**Target Platform**: Windows 11 x64, in VS Code's integrated terminal. The simulator is K-ROSET 1.8.6.19269, whose first robot answers at 127.0.0.1:9105 (K-IDE manual 2.5.4). The reference terminal is KRterm 1.2.8.

**Project Type**: A command-line tool over two class libraries' worth of behaviour: Core plus a wiring-only executable.

**Performance Goals**: None beyond the spec. A transfer that keeps moving never times out; a stalled one ends within its per-wait timeout.

**Constraints**: Article II (files are opaque, except `.PROGRAM` header names under `--replace`), III (only the allowed commands), IV (capture-backed bytes only), VI (temporary file and replace), X (bounded waits) and XV (no foreign runtime).

**Scale/Scope**: One person, one controller per run. Files range from a few kilobytes to a few megabytes for `SAVE/FULL`.

**Unknowns**: The telnet options, the login exchange, the transfer framing, the completion and error markers, and whether the controller names lines are all settled by the captures and by nothing else (Article IV). [Research R2](research.md#r2-protocol-details-come-from-the-captures) maps each question to the capture that answers it. No other unknown remains open.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Checked against constitution v2.0.1 before research, and again after design.

| Rule | How this plan meets it | Status |
|------|------------------------|--------|
| I Library-First | Three projects, no others. Verbs in Cli only parse and wire; behaviour sits in Core. One type per `.vb` file, checked by a conformance test. | Pass |
| II Files Are Opaque | Load streams the file unchanged (INV-1). `ProgramHeaderReader` reads only `.PROGRAM` header lines, and only under `--replace` (INV-15). | Pass |
| III The Person Commands the Robot | `ControllerCommand` is the one door for every command khi originates: login, LOAD, SAVE with its qualifier, DELETE/P, and the DELETE/P confirmation only as captured. The stand-in records every command, and the suite compares them to the allowed set (INV-2, INV-13, INV-17, INV-18). | Pass |
| IV Recorded Protocol Only | No protocol code before the captures are checked in (gate G0). Every protocol test names its capture (INV-3). | Pass, blocked on captures |
| V Integration Against Captures | The stand-in is a real `TcpListener` replaying captures. There are no socket or stream mocks. Acceptance runs against K-ROSET. | Pass |
| VI A Save Never Damages | `SaveTarget` writes a temporary file beside the target and replaces the target only after completion (INV-4, INV-10). | Pass |
| VII Test-First | Every task runs test, Red, report Red, implement, Green. Every guard records its fire demonstration in its test file. | Pass |
| VIII The Controller's Words | Controller text goes to the output unaltered, in the `<file>:<line>:` or `<file>:` form (INV-7). The result line is defined in [contracts/cli.md](contracts/cli.md). | Pass |
| IX Controllers Are Configuration | `khi.robots.json` is read through `RobotSelection`, the one door. The login name `as` is one constant in Core citing manual 2.6.1. The relay script takes its ports as required parameters with no defaults. | Pass |
| X Bounded Waits | Every read goes through `ControllerConnection` with a named `WaitStep` and a timeout. Only `TerminalRelay` holds the unbounded wait after login (INV-5, INV-6, INV-10). | Pass |
| XI Anti-Abstraction | No interfaces planned and no DI container; `Timeouts` is passed as a plain parameter. An abstraction needs three call sites in hand. | Pass |
| XII One Door for Every Rule | The doors are `RobotSelection`, `SaveQualifier`, `ControllerCommand`, `ProgramHeaderReader` and `ControllerConnection` waits. See [research R15](research.md#r15-one-door-per-rule). | Pass |
| XIII Production-Route Reachability | Every refusal and error has a test through `KhiApp.Run`; exit-code checks also run through `khi.exe`. | Pass |
| XIV Archive, Never Delete | Nothing to retire yet. | Pass |
| XV Compiled .NET | VB.NET on .NET 10. PowerShell only for the dev-time capture relay. The MSTest packages are justified below. | Pass, justified |
| XVI The Manuals Are the Reference | The spec's manual facts table is the citation source. Each command, qualifier and message cites manual and section beside its capture. The Manuals folder is denied to file tools in `.claude/settings.json`. | Pass |
| Option Strict / Explicit / Infer | Set in each `.vbproj` itself rather than a shared props file, because the gate checks every project file. A conformance test reads the files. | Pass |
| Header block | Checked by a conformance test on every `.vb` file. | Pass |
| XML documentation | `GenerateDocumentationFile` is on, and the conformance test 2.0.1 requires compares the public API with the XML documentation file ([R4](research.md#r4-xml-documentation-enforcement)). | Pass |
| Naming | Checked in review. | Pass |
| Diagnose-Before-Fix, Propose-Before-Building | Apply to implementation. Tasks touching more than one file are proposed first. | Pass |
| No placeholder values | Every verb and option not yet wired refuses itself with a message saying so (FR-032). | Pass |

**Gate G0, protocol code**: No task that sends or awaits controller bytes may start until FR-002, FR-003 and FR-036's captures are checked in with notes. A capture K-ROSET cannot produce leaves its behaviour recorded as not implemented (FR-003).

**Settled by the Architect's rulings of 2026-11-03**:
- **Capture route (FR-005)**: The relay, proved together. The Architect runs K-ROSET and KRterm, the agent runs the relay and walks [quickstart.md P0](quickstart.md#p0-prove-the-capture-route), and captures (a) to (l) follow in the same session. Gate G0 stands until they are checked in.
- **XML documentation**: Constitution 2.0.1 makes the conformance test the rule.
- **INV-18**: Restated so it is testable. Any reply to DELETE/P that is not a captured reply stops before LOAD, shown by replaying capture (k) with the reply altered ([contracts/capture-format.md](contracts/capture-format.md#the-stand-in)).
- **Keys at acceptance**: If VS Code's terminal holds back Ctrl+] or the AS control keys, check `terminal.integrated.sendKeybindingsToShell` and report what was needed.

**Re-check after design**: The design in [data-model.md](data-model.md) and [contracts/](contracts/) adds no project, interface, package or wait beyond those above. Every guard in the invariants has a named test and fire demonstration in [contracts/test-guards.md](contracts/test-guards.md). Pass.

## Project Structure

### Documentation (this feature)

```text
specs/001-khi-terminal-load-save/
├── spec.md
├── plan.md              # this file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── cli.md           # verbs, options, output, exit codes
│   ├── robots-file.md   # khi.robots.json
│   ├── capture-format.md# capture files, notes, the stand-in
│   ├── tasks-template.md# templates/tasks.json and its problem matcher
│   └── test-guards.md   # every guard, its test and its fire demonstration
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2, from /speckit-tasks
```

### Source Code (repository root)

```text
KawasakiVSCode.slnx
.gitattributes                      # captures and fixtures stored without line-ending conversion
README.md                           # publishing khi and copying the template
templates/
└── tasks.json
tools/
└── Start-CaptureRelay.ps1          # dev-time logging relay (Article XV)
src/
├── KawasakiVSCode.Core/
│   ├── KawasakiVSCode.Core.vbproj
│   ├── Robots/                     # RobotEntry, RobotsFile, RobotSelection
│   ├── Connection/                 # ControllerConnection, TelnetNegotiator, Timeouts, WaitStep
│   ├── Session/                    # LoginSequence, LoginName, SessionState
│   ├── Commands/                   # ControllerCommand, SaveQualifier
│   ├── Transfer/                   # LoadOperation, SaveOperation, TransferFraming, SaveTarget,
│   │                               # ProgramHeaderReader, ReplaceProgress
│   ├── Terminal/                   # TerminalSession, TerminalRelay, TypedTransfer
│   └── Reporting/                  # ControllerReply, ResultLine, ExitCode, OutcomeWriter
├── KawasakiVSCode.Cli/
│   ├── KawasakiVSCode.Cli.vbproj   # AssemblyName khi
│   ├── Program.vb                  # Main calls KhiApp.Run
│   ├── KhiApp.vb                   # parse, dispatch, exit code
│   ├── ArgumentParser.vb
│   ├── TermVerb.vb, LoadVerb.vb, SaveVerb.vb
│   └── ConsoleKeySource.vb         # raw keys to bytes; Ctrl+] ends the session
tests/
└── KawasakiVSCode.Tests/
    ├── KawasakiVSCode.Tests.vbproj
    ├── Captures/<id>/              # exchange.txt, note.md, files/
    ├── Fixtures/
    ├── StandIn/                    # ControllerStandIn, CaptureReader, CommandRecorder
    ├── Protocol/                   # INV-1 to INV-18 over real sockets
    ├── Route/                      # through KhiApp.Run and khi.exe
    └── Conformance/                # options, header blocks, XML docs, one type per file
```

**Structure Decision**: These are the three projects of Article I; the folders inside them group files and add no projects. The type names above are a starting map, and the tests decide what actually exists (Article XI). The capture relay is a PowerShell script, not a fourth project, so Article I needs no justification for it.

## Complexity Tracking

| Addition | Why needed | Simpler alternative rejected because |
|----------|------------|--------------------------------------|
| `MSTest.TestFramework`, `MSTest.TestAdapter`, `Microsoft.NET.Test.Sdk` (Article XV) | .NET has no built-in test runner. These are Microsoft's, and the SDK's VB `mstest` template is built on them. | The `MSTest` meta-package also brings code-coverage and telemetry extensions, which this project does not need. A hand-made runner would be a fourth project and its own source of bugs. |
| Conformance tests for options, header blocks, XML docs and one type per file | The gates require these, and the VB compiler checks only the options. | Review by eye misses what a test catches every run. |
