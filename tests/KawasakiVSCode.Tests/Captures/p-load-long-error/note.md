# Capture (p): a LOAD longer than one block, with the question in its first block

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. The agent wrote `files/capp.as`: program `capp`, generated content only. It has a comment, `TYPE "capture p"`, then capture (c)'s syntax error `x = 1 +` at step 3, file line 4, starting at file byte 111, then `TYPE` steps 5 to 30 and `.END`. ASCII, CR LF line endings, 1,815 bytes: three full blocks of 512 and one of 279. The agent copied it into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), and checked the copy was identical (SHA-256).
2. Memory before this session: not checked.
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. At `>`, the Architect pressed Ctrl+V by mistake, and KRterm sent its byte, `16` (record 9). This is a stray keystroke, not protocol.
6. The Architect entered `load capp.as`, which reached the relay in one record (record 11), and pressed Enter.
7. At the STEP syntax error question, the Architect typed `1` and pressed Enter.
8. When `>` came back, KRterm disconnected; the relay saw the client close.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version, read again for this capture), project RCHLab as recorded for capture (o).
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them for capture (o); not read again for this capture.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\p-load-long-error\exchange.txt
```

The relay exited 0, closed by the client: 25 records, 1,094 bytes toward the controller, 337 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (04:20)

## Payload regions

- Record 17, bytes 8 to 519: bytes 1 to 512 of `files/capp.as`, identical byte for byte. Bytes 1 to 7 are the block's header, `02 43 20 20 20 20 30`, and byte 520 is `17`.
- Record 19, bytes 8 to 519: bytes 513 to 1,024 of `files/capp.as`, identical byte for byte, with the same header and `17`.
- Bytes 1,025 to 1,815 of the file were never sent.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `16`, the Architect's stray Ctrl+V; not protocol.
- Record 11: `load capp.as` in one record.
- Record 20: `31`, the answer `1`; record 22: `0d 0a`, Enter.

Record 13 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **Records 9 and 10, a stray keystroke, not protocol**: the Architect pressed Ctrl+V by mistake. The controller answered its `16` with `2e 1b 5b 32 44 1b 5b 4b 3e` (record 10): a `.`, the cursor back two places, the line cleared, and `>` again, leaving the prompt as it was. khi never sends this, so a test replays (p) as a slice of records 1 to 8 and 11 to 25 (D4).
3. **Up to the first block, as capture (o)**: records 14 to 16 match capture (o)'s records 12 to 14 but for the program and file name.
4. **The question comes with a request**: 16 ms after the first block, the controller sent `05 02 43 17`, its request for the next block, followed in the same record by `Program  capp()`, the error lines and the question (record 18). That record matches capture (o)'s record 16 but for the name. In captures (c), (n) and (o) the question came the same way.
5. **KRterm answers that request**: 16 ms later, before any answer was typed, KRterm sent the file's second block (record 19). In captures (c), (n) and (o), where the whole file had gone in the first block, KRterm answered the same request with the end-of-file block, `02 43 20 20 20 20 30 1a 17`. Either way, KRterm sends the file's next block; the question does not change what it sends.
6. **Then nothing until the answer**: the controller sent nothing more for 18.4 s, until the Architect typed `1` (record 20), which it echoed (record 21), and Enter (record 22).
7. **After the answer, as capture (o)**: 31 ms after Enter, CR LF and the `E` block (record 23), KRterm's `E    0` (record 24), and `File load completed. (1 errors)` CR LF `>` (record 25). Records 23 to 25 are byte-identical to capture (o)'s records 21 to 23. The controller asked for no third block: the file's last 791 bytes and the end-of-file block were never sent.
8. **What was kept**: not checked; `DIRECTORY/P` was not run. Capture (o) shows that `1` left no program in memory.
9. **Reply times**: the controller answered every client record within 64 ms (record 2, the telnet answer). Every wait here comes out at the 10-second floor (research R9); the wait for the answer itself has no limit (Article X).

## Manual citations

- **LOAD and the question**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35): "The step format is incorrect (0: Continue load 1: Delete program and exit)". This controller printed `(0:Change to comment and continue, 1:Delete program and abort)`.
- **P0118**: 90209-1022 Appendix 4, Error Message List: "P0118 Illegal variable type."
- **The transfer framing**: not in 90209-1022.
