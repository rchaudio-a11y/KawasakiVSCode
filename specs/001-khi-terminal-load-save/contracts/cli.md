# Contract: the khi command line

What the person and VS Code tasks see. The behaviour behind it is in [spec.md](../spec.md), and the field rules are in [data-model.md](../data-model.md).

## Synopsis

```text
khi term [--robot <name>]
khi load <file> [--robot <name>]
khi save <file> [--qualifier <q>] [--robot <name>]
```

- **Options**: An option may appear before or after `<file>`. Each option may appear once.
- **Refused before connecting**, each by name: an unknown verb, an unknown option, an option the verb doesn't take, a repeated option, a missing `<file>`, an extra argument, and an unsupported qualifier. Verbs, options and qualifiers match exactly, case included. Each refusal is one line on stderr, exit 2; only the first is followed by the usage:

  ```text
  khi: no verb given                       (followed by the usage below)
  khi: unknown verb <verb>
  khi: unknown option <option>             (any argument starting with -, other than --robot and --qualifier)
  khi: khi <verb> does not take <option>
  khi: <option> is given twice
  khi: <option> needs a value              (the next argument is missing or starts with -)
  khi: khi <verb> needs a <file>
  khi: unexpected argument <argument>
  khi: qualifier "<q>" is not supported; khi save takes --qualifier P, --qualifier FULL, or no qualifier
  khi: unknown qualifier "<q>"; khi save takes --qualifier P, --qualifier FULL, or no qualifier
  ```

  "Not supported" is for the qualifiers the manual lists with no capture behind them (FR-024); "unknown" is for anything else. The usage:

  ```text
  usage: khi term [--robot <name>]
         khi load <file> [--robot <name>]
         khi save <file> [--qualifier <q>] [--robot <name>]
  ```
- **Refused before connecting, for a file**:

  ```text
  khi: <file> does not exist                                  (khi load, FR-019)
  khi: <file> holds byte <ff|17> at offset <n>; it cannot be sent  (khi load, FR-042; <n> counts from 0)
  khi: <file> has no extension; name the file with one        (khi save, FR-043)
  khi: <file> has a name outside ASCII; it cannot be sent      (khi load and khi save, FR-044)
  ```
- **Unwired verbs and options**: any not yet built is refused with `khi: <verb or option> is not implemented yet`.
- **Qualifiers**: `<q>` is written as the manual writes it without the slash: `P` or `FULL`. Leaving out `--qualifier` sends a bare `SAVE`.

## Output streams

| Stream | Carries |
|--------|---------|
| stdout | The controller's bytes, unaltered. `khi load` and `khi save` add their file-reference lines and the result line. |
| stderr | khi's own messages: refusals, timeouts and connection failures |

## A question during a LOAD (`khi load`)

The controller's question is printed verbatim on stdout. A question is a line `(<digit>:<label>, <digit>:<label>)`; any other unrecognised line followed by the controller going quiet for that wait's timeout, with no `>` prompt, is treated the same way. A line followed by the `>` prompt is a refusal: the load fails, exit 1. The question comes with the controller's request for the next block, and khi answers that request as KRterm answers any request, with the file's next block (FR-016; captures (c), (n), (o), (p)); it then sends nothing more of the file until the controller asks again (FR-021). khi then reads the person's answer from the console with no time limit, and sends it as typed, Enter included; a Ctrl+C that reaches khi there is discarded. khi never answers on its own (FR-021). Whichever answer is given, the load exits 1, and the result line names the answer (below). If the console closes or its input ends first, khi closes the session, exits 1, and says on stderr:

```text
khi: no answer to the controller's question; robot <name> may still be waiting on it and may need a restart
```

## Unknown lines during a SAVE (`khi save`)

A line the controller prints outside the file data that is not a captured normal line ([research R17](../research.md#r17-normal-lines-during-a-save)) is printed verbatim on stdout, as all controller text is. The save then fails: the target is untouched, the result line says `failed`, and the exit code is 1 (FR-040).

## Controller error lines (`khi load`)

When the controller reports an error, khi prints the controller's text verbatim, preceded by a file reference:

```text
<file>:<line>: <controller text>     where the controller names a line of the file
<file>: <controller text>            where it names none, or names a step rather than a line
```

`<file>` is the path as the person gave it. `<line>` is only ever the line the controller names; khi never works one out or translates a step number.

## The result line (`khi load`, `khi save`)

The last line on stdout, on success and on failure:

```text
khi <verb> <ok|failed>: robot <name>, file "<path>", <n> bytes | sent <n> of <size> bytes, programs <a, b | none>[; completed with <n> error(s); answered <answer> (<label>)[; the controller's copy differs from the file]]
```

- **Bytes**: count file content only, the bytes sent or written, never protocol traffic. When the bytes sent differ from the file's size, as after a question with file data left (capture (p)), they read `sent <n> of <size> bytes`.
- **The question part**: appears only when the controller asked a question during `khi load` and the person answered it (FR-021). The status is then always `failed`.
  - `completed with <n> error(s)`: the controller's own count, `1 error` or `<n> errors`.
  - `answered <answer> (<label>)`: what the person typed, and the controller's label for that answer, taken as its question printed it: the text after `<answer>:` in the question's list of choices, up to the next choice or the closing parenthesis. If the answer matches no choice, the parenthesis is left out.
  - `the controller's copy differs from the file`: added after the answer `0`, which keeps the program with the step made a comment (capture (n)).

Examples:

```text
khi load ok: robot cell1, file "C:\cells\cell1\pallet.as", 4182 bytes, programs a1, test
khi load failed: robot cell1, file "pallet.as", 4182 bytes, programs none
khi save ok: robot sim, file "backup\full.as", 88213 bytes, programs main, pick, place
khi load failed: robot sim, file "capn.as", 148 bytes, programs capn; completed with 1 error; answered 0 (Change to comment and continue); the controller's copy differs from the file
khi load failed: robot sim, file "capo.as", 148 bytes, programs capo; completed with 1 error; answered 1 (Delete program and abort)
khi load failed: robot sim, file "capp.as", sent 1024 of 1815 bytes, programs capp; completed with 1 error; answered 1 (Delete program and abort)
```

## Timeout and connection messages (stderr)

```text
khi: timed out waiting for <step> (robot <name>)
khi: cannot connect to robot <name> at <address>:<port>: <system reason>
khi: robot <name> closed the connection
```

`<step>` is the WaitStep wording in [data-model.md](../data-model.md#timeouts-and-waitstep).

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success, or a terminal session the person ended (Ctrl+], console closed, input ended) |
| 1 | The controller reported an error or refusal, or asked a question the person gave no answer to before the console closed or input ended |
| 2 | khi refused before connecting |
| 3 | A wait timed out |
| 4 | The connection failed, was refused, or was closed by the controller |
| 5 | A local file step failed, such as replacing the target |

## `khi term` keys

| Key | Effect |
|-----|--------|
| Any key but Ctrl+C and Ctrl+] | Sent to the controller as its byte |
| Ctrl+C | VS Code's copy. If VS Code passes it through because nothing is selected, khi discards it: never sent, and not a quit. The same in `khi load` and `khi save` (FR-041) |
| Ctrl+] | Ends the session and exits 0; never sent |
| Closing the console, or input ending | Ends the session and exits 0 |

## Robots file

`khi.robots.json` in the current folder; see [robots-file.md](robots-file.md).
