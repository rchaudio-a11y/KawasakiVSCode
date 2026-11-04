# Capture (s): a LOAD of a file of exactly 1,024 bytes

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. The agent wrote `files/caps.as`: program `caps`, generated content only, with no error: a comment, `TYPE` steps 1 to 14, a filler comment of dashes, and `.END`. ASCII, CR LF line endings, exactly 1,024 bytes: two full blocks of 512. The agent copied it into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), checked the copy was identical (SHA-256), and removed it after the capture.
2. Memory before this session: not checked.
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. Records 9 to 29 are an operator's slips, not protocol: `caps.as` entered without `load` (records 9 to 12), `load ` typed (records 13 to 22), a stray Ctrl+V (records 23 and 24), and five backspaces (records 25 to 29).
6. The Architect typed `load ` again and entered `caps.as`, in one record, then pressed Enter (records 30 to 42).
7. When `>` came back, KRterm disconnected; the relay saw the client close.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab as recorded for capture (o).
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them for capture (o); not read again for this capture.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\s-load-1024\exchange.txt
```

The relay exited 0, closed by the client: 55 records, 1,127 bytes toward the controller, 242 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (04:58)

## Payload regions

Each block's bytes 1 to 7 are its header, `02 43 20 20 20 20 30`, and its last byte is `17`. The file bytes, identical to `files/caps.as` byte for byte:

- Record 46, bytes 8 to 519: file bytes 1 to 512.
- Record 48, bytes 8 to 519: file bytes 513 to 1,024.
- Record 50, from byte 8: empty. Byte 8 is the block's closing `17`.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Records 30 to 40, client records only: `load caps.as`, typed as `l`, `o`, `a`, `d`, space, then `caps.as` in one record (record 40).

Record 42 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). A test replays this capture as a slice of records 1 to 8 and 30 to 55, leaving out the slips (D4). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **The slips**: `caps.as` alone drew `^(P0109)Invalid statement.` and the prompt (record 12). The stray Ctrl+V drew `.`, the cursor back seven places, the line cleared and `>` again (record 24), as in capture (p); the line typed so far was gone. The controller echoed none of the five backspaces that followed. None of this is protocol khi plays.
3. **Up to the first block, as capture (b)**: records 43 to 45 match capture (b)'s records 34 to 36 but for the file name, and the second request came with `Program  caps()` (record 47), as capture (b)'s did (record 38).
4. **Two full blocks**: KRterm sent 512 file bytes at each of the first two requests (records 46 and 48).
5. **Then an empty block**: at the third request (record 49), with no file bytes left, KRterm sent a `C` block holding no bytes, `02 43 20 20 20 20 30 17` (record 50), byte-identical to capture (q)'s record 15. It did not send the end-of-file block yet.
6. **Then the end-of-file block**: at the fourth request (record 51), the end-of-file block (record 52). Records 52 to 55 are byte-identical to capture (b)'s records 39 to 42, `File load completed. (0 errors)` CR LF `>` included.
7. **The rule the captures share**: each request gets the next block of up to 512 file bytes; a block shorter than 512 bytes, an empty one included, is the file's last, and the request after it gets the end-of-file block. Captures (b) (103 bytes), (l) (61,441), (q) (0), (r) (1,815) and this capture (1,024) all follow it.
8. **Reply times**: the controller answered every client record within 57 ms (record 2, the telnet answer). Every wait here comes out at the 10-second floor (research R9).

## Manual citations

- **LOAD**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35).
- **P0109**: seen only after the operator's slip; khi relies on it for nothing, so it is not cited.
- **The transfer framing**: not in 90209-1022.
