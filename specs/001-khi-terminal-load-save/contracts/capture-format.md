# Contract: captures and the stand-in

How recorded sessions are stored, and how tests replay them (Articles IV and V). The relay writes the format, the stand-in reads it, and a person can review it in a diff.

## Folder

```text
tests/KawasakiVSCode.Tests/Captures/<id>-<slug>/
├── exchange.txt     # every byte, in both directions, in order
├── note.md          # what was done, against what, when
└── files/           # files KRterm wrote (SAVE) or read (LOAD), byte for byte
```

`<id>` is the spec's letter: `a` for connect and login, through `k` for DELETE/P. The folder carries `-text` in `.gitattributes`, so Git never rewrites line endings in it.

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
- **Payload regions**: Compared with the fixture the test names (INV-1). For a SAVE, the test checks the written file against the capture's file in `files/`.
- **Typed regions**: Taken from the test's own input.
- **Recording**: Every client line outside payload regions is recorded as a command, so the suite can compare the union against the allowed set (INV-2).
- **Faults**: The stand-in can stop at a given record, either by going silent (INV-5, INV-6, INV-10) or by closing the connection (INV-4, INV-16). These faults only cut a capture short.
- **Altered reply (INV-18 only)**: The stand-in can replace the controller's reply to one DELETE/P in capture (k) with different bytes. A test using it must assert that khi stops: no LOAD and nothing further sent. This is the only case where the stand-in sends bytes no capture holds.

## Capture-only rule for tests

Every protocol test names the capture it replays. A test that writes its own controller reply fails the INV-3 gate. The one exception is an altered-reply test under INV-18. It asserts only that khi stops on bytes it does not recognise, never bytes khi waits for, so Article IV still holds, as the Architect ruled on 2026-11-03.
