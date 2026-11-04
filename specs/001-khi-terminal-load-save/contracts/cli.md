# Contract: the khi command line

What the person and VS Code tasks see. The behaviour behind it is in [spec.md](../spec.md), and the field rules are in [data-model.md](../data-model.md).

## Synopsis

```text
khi term [--robot <name>]
khi load <file> [--robot <name>]
khi save <file> [--qualifier <q>] [--robot <name>]
```

- **Options**: An option may appear before or after `<file>`. Each option may appear once.
- **Refused before connecting**, each by name: an unknown verb, an unknown option, an option the verb doesn't take, a repeated option, a missing `<file>`, an extra argument, and an unsupported qualifier.
- **Unwired verbs and options**: any not yet built is refused with `khi: <verb or option> is not implemented yet`.
- **Qualifiers**: `<q>` is written as the manual writes it without the slash: `P` or `FULL`. Leaving out `--qualifier` sends a bare `SAVE`.

## Output streams

| Stream | Carries |
|--------|---------|
| stdout | The controller's bytes, unaltered. `khi load` and `khi save` add their file-reference lines and the result line. |
| stderr | khi's own messages: refusals, timeouts and connection failures |

## A question during a LOAD (`khi load`)

The controller's question is printed verbatim on stdout. khi then reads the person's answer from the console with no time limit, and sends it as typed, Enter included; a Ctrl+C that reaches khi there is discarded. khi never answers on its own (FR-021). If the console closes or its input ends first, khi closes the session, exits 1, and says on stderr:

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
khi <verb> <ok|failed>: robot <name>, file "<path>", <n> bytes, programs <a, b | none>
```

- **Bytes**: count file content only, the bytes sent or written, never protocol traffic.

Examples:

```text
khi load ok: robot cell1, file "C:\cells\cell1\pallet.as", 4182 bytes, programs a1, test
khi load failed: robot cell1, file "pallet.as", 4182 bytes, programs none
khi save ok: robot sim, file "backup\full.as", 88213 bytes, programs main, pick, place
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
