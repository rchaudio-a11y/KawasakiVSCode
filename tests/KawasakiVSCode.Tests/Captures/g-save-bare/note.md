# Capture (g): SAVE with no qualifier

**Status**: Draft, for the Architect to confirm.

## What was done

1. Memory before this session: `capb`, `capl` and `home`, as in captures (d) and (e).
2. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
3. At `login: `, the Architect typed `as` and pressed Enter.
4. At `>`, the Architect entered `SAVE capg.as` and pressed Enter.
5. When `>` came back, KRterm disconnected; the relay saw the client close.
6. KRterm had written `C:\KawasakiRobot\KRterm\capg.as`. The agent moved it into `files/capg.as` and checked it was unchanged (SHA-256).

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\g-save-bare\exchange.txt
```

The relay exited 0, closed by the client: 38 records, 50 bytes toward the controller, 329,246 bytes from it. Each record is stamped on receipt; the first reads 0.000. `exchange.txt` is 1.32 MB.

## Date

2026-11-04 (01:18)

## Payload regions

Records 14 to 36, the controller's records after the `B` block. Inside them are 4,312 `D` blocks, `05 02 44`, the file bytes, `17`. Their payloads, taken in order, are the 311,587 bytes of `files/capg.as`, identical byte for byte.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `SAVE capg.as` in one record.

Record 11 is what KRterm sent when the Architect pressed Enter on the SAVE line, as in capture (d). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **The same protocol as SAVE/P and SAVE/FULL**: `18` (CAN), `SAVE capg.as` and CR LF (record 11); the controller redrew the line and sent the `B` block naming `capg.as` (record 12); KRterm's `B    0` (record 13); `Saving...(capg.as)` and every `D` block, one per line, without waiting; the `E` block at the end of record 36; KRterm's `E    0` (record 37); `File save completed.` CR LF `>` (record 38).
3. **What a bare SAVE holds**: the same 15 `.*` header lines as captures (d) and (e), then `.NETCONF`, `.NETCONF2`, `.ROBOTDATA1`, `.OPE_INFO1`, `.SYSDATA`, `.CONDITION`, `.AUXDATA`, `.INTER_PANEL_D`, `.INTER_PANEL_TITLE`, `.INTER_PANEL_COLOR_D`, `.SIG_COMMENT`, and the three programs in alphabetical order, `.PROGRAM capb() #0`, `.PROGRAM capl() #1`, `.PROGRAM home() #0`.
4. **Against SAVE/FULL (capture (e))**: a bare SAVE adds `.CONDITION`, which SAVE/FULL did not hold, and leaves out the storage, maintenance and log sections (`.PGM_EDT_LOG` to `.BRKLOG`). Memory held no pose, real or string variables, so no variable sections appear.
5. **Display text between blocks**, none of it written to the file: as in capture (e) up to `SIGNAL_NAME`, with `CONDITON DATA`, spelled so, before `.CONDITION`; then `Program capb`, `Program capl` and `Program home`.
6. **The file**: 4,312 lines, the longest 388 characters; ASCII only, CR LF on every line, no `1a`.
7. **Reply times**: the controller answered every client record within 37 ms (record 2, the telnet answer). The save, from the `B` block to `File save completed.`, took 0.77 s. Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **SAVE with no qualifier**: 90209-1022 §5.3, SAVE (page 5-30): "Using the SAVE command alone stores all the five data types in one file" (program, pose variable, real variable, string variable and system data); with no program named, all programs in memory are saved; with no extension, `.as` is added.
- **The messages and section labels**: not in 90209-1022, as in capture (e).
- **The section names in the file**: not described in 90209-1022.
- **CAN and the transfer framing**: not in 90209-1022, as in capture (b).
