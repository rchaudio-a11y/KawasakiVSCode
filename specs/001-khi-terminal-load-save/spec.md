# Feature Specification: khi — terminal, LOAD and SAVE from VS Code

**Feature Branch**: `001-khi-terminal-load-save`

**Created**: 2026-11-02

**Status**: Draft

**Input**: User description: "KawasakiVSCode 001: khi — terminal, LOAD and SAVE from VS Code." The Architect's Add items follow as given, and the [Clarifications](#clarifications) override them where they differ; the Invariants are carried under [Invariants as Tests](#invariants-as-tests). Backticks are added only where Markdown would otherwise drop the text.

> 1. The solution, per Article I, on .NET 10: KawasakiVSCode.Core, KawasakiVSCode.Cli (assembly name khi), KawasakiVSCode.Tests.
> 2. Captures first. Before any protocol code, capture KRterm ↔ K-ROSET for: connect and login; LOAD of a small program; LOAD of a program the controller rejects (a syntax error on a known line); SAVE of programs only; a full SAVE; SAVE and LOAD typed at the prompt during an interactive session. Each capture is checked into KawasakiVSCode.Tests/Captures/ with a note: what was done, against what, which K-ROSET and KRterm versions, the date. Loopback capture needs a loopback adapter; a logging relay (KRterm → relay → K-ROSET) avoids it. Prove one route at plan.
> 3. `khi term [--robot <name>]` — connects, answers the telnet negotiation, logs in, and relays keystrokes and output in the console it runs in (VS Code's integrated terminal). When the controller begins a SAVE or LOAD the person typed, khi plays the PC end against files relative to the current folder, then hands the prompt back. After login it waits on the person without limit. Ctrl+C or the session's exit closes the socket and exits zero; a controller-side close is reported and exits non-zero.
> 4. `khi load <file> [--robot <name>]` — connects, logs in, sends LOAD, streams the file byte for byte, and prints the controller's reply verbatim. Exit 0 only when the controller reports the load complete without error. Where the controller names a line, output carries `<file>:<line>: <controller text>`.
> 5. `khi save <file> [--qualifier <q>] [--robot <name>]` — connects, logs in, sends SAVE with the qualifier, writes to a temporary file beside `<file>`, and replaces `<file>` only after the controller signals the transfer complete. The qualifiers supported are those the manual lists and a capture backs; any other is refused by name.
> 6. Controllers. A robots file at the workspace root, khi.robots.json: named entries of address, port and a simulator flag, plus an optional default. --robot picks one; without it the default is used; with neither, refuse and list the names. An unknown name is refused by name.
> 7. `khi load` and `khi save` each end with one result line: the verb, the robot, the file, the bytes transferred, and the programs where the controller reports them.
> 8. A tasks template, templates/tasks.json, for a program repository's .vscode/: *khi: Load current file* (`${file}`), *khi: Save controller to file* (prompts for the file name and qualifier), *khi: Open terminal*. Load carries a problem matcher for the `<file>:<line>:` form. A README section shows how to publish khi to a folder on the PATH and copy the template.

## Clarifications

### Session 2026-11-02

- Q: How does a person reload an edited program the controller already holds? → A: With `khi load --replace`, in this feature. The person chooses it; khi reads the program names from the file's `.PROGRAM` header lines (Article II, v2.0.0) and replaces those programs with DELETE/P for each name, then LOAD (Article III, v2.0.0). Without `--replace`, a name already in memory fails with the controller's words. *Superseded on 2026-11-04: `--replace` is dropped from this feature.*
- Q: What does `khi load` do when a rejected LOAD stops at a question? → A: It prints the question, answers nothing, closes the session and exits non-zero. The capture note records what stays in memory. *Superseded on 2026-11-04: the question is handed to the person (constitution v3.0.0).*
- Q: What does the output carry when the controller names no line? → A: The output names the file and the problem matcher points at the file. No line is ever invented or translated from a step number. The plan brings the capture.
- Q: Who gets Ctrl+C in `khi term`? → A: The controller; khi relays it, and it cancels the input line at the AS prompt. The session ends on Ctrl+], or when the console closes or its input ends. *Superseded on 2026-11-04: khi never relays Ctrl+C (Session 2026-11-04).*

