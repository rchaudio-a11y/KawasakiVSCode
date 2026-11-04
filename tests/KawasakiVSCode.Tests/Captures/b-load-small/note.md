# Capture (b): LOAD of a small program

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. The agent copied `files/capb.as` into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), and checked the copy was identical (SHA-256).
2. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
3. At `login: `, the Architect typed `as` and pressed Enter.
4. At `>`, the Architect typed `load capb.as` and pressed Enter.
5. When `>` came back, KRterm disconnected; the relay saw the client close.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\b-load-small\exchange.txt
```

The relay exited 0, closed by the client: 42 records, 170 bytes toward the controller, 181 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-03

## Payload regions

Record 37, bytes 8 to 110: the 103 bytes of `files/capb.as`, identical byte for byte. Bytes 1 to 7 are the block's header, `02 43 20 20 20 20 30`, and byte 111 is `17`.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Records 9 to 31, odd: `load capb.as`, one character per record.

Record 33 is not typed as such: it is what KRterm sent when the Architect pressed Enter on the LOAD line (observation 3). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **Typing**: the controller echoed each character of `load capb.as` (records 10 to 32).
3. **Enter on a LOAD line**: KRterm did not send CR LF after the typed characters. It sent `18` (CAN), then the whole line `load capb.as`, then CR LF, in one record (33). The controller answered (record 34) with `2e`, then `ESC [14D` and `ESC [K`, then `>load capb.as` CR LF: the line on screen was cleared and redrawn as the controller received it the second time.
4. **Transfer framing**: each controller block starts `05 02` (ENQ STX), then a letter, and ends `17` (ETB). KRterm answers each with `02` (STX), the same letter, the five characters `    0` (four spaces and `0`), and `17`. The exchange, in order:

   | Records | Controller sent | KRterm answered |
   |---------|-----------------|-----------------|
   | 34, 35 | `A`, the file name `capb.as` | `A    0` |
   | 36, 37 | `C`, then `Loading...(capb.as)` CR LF after the ETB | `C    0`, the whole file, ETB |
   | 38, 39 | `C`, then `Program  capb()` CR LF after the ETB (two spaces) | `C    0`, `1a` (SUB), ETB |
   | 40, 41 | `E` | `E    0` |
   | 42 | `File load completed. (0 errors)` CR LF, then `>` | — |

   The meaning of the letters and of the `    0` field is not documented in 90209-1022; this capture shows only what was sent. Every `    0` field here reads 0.
5. **One data block**: KRterm sent the whole 103-byte file in one `C` block, and ended the file with a `C` block holding only SUB. How KRterm splits a file larger than one block is not shown by this capture.
6. **File content unchanged**: the file's CR LF line endings went across as they are in the file. The file holds no `ff` byte, so the question whether telnet doubles one inside file content stays open.
7. **Case**: the Architect typed `load` in lower case and the controller accepted it.
8. **Reply times**: the controller answered every client record within 66 ms (record 16, an echo). The longest reply in the transfer was 64 ms (record 34, the answer to the LOAD line). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.
9. **After the session**: `capb` is in the controller's memory, as the completion message reports. No command in this capture lists memory.

## Manual citations

- **LOAD**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35). LOAD loads programs and variables from a file on the PC; `.as` is added when no extension is given; loading a program name already in memory is an error, and the LOAD is aborted.
- **The messages**: §5.3's example shows `Loading...`, `Program a1()` and `Loading done.`. This controller printed `Loading...(capb.as)`, `Program  capb()` with two spaces, and `File load completed. (0 errors)`. khi relies on the captured bytes, not the manual's example.
- **CAN before the line, and the transfer framing**: not in 90209-1022. `18` is not among §2.4.1's terminal control commands. The bytes are named here by their ASCII names (CAN, ENQ, STX, ETB, SUB) and recorded as KRterm and the controller exchanged them.
