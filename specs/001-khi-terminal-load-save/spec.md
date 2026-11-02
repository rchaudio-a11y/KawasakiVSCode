# Feature Specification: khi — terminal, LOAD and SAVE from VS Code

**Feature Branch**: `001-khi-terminal-load-save`

**Created**: 2026-11-02

**Status**: Draft

**Input**: User description: "KawasakiVSCode 001: khi — terminal, LOAD and SAVE from VS Code." The Architect's Add items follow as given; the Invariants are carried word for word under [Invariants as Tests](#invariants-as-tests). Backticks are added only where Markdown would otherwise drop the text.

> 1. The solution, per Article I, on .NET 10: KawasakiVSCode.Core, KawasakiVSCode.Cli (assembly name khi), KawasakiVSCode.Tests.
> 2. Captures first. Before any protocol code, capture KRterm ↔ K-ROSET for: connect and login; LOAD of a small program; LOAD of a program the controller rejects (a syntax error on a known line); SAVE of programs only; a full SAVE; SAVE and LOAD typed at the prompt during an interactive session. Each capture is checked into KawasakiVSCode.Tests/Captures/ with a note: what was done, against what, which K-ROSET and KRterm versions, the date. Loopback capture needs a loopback adapter; a logging relay (KRterm → relay → K-ROSET) avoids it. Prove one route at plan.
> 3. `khi term [--robot <name>]` — connects, answers the telnet negotiation, logs in, and relays keystrokes and output in the console it runs in (VS Code's integrated terminal). When the controller begins a SAVE or LOAD the person typed, khi plays the PC end against files relative to the current folder, then hands the prompt back. After login it waits on the person without limit. Ctrl+C or the session's exit closes the socket and exits zero; a controller-side close is reported and exits non-zero.
> 4. `khi load <file> [--robot <name>]` — connects, logs in, sends LOAD, streams the file byte for byte, and prints the controller's reply verbatim. Exit 0 only when the controller reports the load complete without error. Where the controller names a line, output carries `<file>:<line>: <controller text>`.
> 5. `khi save <file> [--qualifier <q>] [--robot <name>]` — connects, logs in, sends SAVE with the qualifier, writes to a temporary file beside `<file>`, and replaces `<file>` only after the controller signals the transfer complete. The qualifiers supported are those the manual lists and a capture backs; any other is refused by name.
> 6. Controllers. A robots file at the workspace root, khi.robots.json: named entries of address, port and a simulator flag, plus an optional default. --robot picks one; without it the default is used; with neither, refuse and list the names. An unknown name is refused by name.
> 7. `khi load` and `khi save` each end with one result line: the verb, the robot, the file, the bytes transferred, and the programs where the controller reports them.
> 8. A tasks template, templates/tasks.json, for a program repository's .vscode/: *khi: Load current file* (`${file}`), *khi: Save controller to file* (prompts for the file name and qualifier), *khi: Open terminal*. Load carries a problem matcher for the `<file>:<line>:` form. A README section shows how to publish khi to a folder on the PATH and copy the template.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record the real sessions first (Priority: P1)

The Architect records real KRterm ↔ K-ROSET sessions and checks them in before any protocol code exists. Every later story replays these recordings, so they decide what khi may send and wait for.

**Why this priority**: Article IV leaves a behaviour with no capture behind it unimplemented, so nothing else can start without these.

**Independent Test**: Open `KawasakiVSCode.Tests/Captures/` and confirm that every exchange FR-002 and FR-003 list is present with its note, and that the plan names the capture route it proved.

**Acceptance Scenarios**:

1. **Given** a capture route the plan has proved, **When** the Architect records an exchange listed in FR-002 or FR-003, **Then** the capture is checked in with a note of what was done, against what, the K-ROSET and KRterm versions, and the date.
2. **Given** a checked-in capture, **When** the stand-in replays it over a real socket, **Then** the bytes it plays are the bytes recorded.
3. **Given** a behaviour with no capture behind it, **When** a plan or review reaches it, **Then** it is recorded as not implemented.

---

### User Story 2 - Load a program file into the controller (Priority: P1)

A robot programmer runs `khi load` on a program file and sees, in the controller's own words, whether it loaded and what was transferred.

**Why this priority**: Getting an edited program onto the controller is the main reason to work from VS Code.

**Independent Test**: Replay the LOAD captures against `khi load`. Confirm the stand-in receives the file unchanged, the output carries the controller's text exactly, and the exit code follows the controller's result.

**Acceptance Scenarios**:

1. **Given** a program whose name the controller does not hold, **When** the programmer runs `khi load <file>`, **Then** the file's bytes reach the controller unchanged, the controller's reply prints verbatim, one result line ends the output, and the exit code is zero.
2. **Given** a program the controller rejects, **When** the programmer runs `khi load <file>`, **Then** the controller's text prints exactly, the output carries `<file>:<line>:` where the controller names a line, and the exit code is non-zero.
3. **Given** the controller stops to ask a question during the load, **When** that question arrives, **Then** khi prints it verbatim, sends no answer, closes the session, and exits non-zero.
4. **Given** a program whose name is already in the controller's memory, **When** the programmer runs `khi load <file>`, **Then** the controller's refusal prints verbatim, the exit code is non-zero, and khi sends no command to remove the existing program.
5. **Given** a file that does not exist, **When** the programmer runs `khi load <file>`, **Then** khi refuses by naming the file and does not connect.
6. **Given** a load under way, **When** the connection drops or a wait times out, **Then** khi says the load did not finish and that the controller's memory may have changed, one result line states what was transferred, and the exit code is non-zero.

---

### User Story 3 - Save controller data to a file (Priority: P1)

A robot programmer runs `khi save` to copy programs or everything the controller holds into a file on the PC, and can trust that a failed save never harms the file already there.

**Why this priority**: This is the backup and the way programs get into the repository; a save that can destroy the last good copy is worse than none.

**Independent Test**: Replay the SAVE captures against `khi save`, then replay them cut short. Confirm the target holds the controller's data after a complete transfer and is byte-identical to before after an incomplete one.

**Acceptance Scenarios**:

1. **Given** a supported qualifier, **When** the programmer runs `khi save <file> --qualifier <q>`, **Then** the controller's data lands in `<file>`, one result line ends the output, and the exit code is zero.
2. **Given** `<file>` already exists, **When** the transfer completes, **Then** `<file>` is replaced and no temporary file remains.
3. **Given** `<file>` already exists, **When** the connection drops, a wait times out, or the controller returns an error, **Then** `<file>` is byte-identical to before, no temporary file remains, one result line states what was transferred, and the exit code is non-zero.
4. **Given** a qualifier no capture backs, **When** the programmer names it, **Then** khi refuses it by name and does not connect.
5. **Given** a file named with no extension, or one that already exists, **When** the transfer completes, **Then** khi has written exactly the named file, with no extension added and no `.bas` copy left behind.

---

### User Story 4 - Work at the controller's prompt (Priority: P2)

A robot programmer opens `khi term` and works at the AS prompt as they would in KRterm, including typing SAVE and LOAD against files in the current folder.

**Why this priority**: The two transfer verbs cover the routine work; the terminal covers everything else a person needs to type.

**Independent Test**: Run `khi term` against the interactive capture and against a silent stand-in. Confirm the typed commands are the only commands sent, a typed transfer uses the current folder, and the session stays open while the person is idle.

**Acceptance Scenarios**:

1. **Given** a reachable controller, **When** the person runs `khi term`, **Then** khi answers the telnet negotiation, logs in, shows the prompt, and sends what the person types with no command of its own.
2. **Given** an open session, **When** the person types a SAVE, **Then** khi writes the file relative to the current folder under the same protection as `khi save`, and hands the prompt back.
3. **Given** an open session, **When** the person types a LOAD, **Then** khi supplies the file relative to the current folder, unchanged, and hands the prompt back.
4. **Given** a logged-in session, **When** the person types nothing for longer than every transfer timeout, **Then** the session stays open.
5. **Given** an open session, **When** the person presses Ctrl+C or the session's exit occurs, **Then** khi closes the socket and exits zero.
6. **Given** an open session, **When** the controller closes the connection, **Then** khi reports it and exits non-zero.
7. **Given** an open session, **When** the controller reports an error, **Then** the text reaches the screen unaltered, the session continues, and it does not count as a failure of the tool.
8. **Given** a controller that goes silent before login completes, **When** the wait times out, **Then** khi names the step it was waiting on, closes the session, and exits non-zero.
9. **Given** a typed LOAD the controller interrupts with a question, **When** the person types an answer, **Then** the answer reaches the controller unchanged and the transfer continues.

---

### User Story 5 - Choose the controller by name (Priority: P2)

A robot programmer names the controllers for a workspace once, and every verb picks one by name or uses the workspace's default.

**Why this priority**: Every verb needs a target, and reaching the wrong robot is the mistake this prevents.

**Independent Test**: Run each verb through the `khi` entry point with a named robot, an unknown name, a default, and no default. Confirm the target or refusal in each case without any transfer.

**Acceptance Scenarios**:

1. **Given** a robots file with several entries, **When** the person passes `--robot <name>`, **Then** khi uses that entry.
2. **Given** a robots file with a default and no `--robot`, **When** a verb runs, **Then** khi uses the default.
3. **Given** a robots file with no default and no `--robot`, **When** a verb runs, **Then** khi refuses and lists the names.
4. **Given** a name not in the file, **When** the person passes it, **Then** khi refuses it by name.
5. **Given** a verb or option khi does not have wired, **When** the person uses it, **Then** khi refuses it with a message saying so and exits non-zero.

---

### User Story 6 - Run it all from VS Code (Priority: P3)

A robot programmer copies the tasks template into a program repository and uses three named tasks instead of typing commands, with rejected lines appearing in the Problems list.

**Why this priority**: This is the finished experience, but it adds nothing to what the verbs already do at the command line.

**Independent Test**: Run the acceptance in INV-8 in a scratch repository with K-ROSET running.

**Acceptance Scenarios**:

1. **Given** the README and a program repository, **When** the person follows the README section, **Then** khi is published to a folder on the PATH and the template is in the repository's `.vscode/`.
2. **Given** the template and K-ROSET running, **When** the person runs *khi: Save controller to file* and picks full, **Then** the named file is written.
3. **Given** that file is open and the controller no longer holds its programs, **When** the person runs *khi: Load current file*, **Then** it loads back.
4. **Given** a load the controller rejects naming a line, **When** the task ends, **Then** the Problems list shows the line.
5. **Given** the template, **When** the person runs *khi: Open terminal* and types a SAVE, **Then** the file is written into the repository's folder.

---

### Edge Cases

- **Re-loading an edited program**: Both AS manuals say LOAD refuses a program name already in memory and aborts, and that programs are not overwritten. So *Load current file* fails on any program the controller already holds, until the person removes it by typing at *Open terminal*. khi never removes it; Article III forbids that.
- **A rejected step may be a question, not an error**: Both manuals say an incorrect step produces "The step format is incorrect (0: Continue load 1: Delete program and exit)". `khi load` cannot answer it (FR-021). The rejected-LOAD capture's note records what the controller's memory holds after the session closes at that question.
- **The controller names no line**: Neither manual shows a line number for a rejected step. If the rejected-LOAD capture shows none, output stays verbatim without `<file>:<line>:`, the problem matcher has nothing to match, and the plan raises this with the Architect before going further.
- **The line named is not a file line**: If the controller names a step within a program rather than a line of the file, Article II forbids translating it by reading the file. The plan raises this with the Architect too.
- **A LOAD interrupted partway**: The manuals warn that original data is deleted if LOAD is canceled while overwriting. Article VI protects only the PC side, so after any failed or interrupted load khi says the load did not finish and that the controller's memory may have changed.
- **Ctrl+C during `khi load` or `khi save`**: Treated as a failure. The result line says what was transferred, a save's target is byte-identical to before with no temporary file left, and the exit code is non-zero.
- **Ctrl+C during a transfer typed in `khi term`**: The session ends and khi exits zero, because the person ended it. A typed SAVE's target stays byte-identical to before with no temporary file left; a typed LOAD carries the warning above.
- **Ctrl+C at the prompt**: khi closes the session, so AS's own Ctrl+C (cancel the input line, leave a `^C:Exit` dialog) is not available in `khi term`.
- **A question during a typed transfer**: Keystrokes keep being relayed so the person can answer. The wait stays bounded (Article X), so the plan sizes the transfer timeout with this in mind.
- **A typed transfer stalls**: The wait times out, khi names the step, closes the session, and exits non-zero. The session does not return to the prompt, because Article X leaves it closed.
- **A typed LOAD names a missing file**: khi answers the controller as the capture shows KRterm doing, then hands the prompt back.
- **A typed SAVE khi cannot write**: khi reports it, closes the session, and exits non-zero, unless a capture shows a recorded way to decline.
- **The target cannot be replaced at the end** (another program holds it open): khi reports it, the target is byte-identical to before, no temporary file remains, and the exit code is non-zero.
- **The target folder does not exist or cannot be written**: `khi save` refuses by naming the path and does not connect.
- **The process is killed outright**: The temporary file may remain, but the target is untouched. A later save still works.
- **The controller is unreachable or refuses the connection**: khi reports the robot's name and address and exits non-zero.
- **Another terminal already holds the controller**: Whatever the controller does is reported or times out; khi does not retry.
- **Robots file missing or unreadable, or a default naming no entry**: khi refuses, naming the path or the name, and does not connect.
- **An empty file given to `khi load`**: It is sent like any other. The controller's reply decides the result (Article II).

## Requirements *(mandatory)*

### Functional Requirements

**Solution**

- **FR-001**: The solution MUST consist of KawasakiVSCode.Core, KawasakiVSCode.Cli (assembly name `khi`) and KawasakiVSCode.Tests, per Article I, on .NET 10, per Article XV.

**Captures**

- **FR-002**: Before any protocol code exists, a real KRterm ↔ K-ROSET session MUST be captured for each of: (a) connect and login; (b) LOAD of a small program; (c) LOAD of a program the controller rejects, with a syntax error on a known line; (d) SAVE of programs only (`SAVE/P`); (e) a full SAVE (`SAVE/FULL`); (f) SAVE and LOAD typed at the prompt during an interactive session.
- **FR-003**: Four more captures MUST be taken, because requirements below cannot pass Article IV without them: (g) SAVE with no qualifier, for FR-025; (h) a SAVE the controller refuses, for INV-4's controller error; (i) LOAD of a program name already in memory, for User Story 2 and INV-8; (j) a typed LOAD naming a file that does not exist, for the edge case above. If K-ROSET cannot be made to produce one, the behaviour it backs is recorded as not implemented and reported to the Architect.
- **FR-004**: Each capture MUST be checked into `KawasakiVSCode.Tests/Captures/` with a note: what was done, against what, which K-ROSET and KRterm versions, and the date. A SAVE capture MUST also keep the file KRterm wrote, so replay can compare bytes. The rejected-LOAD capture's note MUST record what the controller's memory holds after the session closes at the controller's question.
- **FR-005**: The plan MUST prove one capture route: a logging relay (KRterm → relay → K-ROSET) or a loopback adapter.
- **FR-006**: Every command, qualifier and controller message khi relies on MUST cite the manual number and section beside the capture that proves it (Article XVI).

**Controllers**

- **FR-007**: khi MUST read controllers from `khi.robots.json` at the workspace root: named entries of address, port and a simulator flag, plus an optional default.
- **FR-008**: `--robot` MUST pick an entry. Without it the default MUST be used. With neither, khi MUST refuse and list the names. An unknown name MUST be refused by name.
- **FR-009**: No address, port or credential may appear in code. The login name `as` MUST live in one named constant in KawasakiVSCode.Core, citing the manual section that gives it (Article IX).

**Terminal**

- **FR-010**: `khi term [--robot <name>]` MUST connect, answer the telnet negotiation, log in, and relay keystrokes and output in the console it runs in (VS Code's integrated terminal).
- **FR-011**: In `khi term` the controller's output MUST reach the screen unaltered. An error the controller reports there is the person's to read; it MUST NOT end the session or count as a failure of the tool.
- **FR-012**: When the controller begins a SAVE or LOAD the person typed, khi MUST play the PC end against files relative to the current folder, then hand the prompt back. khi MUST recognise this from the controller's recorded transfer framing, not by reading what the person typed.
- **FR-013**: A typed SAVE MUST follow the same temporary-file and replacement rule as `khi save`. A typed LOAD MUST send the file's bytes unchanged. Keystrokes MUST keep being relayed during a typed transfer.
- **FR-014**: After login, `khi term` MUST wait on the person without limit. Ctrl+C or the session's exit MUST close the socket and exit zero. A controller-side close MUST be reported and exit non-zero.
- **FR-015**: Every wait during connect, login and a typed transfer MUST have a timeout. A timeout MUST name the step that was waiting, leave the session closed, and exit non-zero.

**Load**

- **FR-016**: `khi load <file> [--robot <name>]` MUST connect, log in, send LOAD, stream the file byte for byte, and print the controller's reply verbatim.
- **FR-017**: `khi load` MUST exit zero only when the controller reports the load complete without error.
- **FR-018**: Where the controller names a line, the output MUST carry `<file>:<line>: <controller text>`. khi MUST NOT work out a line by reading the file (Article II).
- **FR-019**: A file that does not exist MUST be refused by name before connecting.
- **FR-020**: After a failed or interrupted load, khi MUST say the load did not finish and that the controller's memory may have changed.
- **FR-021**: When the controller asks a question during `khi load`, khi MUST print it verbatim, send no answer, close the session, and exit non-zero. Article III gives `khi load` no answer to send: a reply is neither a login, the verb's command, framing nor file content.

**Save**

- **FR-022**: `khi save <file> [--qualifier <q>] [--robot <name>]` MUST connect, log in, send SAVE with the qualifier, write to a temporary file beside `<file>`, and replace `<file>` only after the controller signals the transfer complete.
- **FR-023**: On any failure (timeout, disconnect, controller error) the existing file MUST be byte-identical to before and the temporary file removed (Article VI).
- **FR-024**: The qualifiers supported MUST be those the manual lists and a capture backs. Any other MUST be refused by name before connecting, including those the manual lists with no capture: `L`, `R`, `S`, `A`, `SYS`, `ROB`, `ELOG`, `OPLOG`, `ALLLOG`, `STG`.
- **FR-025**: With the captures above, the supported forms MUST be `--qualifier P` (`SAVE/P`), `--qualifier FULL` (`SAVE/FULL`), and no qualifier (`SAVE`).
- **FR-026**: khi MUST write exactly the file the person named: no extension added and no `.bas` backup of the old file. A typed SAVE writes the file the controller announces, under the same protection.
- **FR-027**: The LOAD or SAVE command khi sends MUST carry the file's bare name, not its path. The path stays on the PC.

**Result line**

- **FR-028**: `khi load` and `khi save` MUST each end with one result line, on success and on failure: the verb, the robot, the file, the bytes transferred, and the programs where the controller reports them. Bytes count file content only, not protocol traffic.

**Template and README**

- **FR-029**: `templates/tasks.json` MUST provide, for a program repository's `.vscode/`: *khi: Load current file* (`${file}`), *khi: Save controller to file* (prompts for the file name and qualifier), and *khi: Open terminal*.
- **FR-030**: The load task MUST carry a problem matcher for the `<file>:<line>:` form. The save task's qualifier prompt MUST offer only supported qualifiers.
- **FR-031**: A README section MUST show how to publish khi to a folder on the PATH and copy the template.

**Refusals**

- **FR-032**: An unwired verb or option MUST be refused with a message saying so. No placeholder value may be presented as real.
- **FR-033**: Every refusal and error MUST be shown reaching the exit code and the output through the `khi` entry point (Article XIII).

### Invariants as Tests

These are the Architect's, word for word.

- **INV-1 — Not an editor.** The bytes khi load sends for a file's content equal the file on disk. Gate: replay the LOAD capture with a fixture that has trailing spaces, tabs and mixed line endings; the stand-in receives them unchanged. Fire: trim trailing whitespace before sending.
- **INV-2 — Not a robot runner.** Across every verb and every test, the controller commands khi issues of its own are only the login and the running verb's LOAD or SAVE command; in khi term, the only commands are the ones the test typed. Gate: the stand-in records every command received and the suite compares the union against that set; file content is covered by *Not an editor*. Fire: send ERESET before LOAD.
- **INV-3 — Not a guesser.** Every byte sequence khi originates or waits for — negotiation, login, command, transfer framing — is asserted by a test that names the capture it replays; a protocol test with no capture fails the gate. File content and typed input are not protocol. Fire: add a test with a hand-written expected reply.
- **INV-4 — Not destructive on the PC side.** The stand-in drops the connection halfway through a SAVE; the target file is byte-identical to before and no temporary file remains. Repeat with a timeout and with a controller error. Fire: write straight to the target.
- **INV-5 — No transfer hangs.** The stand-in goes silent during connect, during login, and mid-transfer; khi load and khi save exit non-zero within their timeout, naming the step they were waiting on, and khi term does the same before login. Fire: remove the timeout on one read.
- **INV-6 — The terminal waits on the person.** After login, a silent stand-in leaves khi term open past every transfer timeout. When the stand-in closes the connection, khi term reports it and exits non-zero; when the test ends the session, it exits zero. Fire: apply the transfer timeout to the relay.
- **INV-7 — The controller's words, verbatim.** Replaying the rejected-LOAD capture, the output contains the controller's text exactly, carries `<file>:<line>:` for the line it names, and the exit code is non-zero.
- **INV-8 — Acceptance against K-ROSET, from VS Code.** With the template copied into a scratch repository and K-ROSET running: *Save controller to file* (full) writes the file; *Load current file* loads it back; a second full save equals the first except for lines the controller stamps, which the plan lists. *Open terminal* reaches the prompt, and SAVE typed there writes into the scratch repository's folder.
- **INV-9 — The constitution.** The constitution in `.specify/memory/constitution.md` is v1.0.0 as corrected at merge, and every review gate it lists is shown passing.

**Added by this spec, because the articles require them and the list above does not reach them:**

- **INV-7 Fire** (Article VII requires one): reword the controller's text before printing it.
- **INV-8 note**: Both manuals say LOAD refuses a program name already in memory, so before *Load current file* the person removes the saved programs by typing DELETE at *Open terminal*. Capture (i) confirms this. Article III keeps khi from doing it.
- **INV-10 — Typed transfers are bounded and safe.** The stand-in goes silent, and separately drops the connection, halfway through a SAVE typed in `khi term`. khi term reports it and exits non-zero, the target is byte-identical to before, and no temporary file remains. Fire: apply the terminal's unlimited wait to the typed transfer.
- **INV-11 — No answers on the person's behalf.** Replaying the rejected-LOAD capture up to the controller's question, `khi load` prints the question, sends nothing further, closes the session, and exits non-zero. Fire: send `1` at the question.
- **INV-12 — The path stays on the PC.** Given a file named by full path, the command the stand-in records carries only the bare name. Fire: send the full path.

### Manual Facts This Spec Relies On

Each was read in both AS Language Reference Manuals, 90209-1022 (E) and 90209-1025 (F), at the same section number. The citations in the code still sit beside the capture that proves them (FR-006).

| What | Section |
|------|---------|
| Enter `as` at the `login :` message; the `>` prompt follows | 2.6.1 |
| Ctrl+C cancels the last input line, among the other terminal control keys | 2.4.1 |
| SAVE and LOAD use the PC connection; `SAVE file.pg`, `LOAD data01.as` | 2.6.2 |
| A KRterm session ends from its [Com] [Disconnect] menu; the manuals list no logout command | 2.6.3 |
| SAVE qualifiers; `/FULL` is all data that can be saved; bare `SAVE` stores five data types in one file | 5.3 |
| Extensions added when none is given (`.as`, `.PG`, and so on); the old file is renamed to `.bas` | 5.3 |
| LOAD refuses a program name already in memory and aborts; variables are overwritten without warning | 5.3 |
| "The step format is incorrect (0: Continue load 1: Delete program and exit)" | 5.3 |
| Original data is deleted if LOAD is canceled while overwriting | 5.3 |
| `Loading...` and `Loading done.` as the load's progress and completion | 5.3 |
| DELETE removes programs and variables from memory | 5.2 |
| `P0108 Syntax error.`, `P1033 Program name already exists.`, `P2009 Illegal file name.` | Appendix 4 |

### Key Entities *(include if feature involves data)*

- **Robot entry**: One named controller: name, address, port, simulator flag. The name is how every verb and message refers to it.
- **Robots file**: The workspace's list of robot entries and its optional default.
- **Capture**: A recorded exchange with a real terminal session, with its note and any file KRterm wrote. Every protocol test names the capture it replays.
- **Stand-in**: The in-process controller replacement that replays captures byte for byte over a real socket and records what it receives.
- **Result line**: The single closing line of `khi load` and `khi save`: verb, robot, file, bytes, programs.
- **Temporary save file**: The file a save writes beside its target until the controller signals the transfer complete.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From the editor alone, a person completes all three tasks against the simulator (save everything, load it back, reach the prompt and save from there) with no other terminal program.
- **SC-002**: In 100% of load tests, the bytes the controller side receives for a file's content equal the file on disk, including trailing spaces, tabs and mixed line endings.
- **SC-003**: In 100% of interrupted saves (dropped connection, timeout, controller error, at the verb and typed in the terminal), the existing file is byte-identical to before and no temporary file remains.
- **SC-004**: In 100% of silence cases during connect, login and transfer, the tool ends non-zero within its stated timeout and names the step it was waiting on.
- **SC-005**: A logged-in terminal session stays open while silent for longer than every transfer timeout.
- **SC-006**: Across the whole test suite, zero controller commands fall outside the login, the running verb's own command, and what the test typed.
- **SC-007**: 100% of the byte sequences the tool originates or waits for trace to a named recording, and 100% of the commands, qualifiers and controller messages it relies on cite a manual number and section.
- **SC-008**: A controller rejection appears in the output exactly as the controller wrote it, with a non-zero exit, and where a line is named the person reaches that line from the editor's problem list in one click.
- **SC-009**: A second full save equals the first in every line except the stamped lines the plan lists.
- **SC-010**: Following only the README, a person publishes the tool and installs the template in a new program repository.
- **SC-011**: All nine of the constitution's review gates are shown passing, and every guard carries its fire demonstration and a test through the tool's entry point.

## Assumptions

- **"Full" means `SAVE/FULL`, "programs only" means `SAVE/P`.** The manual's `/FULL` is "all data that can be saved"; bare `SAVE` is a different, smaller set.
- **"The session's exit" means the console closing or its input ending.** The manuals list no logout command for the prompt, so there is no controller command for this.
- **`khi.robots.json` is read from the current folder.** A command-line tool cannot see VS Code's workspace root; the template's tasks run there, and a person who changes folder gets the missing-file refusal.
- **The simulator flag changes no behaviour in this feature.** It is read and checked for its type because Article IX requires the field, and nothing yet acts on it.
- **Timeouts apply to each wait, not to a whole transfer,** so a long transfer that keeps moving does not time out. The plan sets each value from the timing seen in the captures. There is no person-facing timeout option.
- **Proof comes from K-ROSET.** Real-controller behaviour is not proven by this feature. Article IV allows a controller capture to be added later.
- **The tools are on this PC.** K-ROSET 1.8.6.19269 and KRterm 1.2.8 are installed; neither was started for this spec, and whether K-ROSET needs a licence to run was not checked. No loopback adapter or Npcap is installed, which favours the relay route. Any relay stays within Article XV, and within Article I unless the plan justifies a new project.
- **Windows only,** because K-ROSET and KRterm are Windows programs.
- **Keys VS Code keeps for itself never reach khi,** so they cannot be relayed. The plan checks the keys in manual section 2.4.1 against this.
- **Out of scope:** `/SEL` and choosing programs by name, `LOAD/Q` at `khi load` (its questions need a person; it can be typed in `khi term`), the `USB_` commands, and removing programs from the controller on the person's behalf.