### Session 2026-11-03

- Q: Under `--replace`, what happens when a program is not in memory, when the controller asks to confirm a DELETE/P, and when it asks anything else? → A: A P1038 reply is not a refusal; that program simply loads. A DELETE/P confirmation is confirmed for that program only, as the capture shows. Any other question stops before LOAD, and the output names every program already deleted (Article III, v2.0.0). *Superseded on 2026-11-04 with `--replace`.*
- Q: How is the stop on an unexpected reply tested without guessing a question? → A: Under `--replace`, any reply to DELETE/P that is not one of the captured replies (present, absent/P1038, refused, and the confirmation if one is captured) stops before LOAD, and the output names every program already deleted. The test replays the DELETE/P capture with the reply altered and checks that no LOAD is sent. *Superseded on 2026-11-04 with `--replace`.*

### Session 2026-11-04

The Architect's rulings on the captures.

- Q: Does this feature keep `khi load --replace`? → A: No. On K-ROSET a plain LOAD replaces a program already in memory, without a word (captures (i) and `i-load-existing-changed`), so `--replace` is dropped from this feature. The constitution stays as it is. The difference from both AS manuals, which say LOAD refuses such a name, goes on the live-controller check (Assumptions).
- Q: What does `khi load` do when the controller asks a question during the load? → A: The question is handed to the person: khi prints it, sends the one answer the person types, and reads on. khi never answers it itself. Constitution v3.0.0 (2026-11-04) amends Article III for this, and Article X leaves the wait for the answer without limit. The question is answered by typing the answer; Ctrl+C is not an answer (the last ruling below). Captures (n) and (o) record the question answered with `0` and with `1`.
- Q: How does khi tell a failed SAVE from a good one? → A: A SAVE fails if the controller prints any line that is neither file data nor a captured normal line (research R17). Those lines are reported verbatim. SAVE's completion line carries no error count (capture (h)), so these lines are the only sign.
- Q: Is a SAVE onto a file name that already exists captured? → A: Yes, as capture (m).
- Q: Are the large capture logs, 1.58 MB for (e) and 1.32 MB for (g), kept as they are? → A: Yes.
- Q: Does khi emulate KRterm's keyboard shortcuts, and what happens to Ctrl+C? → A: khi does not emulate KRterm's shortcuts (Alt+V paste and the like); copy and paste are VS Code's. Ctrl+C is copy and ends at the terminal: khi never relays Ctrl+C to the controller, not at the prompt, not at a load question, not during a transfer. When VS Code passes the keystroke through because nothing is selected, khi discards it; it is not relayed and it is not a quit. Ctrl+] ends the session, and a load question is answered by typing the answer. This reverses the earlier relaying of Ctrl+C in FR-014 and at the load question. Capture `c-load-rejected-ctrl-c` is kept as a record only.
- Q: What does the result line say after a load question? → A: The exit is non-zero either way, and the answer is named in the controller's words: for `0`, completed with 1 error, answered 0 with its label, and the controller's copy differs from the file; for `1`, completed with 1 error, answered 1 with its label (FR-021, contracts/cli.md). The Architect gave the labels as the manual's, "Continue load" and "Delete program and exit"; K-ROSET's question prints "Change to comment and continue" and "Delete program and abort" (captures (n) and (o)), so the labels are taken from the controller's own question, as the Architect then confirmed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record the real sessions first (Priority: P1)

The Architect records real KRterm ↔ K-ROSET sessions and checks them in before any protocol code exists. Every later story replays these recordings, so they decide what khi may send and wait for.

**Why this priority**: Article IV leaves a behaviour with no capture behind it unimplemented, so nothing else can start without these.

