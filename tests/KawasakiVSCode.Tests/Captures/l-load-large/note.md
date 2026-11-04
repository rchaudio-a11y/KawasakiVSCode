# Capture (l): LOAD of a generated program of about 60 KB

**Status**: Draft, for the Architect to confirm.

## What was done

1. The agent generated `files/capl.as`: `.PROGRAM capl()`, a comment, 943 steps of the form `TYPE "capl step 00002: ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789"` (steps 2 to 944, each naming its own step number), a padding comment and `.END`, in ASCII with CR LF line endings. The padding makes the length 61,441 bytes, a prime, so no block size can divide it and the last block is partial. The content is generated only; none of the training files.
2. The agent copied it into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), and checked the copy was identical (SHA-256).
3. K-ROSET had been restarted at about 00:36, after the stall recorded in `c-load-rejected-reconnect`.
4. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
5. At `login: `, the Architect typed `as` and pressed Enter.
6. At `>`, the Architect entered `load capl.as` and pressed Enter.
7. When `>` came back, KRterm disconnected; the relay saw the client close. The teach pendant then showed program `capl` with its steps.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\l-load-large\exchange.txt
```

The relay exited 0, closed by the client: 260 records, 62,468 bytes toward the controller, 661 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (00:41 to 00:43)

## Payload regions

The odd records 15 to 255, 121 `C` blocks from KRterm. In each, bytes 1 to 7 are the header `02 43 20 20 20 20 30` and the last byte is `17`; the bytes between, taken in order across all 121 records, are the 61,441 bytes of `files/capl.as`, identical byte for byte.

- Records 15 to 253: 120 blocks of 512 file bytes each (520 bytes per record).
- Record 255: the last, partial block, 1 file byte, `0a`, the LF that ends the file.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `load capl.as` in one record.

Record 11 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a). The login answered, so the K-ROSET restart had cleared the stall.
2. **Start of the transfer**: as in capture (b), KRterm sent `18` (CAN), the line and CR LF (record 11); the controller sent the `A` block naming `capl.as` (record 12), KRterm `A    0` (record 13), and the controller the `C` block with `Loading...(capl.as)` (record 14).
3. **Block size**: KRterm sends at most 512 file bytes per `C` block. Each block is answered by the controller's next `C` block before KRterm sends another; there is never more than one block in flight.
4. **Blocks ignore line boundaries**: blocks end wherever the 512th byte falls, often mid-word; record 15 ends inside `TYPE "capl step 00008: A`.
5. **The last partial block**: after 120 full blocks, KRterm sent a block holding the 1 remaining byte (record 255), and then, as in capture (b), a block holding only `1a` (SUB) (record 257).
6. **What the controller sends between blocks**: its `C` block after the first data block carried `Program  capl()` CR LF (record 16). The 120 that followed are bare: `05 02 43 17` and nothing else. The controller does not list steps as they arrive.
7. **End**: the `E` block (record 258), KRterm's `E    0` (record 259), and `File load completed. (0 errors)` CR LF `>` (record 260).
8. **Reply times**: the controller answered every client record within 81 ms (record 2, the telnet answer); inside the transfer, within 39 ms. The 121 data blocks took 4.6 s in all. Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.
9. **Memory after the session**: the teach pendant showed program `capl`, with step 1 the comment and steps 2 onward the TYPE steps.

## Manual citations

- **LOAD**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35).
- **The messages**: `Loading...(capl.as)`, `Program  capl()` and `File load completed. (0 errors)`, as in capture (b); §5.3's example differs, and khi relies on the captured bytes.
- **Block size and the transfer framing**: not in 90209-1022, as in capture (b).
