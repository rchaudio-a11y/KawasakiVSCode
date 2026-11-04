# Capture (e): SAVE/FULL, all data that can be saved

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. Memory before this session: `capb` (with the content of `../i-load-existing-changed/files/capi.as`), `capl` (capture (l)) and `home` (the Architect's copy of `capb` from before capture (i)), as in capture (d).
2. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
3. At `login: `, the Architect typed `as` and pressed Enter.
4. At `>`, the Architect entered `SAVE/FULL cape.as` and pressed Enter.
5. When `>` came back, KRterm disconnected; the relay saw the client close.
6. KRterm had written `C:\KawasakiRobot\KRterm\cape.as`. The agent moved it into `files/cape.as` and checked it was unchanged (SHA-256).

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\e-save-full\exchange.txt
```

The relay exited 0, closed by the client: 121 records, 60 bytes toward the controller, 394,969 bytes from it. Each record is stamped on receipt; the first reads 0.000. `exchange.txt` is 1.58 MB.

## Date

2026-11-04 (01:14)

## Payload regions

Records 15 to 119, the controller's records after the `B` block. Inside them are 5,425 `D` blocks, `05 02 44`, the file bytes, `17`. Their payloads, taken in order, are the 372,831 bytes of `files/cape.as`, identical byte for byte. The relay's records cut across blocks wherever TCP delivered them.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login.
- Record 10: `SAVE/FULL cape.as` in one record.

Record 12 is what KRterm sent when the Architect pressed Enter on the SAVE line, as in capture (d). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: the bytes in each direction are the same as capture (a)'s, but the controller's last login reply arrived in two records here, `0a` (record 8) and the banner with `>` (record 9). The same bytes can arrive in different chunks.
2. **The same protocol as SAVE/P**: `18` (CAN), the line and CR LF (record 12); the `B` block naming `cape.as` and KRterm's `B    0` (records 13, 14); `Saving...(cape.as)`, then every `D` block without waiting; the `E` block, KRterm's `E    0` (record 120), and `File save completed.` CR LF `>` (record 121).
3. **One `D` block per line, of any length**: 5,425 blocks for 5,425 lines. The longest line is 511 characters, 513 bytes with CR LF, and went in one block. The 512-byte limit seen in LOAD (capture (l)) does not apply in this direction.
4. **Display text between blocks**, none of it written to the file: `Network Configuration data` (twice), `ROBOT DATA`, `OPERATION INFORMATION DATA`, `SYSTEM DATA`, `AUX DATA`, `INTERFACE PANEL DATA`, `INTERFACE PANEL PAGE TITLE`, `INTERFACE PANEL COLOR DATA`, `SIGNAL_NAME`, `STORAGE DATA`, `MAINTENANCE DATA`, and `Program capb`, `Program capl`, `Program home`. The log sections that follow `MAINTENANCE DATA` have no label.
5. **The file's layout**: the same 15 `.*` header lines as capture (d), then these sections in order, each closed by `.END` except the two `.NETCONF` lines: `.NETCONF`, `.NETCONF2`, `.ROBOTDATA1`, `.OPE_INFO1`, `.SYSDATA`, `.AUXDATA`, `.INTER_PANEL_D`, `.INTER_PANEL_TITLE`, `.INTER_PANEL_COLOR_D`, `.SIG_COMMENT`, `.PGM_EDT_LOG`, `.STORAGE`, `.MAINTENANCE`, `.ERRLOG`, `.TP_TASK_SEQ`, `.ENCTEMPLOG`, `.OPELOG`, `.EXECPGLOG`, `.POSELOG`, `.FLTLOG`, `.ID`, `.BRKLOG`, then the three programs in alphabetical order with headers `.PROGRAM capb() #0`, `.PROGRAM capl() #1` and `.PROGRAM home() #0`. ASCII only, CR LF on every line, no `1a`, no `ff`.
6. **Program headers**: the only lines starting `.PROGRAM` are the three program headers. No section line begins with `.PROGRAM`, so a reader of `.PROGRAM` lines finds exactly the programs.
7. **Speed**: from the `B` block to `File save completed.` took 0.89 s for 373 KB.
8. **Reply times**: the controller answered every client record within 68 ms (record 2, the telnet answer). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **SAVE/FULL**: 90209-1022 §5.3, SAVE/ (pages 5-31 and 5-32): SAVE/FULL "Stores all data that can be saved" and cannot be combined with other SAVE/ qualifiers; with no extension, `.AS` is added for all data.
- **The messages and section labels**: `Saving...(cape.as)`, the labels in observation 4 and `File save completed.` are not in 90209-1022.
- **The section names in the file**: not described in 90209-1022.
- **CAN and the transfer framing**: not in 90209-1022, as in capture (b).
