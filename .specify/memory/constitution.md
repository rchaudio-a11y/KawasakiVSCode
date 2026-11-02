# KawasakiVSCode Constitution

KawasakiVSCode is a VB.NET command-line tool, khi, that does a Kawasaki robot terminal's three jobs
from VS Code: an interactive terminal to a controller's AS prompt over TCP, LOAD (a file on the PC
into the controller) and SAVE (controller data into a file on the PC). It reads AS program files and
a controller list, and it writes the files a SAVE produces. It runs from VS Code tasks in the
repository where the robot programs are kept; there is no UI in this repository beyond the terminal.

## Core Principles

### Article I — Library-First

The tool is partitioned by concern into class libraries; the executable only wires. Initial projects:
KawasakiVSCode.Core (connection, telnet negotiation, login, transfer protocol, controller list),
KawasakiVSCode.Cli (the khi entry point and its verbs), KawasakiVSCode.Tests. Adding a project
requires written justification in the active plan. One class, module, interface or enum per .vb file.

**Rationale:** Behaviour in a library can be tested by concern; an executable that only wires has no logic to hide.

### Article II — Files Are Opaque

The tool moves AS files byte for byte. It never parses, rewrites, reformats or validates AS source;
what it reports about a file's content is what the controller said about it.

**Rationale:** The controller is the authority on AS source; a tool that interprets it can corrupt or contradict it.

### Article III — The Person Commands the Robot

The only controller commands the tool issues on its own are the login and the command of the verb
being run (LOAD or SAVE with its qualifier). Everything else it sends is either protocol traffic
governed by Article IV — telnet negotiation replies and transfer framing — or content it passes
through unchanged: the bytes of the file being loaded (Article II) and what the person types in the
terminal verb. In the terminal verb it adds no command. No path issues motion, execution, motor
power, error reset or any other controller command the person did not type.

**Rationale:** A robot can hurt people and damage equipment, so every command beyond the login and the verb's own comes from a person.

### Article IV — Recorded Protocol Only

Every byte sequence the tool itself originates or waits for — telnet negotiation, the login, the
verb's command, and the transfer framing around a file — is backed by a captured exchange with a real
Kawasaki terminal session (KRterm against K-ROSET or a controller), checked into the test project
with a note of what was captured, against what, and when. A behaviour with no capture behind it is
not implemented. The bytes of a file being loaded (Article II) and what the person types in the
terminal verb (Article III) pass through unchanged and are outside this article.

**Rationale:** Protocol bytes no real session has been seen to exchange are guesses; content passed through is the person's, not the tool's guess.

### Article V — Integration Against Captures and the Simulator

Protocol tests run over real TCP sockets against an in-process stand-in that replays the captures byte
for byte; no mocks of the socket or the stream. Acceptance runs against K-ROSET. Unit tests are allowed
where they improve isolation; they never substitute for capture replay.

**Rationale:** A mock confirms what the code already assumes; replayed captures and K-ROSET show what a controller does.

### Article VI — A Save Never Damages the File It Replaces

SAVE writes to a temporary file beside the target and replaces the target only after the controller
has signalled the transfer complete. On any failure — timeout, disconnect, controller error — the
existing file is byte-identical to before and the temporary file is removed.

**Rationale:** A failed SAVE costs the person nothing, because the last good copy of the program is still there.

### Article VII — Test-First

No implementation before a failing test. Order: write test → run → confirm Red → report Red to the
Architect → implement → confirm Green. A guard is trusted only after it has been shown to fire: inject
the defect, watch it go red, revert, confirm green, record the demonstration beside the guard. A test
that asserts something does NOT happen over a mechanism that does not yet exist passes vacuously and
must not be counted as Red.

**Rationale:** A test seen red has proved it can detect the defect; a test that has never failed has proved nothing.

### Article VIII — The Controller's Words, Verbatim

For LOAD and SAVE: every error the controller returns is reported in its own text, unaltered, with a
non-zero exit code; where it names a line, the output carries `file:line` in a form a VS Code problem
matcher reads; and the verb ends with one result line stating what was transferred (file, bytes,
programs where the controller reports them) — success reports what was done, not only that nothing
failed. In the terminal verb the controller's output reaches the screen unaltered; an error the
controller reports there is the person's to read, not a failure of the tool.

**Rationale:** The controller's own text is what a person can act on; a transfer states its result, a terminal session just shows it.

### Article IX — Controllers Are Configuration

No address, port or credential appears in code. Controllers are named entries in one configuration file
(name, address, port, simulator flag); a workspace may name its default. An unknown robot name is
refused by name. The login name `as` is a documented protocol constant, not a credential: it lives in
one named constant in KawasakiVSCode.Core, citing the AS Language Reference Manual section that gives
it (Article XVI).

