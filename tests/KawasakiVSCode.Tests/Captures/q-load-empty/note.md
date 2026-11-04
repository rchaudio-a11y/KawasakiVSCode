# Capture (q): a LOAD of an empty file

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. The agent wrote `files/capq.as`: an empty file, 0 bytes. The agent copied it into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), and checked the copy was identical (SHA-256), then removed it after the capture.
2. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
3. At `login: `, the Architect typed `as` and pressed Enter.
4. At `>`, the Architect entered `load capq.as` and pressed Enter.
5. When `>` came back, KRterm disconnected; the relay saw the client close.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab as recorded for capture (o).
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them for capture (o); not read again for this capture.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\q-load-empty\exchange.txt
```

The relay exited 0, closed by the client: 20 records, 67 bytes toward the controller, 164 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (04:26)

## Payload regions

Record 15, from byte 8: empty, as `files/capq.as` is. Bytes 1 to 7 are the block's header, `02 43 20 20 20 20 30`, and byte 8 is its closing `17`.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `load capq.as` in one record.

Record 11 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **Up to the first request, as capture (b)**: records 12 to 14 match capture (b)'s records 34 to 36 but for the file name: the `A` block naming `capq.as`, KRterm's `A    0`, and the first `C` request with `Loading...(capq.as)`.
3. **An empty block first**: KRterm answered the first request with a `C` block holding no file bytes, `02 43 20 20 20 20 30 17` (record 15). It did not send the end-of-file block in its place.
4. **Then the end-of-file block**: the controller asked again with `05 02 43 17` alone (record 16). After capture (b)'s one data block, the same request came with `Program  capb()` CR LF; here no program line came. KRterm answered with the end-of-file block, `02 43 20 20 20 20 30 1a 17` (record 17).
5. **The end, as capture (b)**: records 17 to 20 are byte-identical to capture (b)'s records 39 to 42: the end-of-file block, the `E` block, KRterm's `E    0`, and `File load completed. (0 errors)` CR LF `>`.
6. **No program reported**: the controller named no program, so khi's result line would read `programs none`; it reported the load complete with `(0 errors)`.
7. **Reply times**: the controller answered every client record within 51 ms (record 2, the telnet answer). Every wait here comes out at the 10-second floor (research R9).

## Manual citations

- **LOAD**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35).
- **The transfer framing**: not in 90209-1022.
