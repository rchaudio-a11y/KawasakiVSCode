# Data Model: khi — terminal, LOAD and SAVE from VS Code

Phase 1 for [plan.md](plan.md). These are the concepts khi works with, their rules, and how a session moves between states. There is one canonical model per concept (Article XI). File formats are in [contracts/](contracts/).

## Robots

### RobotEntry

One named controller from `khi.robots.json`.

| Field | Type | Rule |
|-------|------|------|
| Name | text | Required and non-empty; unique within the file, ignoring case; it is how every message names the robot |
| Address | text | Required: a host name or IP address |
| Port | whole number | Required, 1 to 65535 |
| Simulator | true or false | Required; read and type-checked, but no behaviour depends on it in this feature |

### RobotsFile

The workspace's controllers: a list of `RobotEntry` and an optional `Default` name. `Default`, when present, must name an entry. A file that is missing, unreadable, malformed or breaking a rule is refused, naming the path and the rule, before any connection.

### RobotSelection

The one door for which robot a verb uses (Article XII). `--robot <name>` picks that entry. With no `--robot`, the default is used. With neither, khi refuses and lists the names. An unknown name is refused by that name.

## Commands

### SaveQualifier

| Value | Sent as | Capture |
|-------|---------|---------|
| none | `SAVE <name>` | (g) |
| `P` | `SAVE/P <name>` | (d) |
| `FULL` | `SAVE/FULL <name>` | (e) |

`SaveQualifier.Parse` is the one door. A qualifier the manual lists but no capture backs (`L`, `R`, `S`, `A`, `SYS`, `ROB`, `ELOG`, `OPLOG`, `ALLLOG`, `STG`) is refused by name as not supported. Anything else is refused by name as unknown.

### ControllerCommand

The only commands khi originates (Article III). Each has a fixed text form taken from its capture.

| Kind | Sent when | Form |
|------|-----------|------|
| Login | Every verb, at the login prompt | the constant `as` (manual 2.6.1) |
| Load | `khi load` | `load <bare name>`, cased as captured |
| Save | `khi save` | `SAVE[/<qualifier>] <bare name>` |

Every command is sent as the line, then, after its echo, `18`, the line again and `0d 0a`, as each KRterm LOAD and SAVE capture shows (tasks.md D1).

Typed input in `khi term`, and the person's answer to a question during `khi load`, are not `ControllerCommand`s. They are content passed through unchanged (Article III, v3.0.0).

## Files

### SaveTarget

The file a save writes.

| Field | Rule |
|-------|------|
| TargetPath | Exactly the path the person named, or for a typed SAVE the name the controller announces, resolved against the current folder. No extension is added. |
| TempPath | In the same folder: `.<name>.<8 random hex>.khi-tmp` |

`Created` → `Writing` → `Committed` (the temporary file is renamed over the target) or `Abandoned` (the temporary file is deleted). The target changes only on `Committed`. If the folder is missing or cannot be written, khi refuses before connecting.

### SaveLineRule

The one door for whether a line the controller prints outside the file data during a SAVE is normal (FR-040). The normal lines are the ones captures (d), (e), (f), (g) and (h) show on a good SAVE, listed in research R17. Any other line is kept verbatim as an `UnknownSaveLine`, and one or more of them fails the save.

## Session

### Timeouts and WaitStep

`Timeouts` holds one value per `WaitStep`. A timeout message names the step.

| WaitStep | Named in messages as |
|----------|----------------------|
| Connect | connecting to `<address>:<port>` |
| Negotiation | telnet negotiation |
| LoginPrompt | the login prompt |
| LoginReply | the prompt after login |
| CommandReply | the reply to `<command>` |
| TransferData | transfer data |
| TransferEnd | the end of the transfer |
| TypedTransfer | a typed `<SAVE or LOAD>` |

Values come from the captures (research R9). `TerminalRelay` after login, and the wait for the person's answer to a question during `khi load` or during a transfer typed in `khi term`, are the only waits with no timeout (Article X, v3.1.0).

### Session states

```text
Connecting → Negotiating → AwaitingLoginPrompt → LoggingIn → Ready
Ready → (verb states below) → Closed
Any waiting state → TimedOut (exit 3) | ConnectionLost (exit 4)
```

**Load**:

```text
Ready → SendingLoad → Streaming → AwaitingCompletion → Done (exit 0)
                                                     ├ controller error → Failed (exit 1)
                                                     └ question → AwaitingAnswer (no time limit)
Streaming → question with file data left → no more data; what capture (p) shows is sent → AwaitingAnswer
Any wait after an unrecognised line → quiet for its timeout, no `>` prompt → AwaitingAnswer (not TimedOut)
Any wait after an unrecognised line → the `>` prompt → Failed (a refusal; exit 1)
AwaitingAnswer → the person's answer sent as typed → AwaitingCompletion
AwaitingAnswer → Ctrl+C → discarded; still AwaitingAnswer
AwaitingAnswer → console closed or input ended → Closed (exit 1; the controller may still be waiting)
```

**Save**:

```text
Ready → SendingSave → Receiving (into SaveTarget.TempPath) → TransferComplete
Receiving → an UnknownSaveLine is kept, and the save goes on to the end of the transfer
TransferComplete → Committed (exit 0) | ReplaceFailed (exit 5; target untouched)
TransferComplete with any UnknownSaveLine → Failed (exit 1; target untouched; each line printed verbatim)
Any failure before Committed → Abandoned (temporary file deleted)
```

**Terminal**:

```text
Ready → Relaying ⇄ TypedTransfer(SAVE or LOAD) → Relaying
Relaying → Ended (Ctrl+], console closed, input ended; exit 0)
         → ConnectionLost (controller closed; exit 4)
TypedTransfer → TimedOut (session closed; exit 3)
```

## Reporting

### ControllerReply

Text the controller sent, kept byte for byte. A reply can carry a line reference only in the form capture (c) shows. Nothing reads a line number out of the file (Article II), and a step number is never turned into a line.

### TransferOutcome and ResultLine

Every run of `khi load` and `khi save` ends with one result line built from this outcome. The format is in [contracts/cli.md](contracts/cli.md).

| Field | Rule |
|-------|------|
| Verb | `load` or `save` |
| Status | `ok` or `failed` |
| Robot | the `RobotEntry` name |
| File | the path as the person gave it |
| Bytes | file content only, sent or written; never protocol traffic |
| Programs | names the controller reported, in its order; empty when it reported none |
| SaveLines | `khi save` and a typed SAVE: each line outside the file data, with `SaveLineRule`'s verdict on it, normal or unknown |
| QuestionAnswer | `khi load` only, after a question: the controller's error count, the answer the person typed, the controller's label for that answer as its question printed it, and whether the controller's copy differs from the file (after `0`) |
| ExitCode | see the exit codes in [contracts/cli.md](contracts/cli.md) |

## Captures

These live in the test project. The format is in [contracts/capture-format.md](contracts/capture-format.md).

| Concept | What it is |
|---------|------------|
| Capture | One recorded session: an ID from (a) to (q), with a suffix for an extra such as `c-load-rejected-ctrl-c`, an exchange log, a note, and the files KRterm wrote or read |
| CaptureRecord | One chunk of bytes: its direction (`C` toward the controller, `S` from it), the time since the start, and the bytes |
| PayloadRegion | A run of `C` or `S` records that carries file content, marked in the note so the stand-in can compare it with a fixture instead of the captured file |
| TypedRegion | A run of `C` records that the person typed, marked so a test can supply its own input |
