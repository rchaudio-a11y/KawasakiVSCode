# Contract: guards, tests and fire demonstrations

Every guard the spec and constitution ask for, where its test lives, and how it is shown to fire. Article VII says to inject the defect, watch the test go red, revert, confirm green, and record that beside the guard. The record goes in the test file's comment block: date, defect, red output, green output.

Each runtime guard also has a test through `KhiApp.Run` (Article XIII).

## Invariants

| Guard | Test | Fire |
|-------|------|------|
| INV-1 Not an editor | Protocol, capture (b) with a fixture holding trailing spaces, tabs and mixed line endings | Trim trailing whitespace before sending |
| INV-2 Not a robot runner | Suite-wide: the union of every recorded command against the allowed set | Send ERESET before LOAD |
| INV-3 Not a guesser | Conformance: every protocol test names a capture, and none writes its own reply except INV-18's altered-reply test | Add a test with a hand-written expected reply |
| INV-4 Not destructive on the PC side | Protocol, capture (d) cut mid-transfer, plus a timeout and capture (h) | Write straight to the target |
| INV-5 No transfer hangs | Protocol, silent at connect, at login and mid-transfer for load and save; term before login | Remove the timeout on one read |
| INV-6 The terminal waits on the person | Protocol, capture (a), silent after login; closed by the stand-in; ended with Ctrl+] | Apply the transfer timeout to the relay |
| INV-7 The controller's words, verbatim | Protocol, capture (c) | Reword the controller's text before printing |
| INV-8 Acceptance against K-ROSET | By hand, [quickstart.md P3](../quickstart.md#p3-acceptance-in-vs-code) | Not applicable (acceptance) |
| INV-9 The constitution | Every review gate shown passing at merge | Not applicable (review) |
| INV-10 Typed transfers bounded and safe | Protocol, capture (f), silent and closed mid typed SAVE | Apply the terminal's unlimited wait to the typed transfer |
| INV-11 No answers on the person's behalf | Protocol, capture (c) at the question | Send `1` at the question |
| INV-12 The path stays on the PC | Protocol, capture (b), file given by full path | Send the full path |
| INV-13 Replacing is the person's choice | Protocol, capture (k), with and without `--replace` | Send DELETE/P without the flag |
| INV-14 Ctrl+C belongs to the controller | Protocol, capture (a), redirected input carrying 0x03 then 0x1D | Close the session on Ctrl+C |
| INV-15 Header lines and nothing else | Unit test on `ProgramHeaderReader`, then a protocol test under `--replace` | Match `.PROGRAM` anywhere in a line |
| INV-16 A replace reports what it touched | Protocol, capture (k), connection dropped during LOAD | Report only on success |
| INV-17 A new program loads | Protocol, capture (k) with a P1038 reply | Treat P1038 as a refusal |
| INV-18 An unrecognised reply stops the replace | Protocol, capture (k) with one DELETE/P reply altered; asserts no LOAD and every deleted program named | Proceed to LOAD on an unrecognised reply |

## Conformance guards for the hard rules

| Guard | Test | Fire |
|-------|------|------|
| Option settings in every project file | Reads each `.vbproj`: `OptionStrict On`, `OptionExplicit On`, `OptionInfer Off`, `GenerateDocumentationFile true` | Remove one setting from one project |
| Header block on every `.vb` file | Reads each `.vb` file for filename, project, description, author `RCH Automation LLC`, created date | Drop the author line from one file |
| XML documentation on every public member | Compares the public API with the generated XML documentation (research R4) | Delete one `<summary>` |
| One type per `.vb` file | Counts top-level `Class`, `Module`, `Interface`, `Enum` and `Structure` declarations per file | Add a second class to one file |

## Structural claims proved in review

Once code exists, these are proved with CodeMem `references` or `type_usages` against a current map, with the count stated. Text search is not enough.

- Only `TerminalRelay` calls the unbounded read.
- `ControllerConnection.Send` is reached only with a `ControllerCommand`, `TransferFraming` bytes, or pass-through content.
- `RobotSelection.Resolve`, `SaveQualifier.Parse` and `ProgramHeaderReader` are the only places their rules are applied.
