# Capture (d): SAVE/P, programs only

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. Memory before this session: `capb` (with the content of `../i-load-existing-changed/files/capi.as`), `capl` (capture (l)), and `home`, which the Architect had made as a copy of `capb` before capture (i) replaced it (observation 7).
2. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
3. At `login: `, the Architect typed `as` and pressed Enter.
4. At `>`, the Architect entered `SAVE/P capd.as` and pressed Enter. The file name was given with its extension because SAVE/P adds `.PG` when none is given (§5.3).
5. When `>` came back, KRterm disconnected; the relay saw the client close.
6. KRterm had written `C:\KawasakiRobot\KRterm\capd.as`. The agent moved it into `files/capd.as` and checked it was unchanged (SHA-256).

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\d-save-p\exchange.txt
```

The relay exited 0, closed by the client: 21 records, 54 bytes toward the controller, 66,712 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (01:09 to 01:10)

## Payload regions

Records 14 to 19, the controller's records after the `B` block. Inside them are 972 `D` blocks, `05 02 44`, the file bytes, `17`. Their payloads, taken in order, are the 62,578 bytes of `files/capd.as`, identical byte for byte. The relay's records cut across blocks wherever TCP delivered them; the blocks, not the records, carry the structure.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `SAVE/P capd.as` in one record.

Record 11 is what KRterm sent when the Architect pressed Enter on the SAVE line. Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **Enter on a SAVE line**: as for LOAD, KRterm sent `18` (CAN), the whole line `SAVE/P capd.as` and CR LF (record 11). The controller redrew the line, with `ESC [16D` for the longer line, and sent a `B` block naming the file: `05 02 42`, `capd.as`, `17` (record 12). KRterm answered `B    0` (record 13).
3. **Data flows one way**: after `B`, the controller sent `Saving...(capd.as)` CR LF and then every `D` block without waiting; KRterm sent nothing until the controller's `E` block. Then KRterm answered `E    0` (record 20) and the controller sent `File save completed.` CR LF and `>` (record 21).
4. **One `D` block per line**: each `D` block carries exactly one line of the file, CR LF included: 972 blocks for 972 lines, the longest 78 bytes. There is no block-size limit to see here, unlike LOAD's 512 bytes.
5. **Display text between blocks**: outside the blocks, the controller printed `Network Configuration data` CR LF before each `.NETCONF` line, and `Program capb`, `Program capl` and `Program home`, each CR LF, right after that program's `.PROGRAM` line. KRterm wrote none of this text to the file.
6. **The file's layout**, as KRterm wrote it:
   - 15 comment lines starting `.*`: a `.****` line, the AS software versions (for example `.*=== AS GROUP ===         : ASE_010300Z5P 2022/09/05 14:24`), `.*	default.as` (with a tab) under `[Shipment setting data]`, and another `.****` line.
   - `.NETCONF` and `.NETCONF2` lines, though SAVE/P saves programs.
   - The programs in alphabetical order, each from `.PROGRAM <name>() #<n>` to `.END`: `.PROGRAM capb() #0`, `.PROGRAM capl() #1`, `.PROGRAM home() #0`. The ` #<n>` after the parentheses is not in the files that were loaded.
   - ASCII only, CR LF on every line, no `1a` at the end.
7. **Program `home`**: the Architect made `home` outside any capture, as a copy of `capb` while `capb` still held capture (b)'s steps, intending to write a home routine later. So `home` carries `; khi capture (b): a small program, loaded and never run` and `TYPE "capture b"`, while `capb` itself, replaced in capture (i), carries `TYPE "capture i"`.
8. **Not the bytes that were loaded**: comment lines come back without their leading spaces (`  ; khi capture (l): …` was loaded; `; khi capture (l): …` was saved), while `  TYPE` lines keep theirs. A file loaded and saved again is not byte-identical.
9. **Reply times**: the controller answered every client record within 54 ms (record 2, the telnet answer). The whole save, from the `B` block to `File save completed.`, took 0.27 s. Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **SAVE/P**: 90209-1022 §5.3, SAVE/ (pages 5-31 and 5-32): SAVE/P stores the selected programs and the subroutines they call; with no program named, all programs and data in memory are saved; programs are stored in alphabetical order; with no extension, `.PG` is added.
- **The messages**: `Saving...(capd.as)`, `Network Configuration data`, `Program <name>` and `File save completed.` are not in 90209-1022. §5.3 says only that the names of the programs saved are displayed on the system terminal.
- **The file's header lines, `.NETCONF` and ` #<n>`**: not described in 90209-1022.
- **CAN and the transfer framing**: not in 90209-1022, as in capture (b).
