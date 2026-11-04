# Contract: captures and the stand-in

How recorded sessions are stored, and how tests replay them (Articles IV and V). The relay writes the format, the stand-in reads it, and a person can review it in a diff.

## Folder

```text
tests/KawasakiVSCode.Tests/Captures/<id>-<slug>/
├── exchange.txt     # every byte, in both directions, in order
├── note.md          # what was done, against what, when
└── files/           # files KRterm wrote (SAVE) or read (LOAD), byte for byte
```

`<id>` is the spec's letter: `a` for connect and login, through `s` for a LOAD of a file of exactly 1,024 bytes. An extra capture beside a spec capture keeps its letter and adds to the slug, as `c-load-rejected-ctrl-c` does. The folder carries `-text` in `.gitattributes`, so Git never rewrites line endings in it.

## exchange.txt

```text
# khi capture 1
# relay listen=<port> target=<address>:<port>
C 0.000 61 73 0d                                |as.|
S 0.014 0d 0a 3e 20                             |..> |
```

| Field | Meaning |
|-------|---------|
| Direction | `C` toward the controller, `S` from it |
| Time | Seconds since the first byte, to the millisecond |
| Bytes | Hexadecimal, as many as the relay read in one go |
| `\|...\|` | Printable ASCII for review only; the hexadecimal bytes are what counts |

Lines starting with `#` are comments. The relay writes the header lines and nothing else as comments.

## note.md

Required headings and content:

| Heading | Content |
|---------|---------|
| What was done | The steps as typed, each typed line verbatim |
| Against what | K-ROSET version, the robot model and controller series (E or F) as K-ROSET reports them, and the port |
| Tools | KRterm version, and the K-IDE version for an optional K-IDE capture |
| Route | The relay command line used |
| Date | ISO date |
| Payload regions | The record ranges carrying file content, with the file in `files/` they match |
| Typed regions | The record ranges the person typed |
| Observations | What the controller said and did. For (c): what the controller's memory held after the session closed at the question. |
| Manual citations | Manual number and section for each command, qualifier and controller message seen (Article XVI) |

## The stand-in

`ControllerStandIn` is a `TcpListener` on 127.0.0.1 with a port the system assigns. It plays one capture against one client connection.

- **Replay**: For each `S` record it sends exactly the captured bytes, once the client has sent everything the capture shows before it.
- **Checking the client**: Client bytes outside the payload and typed regions must equal the captured `C` bytes. A difference fails the test and shows both sides.
- **Payload regions**: Compared with the fixture the test names (INV-1). A region starts where the note says and ends at its block's closing `17`, so a fixture of another length than the captured file still fits; the fixture gives the expected content. For a SAVE, the test checks the written file against the capture's file in `files/`.
- **After the end**: Client bytes after the capture's last record, or after a stop, are recorded and not compared.
- **Typed regions**: Taken from the test's own input.
- **Recording**: Every client line outside payload regions is recorded as a command, so the suite can compare the union against the allowed set (INV-2).
- **Faults**: The stand-in can stop at a given record, either by going silent (INV-5, INV-6, INV-10) or by closing the connection (INV-4). These faults only cut a capture short.

## Capture-only rule for tests

Every protocol test names the capture it replays. A test that writes its own controller reply fails the INV-3 gate. There is no exception: the altered-reply test the Architect allowed under INV-18 on 2026-11-03 went with `--replace` on 2026-11-04.