**Independent Test**: Open `KawasakiVSCode.Tests/Captures/` and confirm that every exchange FR-002, FR-003 and FR-036 list is present with its note, and that the plan names the capture route it proved.

**Acceptance Scenarios**:

1. **Given** a capture route the plan has proved, **When** the Architect records an exchange listed in FR-002, FR-003 or FR-036, **Then** the capture is checked in with a note of what was done, against what, the K-ROSET and KRterm versions, and the date.
2. **Given** a checked-in capture, **When** the stand-in replays it over a real socket, **Then** the bytes it plays are the bytes recorded.
3. **Given** a behaviour with no capture behind it, **When** a plan or review reaches it, **Then** it is recorded as not implemented.

---

### User Story 2 - Load a program file into the controller (Priority: P1)

A robot programmer runs `khi load` on a program file and sees, in the controller's own words, whether it loaded and what was transferred.

**Why this priority**: Getting an edited program onto the controller is the main reason to work from VS Code.

**Independent Test**: Replay the LOAD captures against `khi load`. Confirm the stand-in receives the file unchanged, the output carries the controller's text exactly, and the exit code follows the controller's result.

**Acceptance Scenarios**:

1. **Given** a program whose name the controller does not hold, **When** the programmer runs `khi load <file>`, **Then** the file's bytes reach the controller unchanged, the controller's reply prints verbatim, one result line ends the output, and the exit code is zero.
2. **Given** a program the controller rejects, **When** the programmer runs `khi load <file>`, **Then** the controller's text prints exactly, the output carries `<file>:<line>:` where the controller names a line and `<file>:` where it names none, and the exit code is non-zero.
3. **Given** the controller stops to ask a question during the load, **When** that question arrives, **Then** khi prints it verbatim, waits without limit, sends the one answer the person types and nothing of its own, and reads on; a Ctrl+C that reaches khi meanwhile is discarded; the exit code follows the controller's result (FR-017).
4. **Given** an edited program whose name is already in the controller's memory and not in use, **When** the programmer runs `khi load <file>`, **Then** the controller replaces the program with the file's version, its reply prints verbatim, one result line ends the output, and the exit code is zero, as captures (i) and `i-load-existing-changed` show K-ROSET doing.
5. **Given** a program whose name is in use on the controller, **When** the programmer runs `khi load <file>`, **Then** the controller's refusal prints verbatim, and the exit code is non-zero because the controller reports an error count above zero, as capture (f) shows.
6. **Given** a file that does not exist, **When** the programmer runs `khi load <file>`, **Then** khi refuses by naming the file and does not connect.
7. **Given** a load under way, **When** the connection drops or a wait times out, **Then** khi says the load did not finish and that the controller's memory may have changed, one result line states what was transferred, and the exit code is non-zero.

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
5. **Given** an open session, **When** the person presses Ctrl+], or the console closes or its input ends, **Then** khi closes the socket and exits zero.
6. **Given** an open session, **When** the controller closes the connection, **Then** khi reports it and exits non-zero.
7. **Given** an open session, **When** the controller reports an error, **Then** the text reaches the screen unaltered, the session continues, and it does not count as a failure of the tool.
8. **Given** a controller that goes silent before login completes, **When** the wait times out, **Then** khi names the step it was waiting on, closes the session, and exits non-zero.
9. **Given** a typed LOAD the controller interrupts with a question, **When** the person types an answer, **Then** the answer reaches the controller unchanged and the transfer continues.
10. **Given** an open session with nothing selected, **When** the person presses Ctrl+C and VS Code passes it through, **Then** khi discards it: nothing reaches the controller, the session stays open, and it is not a quit.

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

A robot programmer copies the tasks template into a program repository and uses the named tasks instead of typing commands, with rejected programs appearing in the Problems list.

**Why this priority**: This is the finished experience, but it adds nothing to what the verbs already do at the command line.

**Independent Test**: Run the acceptance in INV-8 in a scratch repository with K-ROSET running.