**Rationale:** One build serves every robot, an unknown name cannot reach another controller, and a documented login has one cited home.

### Article X — Bounded Waits

Every wait during connect, login and a LOAD or SAVE transfer has a timeout, so neither transfer verb
can hang. A timeout is reported as a timeout, naming the step that was waiting, and leaves the
session closed. After login, the terminal verb waits on the person without limit. When the person
ends the session it exits with code zero; if the controller closes the connection, it reports that
and exits non-zero.

**Rationale:** In a VS Code task a hang looks like work in progress, while at the terminal an idle wait is the person's to end.

### Article XI — Anti-Abstraction

One canonical model per concept. Duplication is expected until a pattern has three call sites in hand
(Rule of Three, Fowler 1999); anticipated call sites do not count. No dependency injection container,
no plugin system.

**Rationale:** An abstraction built before three real uses exist encodes a guess about what they share.

### Article XII — One Door for Every Rule

Validation lives at one entry point every code path walks. A second site consults the owning door; it
never restates the rule.

**Rationale:** A rule stated in two places drifts until they disagree, and then one code path goes unguarded.

### Article XIII — Production-Route Reachability

A guard is demonstrated reachable from the production path it protects: at least one test exercises it
through the khi entry point, proving refusals and errors reach the exit code and the output. A rule
with correct logic and no production caller is absent, not partial.

**Rationale:** A guard production never calls protects nothing, however correct its logic.

### Article XIV — Archive, Never Delete

Retired code moves to `_Archive/`; deprecated members receive `<Obsolete>` and stay until reviewed.

**Rationale:** Retired code stays on hand to consult or restore, and nothing leaves without a review.

### Article XV — Compiled .NET, No Foreign Runtime

VB.NET on .NET 10 (LTS). No Python, Node or other runtime dependency. PowerShell is permitted for
build and dev-time scripts only. No third-party package without written justification in the plan.

**Rationale:** An LTS runtime stays supported for years, and anything added must be working on each PC that connects to a robot.

### Article XVI — The Manuals Are the Reference

Manufacturer manuals are always readable from this repository: every manual in
`C:\Users\rchau\OneDrive\Documents\_MemOS\RCH Automation LLC\Manuals`, one folder per manufacturer.
Kawasaki's are in its Kawasaki folder — the controller operation and troubleshooting manuals at the
top, the AS Language Reference Manuals for the E and F controllers (90209-1022, 90209-1025) in
`Kawasaki\AS Language`. Every command, qualifier and controller message the tool relies on cites the
manual number and section it comes from, beside the capture that proves it. The folder is read-only to
this repository: nothing in it is changed, and no manual is copied into the repository.

**Rationale:** A citation beside a capture shows what Kawasaki documents and what the controller did; one read-only library leaves no copy to drift.

## Cross-Cutting Hard Rules

- Option Strict On, Option Explicit On, Option Infer Off in every project.
- Every .vb file starts with a header block: filename, project, description, author (RCH Automation LLC), created date.
- XML documentation comments on every public class, method, property, event and interface member; GenerateDocumentationFile enabled so the compiler enforces it.
- Naming: classes/methods/properties PascalCase; interfaces I-prefixed; private fields `_camelCase`; parameters and locals camelCase.
- Diagnose-Before-Fix: symptom → source → cause → diagnosis → proposed fix → wait for confirmation → then fix.
- Propose-Before-Building for anything touching more than one file, changing a signature, or making an architectural choice.
- No placeholder values presented as real; an unwired verb or option is refused with a message saying so.

## Review Gates

Every plan, task list and implementation review verifies:

- Option settings present in every touched project file.
- Every byte sequence the tool originates or awaits traces to a checked-in capture; file content and typed input are outside the check (Art. IV).
- Every command, qualifier and controller message relied on cites a manual number and section (Art. XVI).
- No path issues a controller command other than login and the running verb's own command (Art. III), asserted by a test that records every command sent across all verbs and compares it to the allowed set; LOAD's file content is checked separately, byte for byte against the file on disk.
- Every new guard carries its fire demonstration (Art. VII) and a production-route test (Art. XIII).
- No rule validated at two doors.
- Every wait during connect, login and transfer has a timeout, and the terminal relay after login is the only unbounded wait (Art. X).
- Header block and XML docs on every new or modified file.
- Any new abstraction justified by three call sites in hand.

## Governance

This constitution is the highest authority in this repository. Where it is silent, ask the Architect;
silence is not permission. A rule is not changed by having been broken. Amendments require Architect
approval, a dated entry with rationale, and the migration path for code the change puts out of
compliance. Versioning: MAJOR for removed or redefined principles, MINOR for new articles or expanded
obligations, PATCH for wording.

**Version**: 1.0.0 | **Ratified**: 2026-11-02 | **Last Amended**: 2026-11-02
