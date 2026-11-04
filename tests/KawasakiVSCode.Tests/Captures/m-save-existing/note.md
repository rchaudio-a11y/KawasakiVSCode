# Capture (m): SAVE onto a file name that already exists

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. The agent wrote `files/before/capm.as`, a 96-byte file whose only program is `capmold`, and copied it into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), checking the copy was identical (SHA-256). No `capm.bas` was in the folder.
2. Memory before this session: `capb` (with the content of `../i-load-existing-changed/files/capi.as`) and `home`.
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. At `>`, the Architect entered `SAVE/P capm.as=capb` and pressed Enter.
6. When `>` came back, KRterm disconnected; the relay saw the client close.
7. KRterm's folder then held `capm.as` (1,034 bytes) and `capm.Bas` (96 bytes). The agent moved both into `files/` and checked each was unchanged (SHA-256).

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\m-save-existing\exchange.txt
```

The relay exited 0, closed by the client: 19 records, 65 bytes toward the controller, 1,347 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (02:11 to 02:12)

## Payload regions

Records 14 to 17. Inside them are 21 `D` blocks, `05 02 44`, the file bytes, `17`; their payloads, in order, are the 1,034 bytes of `files/capm.as`, identical byte for byte.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `SAVE/P capm.as=capb` in one record.

Record 11 is what KRterm sent when the Architect pressed Enter on the SAVE line, as in capture (f). Records are numbered in the order they appear in `exchange.txt`.

## Files

| File | What it is |
|------|------------|
| `files/before/capm.as` | The file in KRterm's folder before the SAVE |
| `files/capm.as` | The file KRterm wrote from the SAVE |
| `files/capm.Bas` | The old file, renamed by KRterm; byte-identical to `files/before/capm.as` |

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **The protocol does not change**: with `capm.as` already in the folder, the exchange is that of capture (f)'s SAVE: `18` (CAN) and the rewritten line `SAVE/P capm.as =capb` (record 11), the `B` block naming `capm.as` and KRterm's `B    0` (records 12, 13), the `D` blocks, the `E` block and `E    0` (records 17, 18), and `File save completed.` (record 19). Nothing on the wire mentions the existing file, a rename or `.bas`.
3. **No dialog**: 11 s passed between the typed line's echo (record 10) and KRterm sending it (record 11), where other captures show about 2 s. The Architect confirmed KRterm showed no popup, so the pause came before Enter was pressed: KRterm sends the line only on Enter. KRterm did not ask before replacing the existing file.
4. **KRterm renames on the PC**: the old file became `capm.Bas`, with a capital `B`, its bytes unchanged. The controller had no part in it.
5. **The same program, the same bytes**: `files/capm.as` is byte-identical to capture (f)'s `capf.as`, a SAVE/P of the same program 50 minutes earlier. SAVE/P of an unchanged program wrote no line that differs between runs.
6. **Reply times**: the controller answered every client record within 35 ms. Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **SAVE/P with a program name**: 90209-1022 §5.3, SAVE/ (pages 5-30 to 5-32).
- **The rename**: 90209-1022 §5.3, SAVE, NOTE (page 5-30): "If the specified file name already exists in memory, then the existing file is automatically renamed with a "b" in front of the file extension", giving `file1.bas`. This capture shows the rename done by KRterm on the PC, as `.Bas`, with nothing in the protocol.
- **KRterm's rewriting of the SAVE line and the transfer framing**: not in 90209-1022.