**Acceptance Scenarios**:

1. **Given** the README and a program repository, **When** the person follows the README section, **Then** khi is published to a folder on the PATH and the template is in the repository's `.vscode/`.
2. **Given** the template and K-ROSET running, **When** the person runs *khi: Save controller to file* and picks full, **Then** the named file is written.
3. **Given** that file is open and the controller still holds its programs, **When** the person runs *khi: Load current file*, **Then** it loads back.
4. **Given** a load the controller rejects, **When** the task ends, **Then** the Problems list shows the line the controller named, or the file where it named none.
5. **Given** the template, **When** the person runs *khi: Open terminal* and types a SAVE, **Then** the file is written into the repository's folder.

---

### Edge Cases

- **Re-loading an edited program**: Both AS manuals say LOAD refuses a program name already in memory and aborts, and that programs are not overwritten. K-ROSET does the opposite: LOAD replaces the program, and nothing in its reply says so (captures (i) and `i-load-existing-changed`). `khi load` sends LOAD and nothing else, so a reload replaces the program on K-ROSET, and on a controller that refuses, khi reports the controller's words. Which way a live controller goes is on the live-controller check (Assumptions).
- **A program in use**: A LOAD of a program that is in use sends the whole file, and the controller then answers `Cannot execute because program already in use.` and `File load completed. (1 errors)` (capture (f)). The load has failed: khi reports the controller's words and exits non-zero (FR-017). khi does nothing to stop the program (Article III).
- **A rejected step may be a question, not an error**: Both manuals say an incorrect step produces "The step format is incorrect (0: Continue load 1: Delete program and exit)". K-ROSET prints `STEP syntax error.` and `(0:Change to comment and continue, 1:Delete program and abort)` (capture (c)). `khi load` hands it to the person and never answers it itself (FR-021). On K-ROSET, closing the session at that question left the controller serving no new connection until K-ROSET was restarted, so what memory held could not be read (captures (c) and `c-load-rejected-reconnect`). That is why the question goes to the person rather than ending the session.
- **The person never answers a load question**: If the console closes or its input ends while khi waits for the answer, khi closes the session, exits non-zero, and says the controller may still be waiting on the question and may need a restart.
- **The controller names no line**: Neither manual shows a line number for a rejected step. Where the controller names none, the output carries `<file>: <controller text>`, the problem matcher points at the file, and no line is invented. The plan brings the capture.
- **The line named is not a file line**: If the controller names a step within a program rather than a line of the file, khi treats it as naming no line. The controller's text keeps its step number verbatim, and it is never translated into a line (Article II).
- **A LOAD interrupted partway**: The manuals warn that original data is deleted if LOAD is canceled while overwriting. Article VI protects only the PC side, so after any failed or interrupted load khi says the load did not finish and that the controller's memory may have changed.
- **Ctrl+C during `khi load` or `khi save`**: Discarded (FR-041). It is not sent and it is not a quit: the transfer goes on, and at a load question khi keeps waiting for the typed answer. A person who wants to stop a transfer ends the task, which is the process being killed outright (below).
- **Ctrl+C during a transfer typed in `khi term`**: Discarded like any Ctrl+C (FR-014); the transfer goes on.
- **Ctrl+] during a transfer typed in `khi term`**: The session ends and khi exits zero, because the person ended it. A typed SAVE's target stays byte-identical to before with no temporary file left; a typed LOAD carries the warning above.
- **Ctrl+C at the prompt**: Ctrl+C is VS Code's copy. With nothing selected, VS Code passes it through and khi discards it; the session stays open. AS's own uses of Ctrl+C, cancelling the input line and the `CTRL+C:Exit` choice some AS prompts offer, are not available from `khi term`; the person uses another choice the prompt offers.
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
- **FR-003**: Eight more captures MUST be taken, because requirements below cannot pass Article IV without them: (g) SAVE with no qualifier, for FR-025; (h) a SAVE the controller refuses or reports an error in, for INV-4's controller error and FR-040; (i) LOAD of a program name already in memory, for User Story 2; (j) a typed LOAD naming a file that does not exist, for the edge case above; (l) LOAD of a generated program of about 60 KB whose length is not a multiple of the transfer block, so the last partial block is captured, for FR-013 and FR-016 on files larger than one block; (m) a SAVE onto a file name that already exists in the PC folder, to show whether the old file's rename to `.bas` (manual §5.3) travels in the protocol, for FR-026; (n) and (o), the LOAD question of capture (c) answered with `0` and with `1`, for FR-021. Capture (l) holds generated content only, none of the training files. If K-ROSET cannot be made to produce one, the behaviour it backs is recorded as not implemented and reported to the Architect.
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
- **FR-014**: After login, `khi term` MUST wait on the person without limit. Ctrl+C MUST NOT reach the controller: when VS Code passes it through because nothing is selected, khi MUST discard it, and it MUST NOT end the session. Ctrl+], the console closing, or its input ending MUST close the socket and exit zero; Ctrl+] itself MUST NOT reach the controller. A controller-side close MUST be reported and exit non-zero.
- **FR-015**: Every wait during connect, login and a typed transfer MUST have a timeout. A timeout MUST name the step that was waiting, leave the session closed, and exit non-zero.

