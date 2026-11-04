# Quickstart: validating khi

How to show the feature works, from the capture route through acceptance in VS Code. It points to the contracts rather than repeating them.

## Prerequisites

- Windows 11 with the .NET SDK 10 (10.0.401 is installed here).
- K-ROSET 1.8.6.19269 with a project that has one robot inserted. Its first robot answers at 127.0.0.1:9105 (K-IDE manual 2.5.4).
- KRterm 1.2.8 at `C:\KawasakiRobot\KRterm\`.
- VS Code with this repository, and a scratch program repository for P3.

## P0 Prove the capture route

FR-005. This is done together, once, before any capture: the Architect runs K-ROSET and KRterm, and the agent runs the relay. Captures (a) to (q) follow in the same session.

1. **Architect**: Start K-ROSET with its robot. **Agent**: Confirm something is listening on 127.0.0.1:9105, and that `tools\Test-CaptureRelay.ps1` passes.
2. **Agent**: Start the relay. `<free port>` is any unused local port. The relay serves one KRterm connection and exits when it closes.

   ```powershell
   tools\Start-CaptureRelay.ps1 -ListenPort <free port> -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile $env:TEMP\route-proof\exchange.txt
   ```

3. **Architect**: In KRterm, add a TCP connection to 127.0.0.1 on `<free port>`, connect, and type `as` at `login: `.
4. **Pass when**:
   - KRterm reaches the `>` prompt.
   - `exchange.txt` holds `S` records with the login prompt and `C` records with `as`.
   - A second connection made straight to 9105 shows the same prompt.
5. **Agent**: Record the result under R1 in `research.md`. This proof is not a capture and is not checked in.

## P1 Take the captures

These are FR-002, FR-003 and FR-036's captures (a) to (q), each through the relay, in the same session as P0. The Architect drives KRterm. The agent starts one relay run per capture, writing straight into that capture's folder, and drafts each note for the Architect to confirm.

1. **Before the first capture**, check that `.gitattributes` marks `Captures/**` and `Fixtures/**` as `-text`.
2. **For each capture**, create its folder and `note.md`, laid out as [contracts/capture-format.md](contracts/capture-format.md) shows, and put any file KRterm wrote or read in `files/`.
3. **For (k)**, record DELETE/P on a program that is present, one that is absent (P1038), one the controller refuses (in execution or selected), and any confirmation it asks for.
4. **For (m)**, put a file under the target name in KRterm's folder first, keep a copy of it in the capture's `files/`, and afterwards move everything KRterm left in the folder, such as a `.bas` file, into `files/` as well.
5. **For (n) and (o)**, load a file with the syntax error of capture (c), each under its own program name, and answer the question with `0` and Enter, then with `1` and Enter. Never close a session at the question: on K-ROSET that leaves the controller unusable until a restart (`c-load-rejected-reconnect`).
6. **For (p)**, load a generated file longer than 512 bytes whose syntax error sits in its first block, and answer the question with `1` and Enter. The capture shows what KRterm sends at a question while file data is still to be sent.
7. **For (q)**, load an empty file. The capture shows what KRterm sends for a file with no bytes.
8. **A case K-ROSET cannot produce** is recorded in the note as not produced, and its behaviour stays unimplemented (FR-003).

## P2 Build and test

```powershell
dotnet build KawasakiVSCode.slnx
dotnet test KawasakiVSCode.slnx
```

**Expected**: every test passes. Every guard in [contracts/test-guards.md](contracts/test-guards.md) has its fire record in its test file.

## P3 Acceptance in VS Code

INV-8 and User Story 6, against K-ROSET.

1. Publish khi to a folder on the PATH and copy `templates/tasks.json` into the scratch repository's `.vscode/`, as the README shows. Add a `khi.robots.json` naming K-ROSET's robot as the default ([contracts/robots-file.md](contracts/robots-file.md)).
2. Run *khi: Save controller to file*, pick **full**, and name the file. **Expected**: the file exists and the result line says `ok`.
3. Open that file and run *khi: Load current file*. **Expected**: the controller's text in the output, the result line says `ok`, and the exit code is 0.
4. Save full again to a second file. **Expected**: the two files match except for the stamped lines, which the implementation lists once the first acceptance run shows them.
5. Run *khi: Open terminal*. **Expected**:
   - The `>` prompt appears.
   - With nothing selected, Ctrl+C does nothing: no `^C` reaches the controller, and the session stays open. With text selected, it copies.
   - A typed `SAVE x.as` writes `x.as` into the scratch repository's folder, and a typed `load x.as` loads it back. khi relays both lines as typed, without KRterm's `18`, so these two steps show whether the controller starts a transfer for a plain typed line (FR-013).
   - Ctrl+] ends the session with exit code 0.
   - If Ctrl+] or an AS control key does not reach khi, check `terminal.integrated.sendKeybindingsToShell` and report what was needed.
6. Edit a program to hold a syntax error and run *khi: Load current file*. When the controller asks its question, type `1` and Enter in the task's terminal. **Expected**: khi waits for the answer however long it takes, then the controller's text in the output, a non-zero exit, and the Problems list showing the named line, or the file where no line is named.
7. Close the terminal panel during a `khi term` session. **Expected**: khi exits without leaving a process running (research R8).

## P4 Live-controller check

Not part of this feature's acceptance, which is against K-ROSET. Before khi is relied on against a live controller, check there each place where K-ROSET and the manuals disagree (spec, Assumptions):

1. **LOAD of a program name already in memory**: load a file, change one step, and load it again. The manuals say the second LOAD is an error and the program is not overwritten (§5.3; `P1033` in Appendix 4); K-ROSET replaces it (captures (i) and `i-load-existing-changed`). Record what the controller does, through the relay, as a capture against that controller.
