# Capture (j): typed LOAD of a file that does not exist

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. The agent checked that no `nosuch.as` was in `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`).
2. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
3. At `login: `, the Architect typed `as` and pressed Enter.
4. At `>`, the Architect entered `load nosuch.as` and pressed Enter.
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
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\j-load-missing\exchange.txt
```

The relay exited 0, closed by the client: 16 records, 46 bytes toward the controller, 134 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (01:26)

## Payload regions

None. No file content crossed.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `load nosuch.as` in one record.

Record 11 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **The start is the same as any LOAD**: `18` (CAN), `load nosuch.as` and CR LF (record 11); the controller redrew the line and sent the `A` block naming `nosuch.as` (record 12).
3. **KRterm's answer for a missing file**: `02 41 2d 31 30 35 32 17`, that is STX, `A`, `-1052`, ETB (record 13). Where a found file gets `    0` in the five-character field, a missing one gets `-1052`. What `-1052` means to KRterm is not documented; it is recorded as sent.
4. **The controller stops there**: it sent `(P2016)Cannot open the file.` CR LF and `>`, split across records 14 to 16. There is no `C` or `E` block and no completion line.
5. **No question**: the session went on at `>`.
6. **Reply times**: the controller answered every client record within 51 ms (record 10, the echo of the LOAD line); the refusal came 0.4 ms after KRterm's `A-1052`. Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **LOAD**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35).
- **The refusal**: 90209-1022 Appendix 4, Error Message List: "P2016 Cannot open the file." The controller printed it with its code, `(P2016)`.
- **The `-1052` status and the transfer framing**: not in 90209-1022. P1052 in Appendix 4 is an unrelated message ("Because the memory was full, could not copy the program.").
