# Contract: guards, tests and fire demonstrations

Every guard the spec and constitution ask for, where its test lives, and how it is shown to fire. Article VII says to inject the defect, watch the test go red, revert, confirm green, and record that beside the guard. The record goes in the test file's comment block: date, defect, red output, green output.

Each runtime guard also has a test through `KhiApp.Run` (Article XIII).

## Invariants

| Guard | Test | Fire |
|-------|------|------|
| INV-1 Not an editor | Protocol, capture (b) with a fixture holding trailing spaces, tabs and mixed line endings | Trim trailing whitespace before sending |
| INV-2 Not a robot runner | Suite-wide: the union of every recorded command against the allowed set | Send ERESET before LOAD |
| INV-3 Not a guesser | Conformance: every protocol test names a capture, and none writes its own reply | Add a test with a hand-written expected reply |
| INV-4 Not destructive on the PC side | Protocol, capture (d) cut mid-transfer, plus a timeout and capture (h) | Write straight to the target |
| INV-5 No transfer hangs | Protocol, silent at connect, at login and mid-transfer for load and save; term before login | Remove the timeout on one read |
| INV-6 The terminal waits on the person | Protocol, capture (a), silent after login; closed by the stand-in; ended with Ctrl+] | Apply the transfer timeout to the relay |
| INV-7 The controller's words, verbatim | Protocol, captures (n) and (o) | Reword the controller's text before printing |
| INV-8 Acceptance against K-ROSET | By hand, [quickstart.md P3](../quickstart.md#p3-acceptance-in-vs-code) | Not applicable (acceptance) |
| INV-9 The constitution | Every review gate shown passing at merge | Not applicable (review) |
| INV-10 Typed transfers bounded and safe | Protocol, capture (d) typed in `khi term`, silent and closed mid typed SAVE | Apply the terminal's unlimited wait to the typed transfer |
| INV-11 No answers on the person's behalf | Protocol, capture (c) at the question: nothing sent while the test types nothing, past every transfer timeout; then captures (n) and (o) with `0` and `1` typed; a Ctrl+C typed there sends nothing | Send `1` at the question before the test types |
| INV-12 The path stays on the PC | Protocol, capture (b), file given by full path | Send the full path |
| INV-14 Ctrl+C never reaches the controller | Protocol: `khi term` on capture (a) with redirected input carrying 0x03 then 0x1D, and during the typed transfer of capture (f); `khi load` on capture (b) and `khi save` on capture (d) with 0x03 in the input. No 0x03 reaches the stand-in, and only 0x1D ends anything | Relay Ctrl+C |
| INV-19 A SAVE with an unknown line fails | Protocol, capture (h) typed in `khi term`: the line shown verbatim, the target untouched, the session goes on; captures (d), (e) and (g) through `khi save`, every non-data line checked and normal | Accept any line outside the file data |

INV-13, INV-15, INV-16, INV-17 and INV-18 were removed on 2026-11-04 with `--replace`.

## Conformance guards for the hard rules

| Guard | Test | Fire |
|-------|------|------|
| Option settings in every project file | Reads each `.vbproj`: `OptionStrict On`, `OptionExplicit On`, `OptionInfer Off`, `GenerateDocumentationFile true` | Remove one setting from one project |
| Header block on every `.vb` file | Reads each `.vb` file for filename, project, description, author `RCH Automation LLC`, created date | Drop the author line from one file |
| XML documentation on every public member | Compares the public API with the generated XML documentation (research R4) | Delete one `<summary>` |
| One type per `.vb` file | Counts top-level `Class`, `Module`, `Interface`, `Enum` and `Structure` declarations per file | Add a second class to one file |
| No address in code (Article IX) | Scans `src/**/*.vb` for IPv4 literals and `localhost` | Put `127.0.0.1` in one Core file |

## Structural claims proved in review

Once code exists, these are proved with CodeMem `references` or `type_usages` against a current map, with the count stated. Text search is not enough.

- Only `TerminalRelay` calls the unbounded read.
- `ControllerConnection.Send` is reached only with a `ControllerCommand`, `TransferFraming` bytes, or pass-through content.
- `RobotSelection.Resolve`, `SaveQualifier.Parse`, `TransferFraming`, `LoadLineRule`, `SaveLineRule`, `SaveTarget` and `PersonInput` are the only places their rules are applied.
