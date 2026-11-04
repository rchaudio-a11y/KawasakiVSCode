# KawasakiVSCode — `khi`

**A command-line tool that does a Kawasaki robot terminal's three jobs from VS Code: an interactive terminal to the controller, LOAD a program into it, and SAVE the controller's data back to a file.**

`khi` lets you keep Kawasaki AS robot programs in git and work on them in VS Code. You edit a program there, press a key to load it into the controller, and save the controller's data back into the repository, so a program's history is the branch's history.

> **Status: in development.** Feature 001 is being built. The foundation is done and tested: the connection, the telnet negotiation and login, the robots file, and the commands. `khi load`, `khi save` and `khi term` are **not usable yet**; they are the next stories. See [Status](#status).

> **Not affiliated with Kawasaki.** This is an independent tool by RCH Automation LLC. "Kawasaki", "KRterm", "K-ROSET" and "KIDE" are named only to say what this works with.

---

## Why

Kawasaki's own tools do these jobs well, but outside the editor where the code lives:

- **KRterm** is the terminal. It runs LOAD and SAVE against its own folder, so saved files collect wherever it was installed.
- **KIDE** is a good editor, with its own project folders.

AS programs are plain text, so they belong in git with everything else. What's missing is a way to reach the robot from VS Code without leaving it. Editing is already covered by existing VS Code extensions for the AS language. `khi` adds the part they can't do: the transfer.

## What it does

```text
khi term [--robot <name>]
khi load <file> [--robot <name>]
khi save <file> [--qualifier <q>] [--robot <name>]
```

| Verb | What it does |
|---|---|
| `khi term` | An interactive terminal to the controller's AS prompt, in VS Code's integrated terminal. A SAVE or LOAD you type there works as it does in KRterm, with files relative to the folder you opened it in. **Ctrl+]** ends the session. |
| `khi load <file>` | Sends a program file to the controller. The controller's reply is printed word for word. An error is printed with a reference to the file, so it lands in VS Code's Problems panel. |
| `khi save <file>` | Saves controller data into `<file>`: all data with `--qualifier FULL`, programs only with `--qualifier P`, or the default set with no qualifier. |

Each `load` and `save` ends with one result line saying what was transferred, and an exit code a VS Code task can act on:

| Code | Meaning |
|---|---|
| 0 | Success, or a terminal session you ended |
| 1 | The controller reported an error, a refusal or a question |
| 2 | `khi` refused before connecting (a bad argument, a missing file, an unknown robot) |
| 3 | A wait timed out |
| 4 | The connection failed or was closed |
| 5 | A local file step failed |

### Controllers

Controllers are named in a `khi.robots.json` file in the folder you run `khi` from, normally the program repository's root:

```json
{
  "default": "sim",
  "robots": [
    { "name": "sim",   "address": "127.0.0.1",   "port": 9105, "simulator": true  },
    { "name": "cell1", "address": "192.168.0.2", "port": 23,   "simulator": false }
  ]
}
```

No address, port or credential is ever written into the code.

## How it works

The controller speaks telnet on TCP port 23. A plain telnet client can reach the AS prompt, but it can't complete a LOAD or SAVE, because the PC end of the transfer is the terminal program's job. That PC end is what `khi` builds.

**Nothing about the protocol is guessed.** Every byte `khi` sends or waits for comes from a recorded exchange between KRterm and the K-ROSET simulator, captured through a logging relay. The recordings are checked into the test project (`tests/KawasakiVSCode.Tests/Captures/`), each with a note saying what was done, against which controller and when. The tests replay them through a stand-in controller over real sockets. A behaviour with no recording behind it is not implemented.

The captures also showed where the simulator differs from the manual. For example, the simulator replaces a program of the same name on LOAD, where the manual says it refuses. So behaviour is taken from the recordings, and the manuals are cited alongside them.

## Safety

`khi` moves files. It never commands the robot.

- **It sends only the login and the command you asked for** (LOAD or SAVE). It never issues motion, execution, motor power, error reset or any other command on its own. In `khi term`, the controller receives what you type.
- **It never answers a controller question for you.** If the controller asks something during a LOAD, `khi` shows the question in the controller's words and waits, for as long as it takes, for the answer you type.
- **Ctrl+C is VS Code's copy key and never reaches the robot.**
- **A failed SAVE never damages the file it would replace.** It writes to a temporary file and replaces the target only when the controller reports the transfer complete.
- **Files are moved byte for byte.** `khi` never reformats or rewrites AS source.

**Prove a workflow on K-ROSET before using it on a real controller, and take a full save with KRterm first.** Feature 001 is accepted against the simulator only.

## Status

Feature 001: terminal, LOAD and SAVE from VS Code.

| Part | State |
|---|---|
| Protocol captures against K-ROSET (E controller) | Done: 19 captures plus 3 extras |
| Foundation: connection, telnet negotiation, login, commands, robots file, exit codes, result line | Built and tested |
| `khi load` | Next |
| `khi save` | Planned |
| `khi term` | Planned |
| VS Code tasks template and install instructions | Planned |
| A real controller | After 001 is accepted on the simulator |

The captures are from an **E controller** in K-ROSET. An F controller may behave differently and needs its own captures before `khi` is trusted on it.

## Repository layout

```text
src/
  KawasakiVSCode.Core/     the connection, protocol, transfer, robots file and reporting
  KawasakiVSCode.Cli/      the khi entry point and its verbs
tests/
  KawasakiVSCode.Tests/    tests, the stand-in controller, and the captures they replay
tools/                     the logging relay used to take captures (PowerShell, dev-time only)
specs/                     the feature specification, plan, contracts and task list
.specify/memory/           the project constitution: the rules every change is held to
```

## Building and testing

Requirements: Windows and the .NET 10 SDK.

```powershell
dotnet build KawasakiVSCode.slnx
dotnet test KawasakiVSCode.slnx --settings KawasakiVSCode.runsettings
```

The `.runsettings` file turns off the test platform's telemetry.

## Built with

VB.NET on .NET 10, with `Option Strict On` in every project, no third-party runtime dependencies, and tests written before the code they cover. Every guard is shown failing before it's trusted. The project's rules are in its constitution, `.specify/memory/constitution.md`.

---

*RCH Automation LLC*