**Load**

- **FR-016**: `khi load <file> [--robot <name>]` MUST connect, log in, send LOAD, stream the file byte for byte, and print the controller's reply verbatim.
- **FR-017**: `khi load` MUST exit zero only when the controller reports the load complete without error.
- **FR-018**: Where the controller names a line, the output MUST carry `<file>:<line>: <controller text>`. Where it names none, the output MUST carry `<file>: <controller text>`. khi MUST NOT invent a line, work one out by reading the file, or translate one from a step number (Article II).
- **FR-019**: A file that does not exist MUST be refused by name before connecting.
- **FR-020**: After a failed or interrupted load, khi MUST say the load did not finish and that the controller's memory may have changed.
- **FR-021**: When the controller asks a question during `khi load`, khi MUST print it verbatim and hand it to the person: wait without limit (Article X), send exactly the one answer the person types, Enter included as captures (n) and (o) show it, and read on. A Ctrl+C that reaches khi at the question MUST be discarded: it is not an answer and not a quit. khi MUST NOT answer on its own (Article III, v3.0.0). After a question, the exit code MUST be non-zero whichever answer was given, and the result line MUST name the answer in the controller's words: `completed with 1 error; answered <answer> (<the controller's label for it>)`, the label taken as the controller's question printed it, with `; the controller's copy differs from the file` added after `0`, which keeps the program with the step made a comment (capture (n)). On K-ROSET (captures (n) and (o)) that reads `completed with 1 error; answered 0 (Change to comment and continue); the controller's copy differs from the file` and `completed with 1 error; answered 1 (Delete program and abort)`. The count is the controller's, as `<n> errors` when it is more than one. If the console closes or its input ends before an answer, khi MUST close the session, exit non-zero, and say the controller may still be waiting on the question.

**Save**

- **FR-022**: `khi save <file> [--qualifier <q>] [--robot <name>]` MUST connect, log in, send SAVE with the qualifier, write to a temporary file beside `<file>`, and replace `<file>` only after the controller signals the transfer complete.
- **FR-023**: On any failure (timeout, disconnect, controller error) the existing file MUST be byte-identical to before and the temporary file removed (Article VI).
- **FR-040**: A SAVE MUST fail if, outside the file data, the controller prints any line that is not one of the captured normal lines (research R17). khi MUST report each such line verbatim, and the failure MUST follow FR-023. This holds for `khi save` and for a SAVE typed in `khi term`; in `khi term` the session goes on (FR-011).
- **FR-024**: The qualifiers supported MUST be those the manual lists and a capture backs. Any other MUST be refused by name before connecting, including those the manual lists with no capture: `L`, `R`, `S`, `A`, `SYS`, `ROB`, `ELOG`, `OPLOG`, `ALLLOG`, `STG`.
- **FR-025**: With the captures above, the supported forms MUST be `--qualifier P` (`SAVE/P`), `--qualifier FULL` (`SAVE/FULL`), and no qualifier (`SAVE`).
- **FR-026**: khi MUST write exactly the file the person named: no extension added and no `.bas` backup of the old file. A typed SAVE writes the file the controller announces, under the same protection.
- **FR-027**: The LOAD or SAVE command khi sends MUST carry the file's bare name, not its path. The path stays on the PC.

**Keys**

- **FR-041**: In `khi load` and `khi save`, as in `khi term` (FR-014), a Ctrl+C that reaches khi MUST be discarded: it MUST NOT be sent to the controller, and it MUST NOT end the run. Copy and paste are VS Code's; khi emulates no KRterm shortcut.

**Result line**

- **FR-028**: `khi load` and `khi save` MUST each end with one result line, on success and on failure: the verb, the robot, the file, the bytes transferred, and the programs where the controller reports them. Bytes count file content only, not protocol traffic.

**Template and README**

- **FR-029**: `templates/tasks.json` MUST provide, for a program repository's `.vscode/`: *khi: Load current file* (`${file}`), *khi: Save controller to file* (prompts for the file name and qualifier), and *khi: Open terminal*.
- **FR-030**: The load task MUST carry a problem matcher for the `<file>:<line>:` form and the `<file>:` form. The save task's qualifier prompt MUST offer only supported qualifiers.
- **FR-031**: A README section MUST show how to publish khi to a folder on the PATH and copy the template.

**Refusals**

- **FR-032**: An unwired verb or option MUST be refused with a message saying so. No placeholder value may be presented as real.
- **FR-033**: Every refusal and error MUST be shown reaching the exit code and the output through the `khi` entry point (Article XIII).

**Replacing programs**

- **FR-034, FR-035, FR-037, FR-038, FR-039**: Removed on 2026-11-04. They described `khi load --replace`, which the Architect dropped from this feature (Clarifications, Session 2026-11-04). `khi load` sends no DELETE/P of any kind.
- **FR-036**: Capture (k), KRterm sending DELETE/P for a program that is present, one that is absent, one the controller refuses, and the confirmation the controller asks for, was taken on 2026-11-04 and is kept. No behaviour in this feature relies on it since `--replace` was dropped.

### Invariants as Tests

These are the Architect's, word for word, except where the Architect's rulings of 2026-11-02, 2026-11-03 and 2026-11-04 changed INV-2, INV-6, INV-7, INV-8 and INV-9.

- **INV-1 — Not an editor.** The bytes khi load sends for a file's content equal the file on disk. Gate: replay the LOAD capture with a fixture that has trailing spaces, tabs and mixed line endings; the stand-in receives them unchanged. Fire: trim trailing whitespace before sending.
- **INV-2 — Not a robot runner.** Across every verb and every test, the controller commands khi issues of its own are only the login and the running verb's LOAD or SAVE command; in khi term, the only commands are the ones the test typed. Gate: the stand-in records every command received and the suite compares the union against that set; file content is covered by *Not an editor*. Fire: send ERESET before LOAD.
- **INV-3 — Not a guesser.** Every byte sequence khi originates or waits for — negotiation, login, command, transfer framing — is asserted by a test that names the capture it replays; a protocol test with no capture fails the gate. File content and typed input are not protocol. Fire: add a test with a hand-written expected reply.
- **INV-4 — Not destructive on the PC side.** The stand-in drops the connection halfway through a SAVE; the target file is byte-identical to before and no temporary file remains. Repeat with a timeout and with a controller error. Fire: write straight to the target.
- **INV-5 — No transfer hangs.** The stand-in goes silent during connect, during login, and mid-transfer; khi load and khi save exit non-zero within their timeout, naming the step they were waiting on, and khi term does the same before login. Fire: remove the timeout on one read.
- **INV-6 — The terminal waits on the person.** After login, a silent stand-in leaves khi term open past every transfer timeout. When the stand-in closes the connection, khi term reports it and exits non-zero; when the test ends the session with Ctrl+], it exits zero. Fire: apply the transfer timeout to the relay.
- **INV-7 — The controller's words, verbatim.** Replaying the rejected-LOAD capture, the output contains the controller's text exactly, carries `<file>:<line>:` for the line it names or `<file>:` where it names none, and the exit code is non-zero.
- **INV-8 — Acceptance against K-ROSET, from VS Code.** With the template copied into a scratch repository and K-ROSET running: *Save controller to file* (full) writes the file; *Load current file* loads it back; a second full save equals the first except for lines the controller stamps, which the plan lists. *Open terminal* reaches the prompt, and SAVE typed there writes into the scratch repository's folder.
- **INV-9 — The constitution.** The constitution in `.specify/memory/constitution.md` is v3.0.0 as corrected at merge, and every review gate it lists is shown passing.

**Added by this spec, because the articles require them and the list above does not reach them:**

- **INV-7 Fire** (Article VII requires one): reword the controller's text before printing it.
- **INV-10 — Typed transfers are bounded and safe.** The stand-in goes silent, and separately drops the connection, halfway through a SAVE typed in `khi term`. khi term reports it and exits non-zero, the target is byte-identical to before, and no temporary file remains. Fire: apply the terminal's unlimited wait to the typed transfer.
- **INV-11 — No answers on the person's behalf.** Replaying the rejected-LOAD capture up to the controller's question, `khi load` prints the question and sends nothing while the test types nothing, past every transfer timeout. When the test types `0` and Enter, or `1` and Enter, exactly those bytes reach the stand-in, and the replay of capture (n) or (o) runs to its end; when the test types Ctrl+C, nothing reaches the stand-in and khi keeps waiting. Fire: send `1` at the question before the test types. *(Changed on 2026-11-04 with constitution v3.0.0.)*
- **INV-12 — The path stays on the PC.** Given a file named by full path, the command the stand-in records carries only the bare name. Fire: send the full path.

**From the 2026-11-02 rulings:**

- **INV-13**: Removed on 2026-11-04 with `--replace`.
- **INV-14 — Ctrl+C never reaches the controller.** In `khi term`, at the prompt and during a typed transfer, and in `khi load` and `khi save`, a Ctrl+C in the input never reaches the stand-in and ends nothing; Ctrl+] ends the `khi term` session, exits zero, and never reaches the stand-in. Fire: relay Ctrl+C. *(Reversed on 2026-11-04: it said Ctrl+C belongs to the controller.)*
- **INV-15, INV-16**: Removed on 2026-11-04 with `--replace`.

**From the 2026-11-03 rulings:**

- **INV-17, INV-18**: Removed on 2026-11-04 with `--replace`.

**From the 2026-11-04 rulings:**

- **INV-19 — A SAVE with an unknown line fails.** Replaying capture (h), `khi save` prints `nosuch:Variable (or program) does not exist.` verbatim, the target file is byte-identical to before, no temporary file remains, and the exit code is non-zero. Replaying captures (d), (e) and (g), every line outside the file data is a captured normal line, and the save succeeds. Fire: accept any line outside the file data.

### Manual Facts This Spec Relies On

Each was read in both AS Language Reference Manuals, 90209-1022 (E) and 90209-1025 (F), at the same section number. The citations in the code still sit beside the capture that proves them (FR-006).

| What | Section |
|------|---------|
| Enter `as` at the `login :` message; the `>` prompt follows | 2.6.1 |
| The terminal control keys typed at the prompt; Ctrl+C among them, which khi never relays | 2.4.1 |
| SAVE and LOAD use the PC connection; `SAVE file.pg`, `LOAD data01.as` | 2.6.2 |
| A KRterm session ends from its [Com] [Disconnect] menu; the manuals list no logout command | 2.6.3 |
| SAVE qualifiers; `/FULL` is all data that can be saved; bare `SAVE` stores five data types in one file | 5.3 |
| Extensions added when none is given (`.as`, `.PG`, and so on); the old file is renamed to `.bas` | 5.3 |
| LOAD refuses a program name already in memory and aborts; variables are overwritten without warning. K-ROSET replaces the program instead (captures (i) and `i-load-existing-changed`), which goes on the live-controller check | 5.3 |
| "The step format is incorrect (0: Continue load 1: Delete program and exit)" | 5.3 |
| Original data is deleted if LOAD is canceled while overwriting | 5.3 |
| `Loading...` and `Loading done.` as the load's progress and completion | 5.3 |
| `P0108 Syntax error.`, `P2009 Illegal file name.` | Appendix 4 |

The K-IDE manual (20201116) adds two facts: K-ROSET's first robot answers at 127.0.0.1:9105 (2.5.4), and transferring programs while the robot is running is strongly discouraged (5.6.1).

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
- **SC-008**: A controller rejection appears in the output exactly as the controller wrote it, with a non-zero exit, and the person reaches the line it named, or the file where it named none, from the editor's problem list in one click.
- **SC-009**: A second full save equals the first in every line except the stamped lines the plan lists.
- **SC-010**: Following only the README, a person publishes the tool and installs the template in a new program repository.
- **SC-011**: All ten of the constitution's review gates are shown passing, and every guard carries its fire demonstration and a test through the tool's entry point.

## Assumptions

- **"Full" means `SAVE/FULL`, "programs only" means `SAVE/P`.** The manual's `/FULL` is "all data that can be saved"; bare `SAVE` is a different, smaller set.
- **The session ends on Ctrl+], the console closing, or its input ending.** The manuals list no logout command for the prompt, so there is no controller command for this.
- **`khi.robots.json` is read from the current folder.** A command-line tool cannot see VS Code's workspace root; the template's tasks run there, and a person who changes folder gets the missing-file refusal.
- **The simulator flag changes no behaviour in this feature.** It is read and checked for its type because Article IX requires the field, and nothing yet acts on it.
- **Timeouts apply to each wait, not to a whole transfer,** so a long transfer that keeps moving does not time out. The plan sets each value from the timing seen in the captures. There is no person-facing timeout option.
- **Proof comes from K-ROSET.** Real-controller behaviour is not proven by this feature. Article IV allows a controller capture to be added later.
- **Live-controller check.** Before khi is relied on against a live controller, these differences between the manuals and K-ROSET are checked there:
  - LOAD of a program name already in memory: both AS manuals say it is an error that aborts the LOAD and that programs are not overwritten (§5.3), and list `P1033 Program name already exists.` (Appendix 4); K-ROSET replaces the program and reports no error (captures (i) and `i-load-existing-changed`).
- **The tools are on this PC.** K-ROSET 1.8.6.19269, KRterm 1.2.8 and K-IDE are installed; none was started for this spec, and whether K-ROSET needs a licence to run was not checked. No loopback adapter or Npcap is installed, which favours the relay route. Any relay stays within Article XV, and within Article I unless the plan justifies a new project.
- **Windows only,** because K-ROSET and KRterm are Windows programs.
- **Keys VS Code keeps for itself never reach khi,** so they cannot be relayed. The plan checks Ctrl+] and the keys in manual section 2.4.1 against this.
- **khi has no shortcuts of its own besides Ctrl+].** It does not emulate KRterm's (Alt+V paste and the like); copy and paste are VS Code's terminal's, and Ctrl+C is copy. A Ctrl+C VS Code passes through is discarded in every verb (FR-014, FR-041).
- **Out of scope:** `/SEL` and choosing programs by name, `LOAD/Q` at `khi load` (its questions need a person; it can be typed in `khi term`), the `USB_` commands, and removing anything on the controller: `--replace` and every form of DELETE were dropped from this feature on 2026-11-04.
