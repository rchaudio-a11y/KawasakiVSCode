# Capture (r): capture (p)'s kind of LOAD, the question answered with 0

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. The agent wrote `files/capr.as`: program `capr`, generated content only, laid out as capture (p)'s `capp.as`. It has a comment, `TYPE "capture r"`, then capture (c)'s syntax error `x = 1 +` at step 3, file line 4, starting at file byte 111, then `TYPE` steps 5 to 30 and `.END`. ASCII, CR LF line endings, 1,815 bytes: three full blocks of 512 and one of 279. The agent copied it into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), checked the copy was identical (SHA-256), and removed it after the capture.
2. Memory before this session: not checked.
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. At `>`, the Architect entered `load capr.as` and pressed Enter.
6. At the STEP syntax error question, the Architect typed `0` and pressed Enter.
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
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\r-load-long-error-0\exchange.txt
```

The relay exited 0, closed by the client: 29 records, 1,909 bytes toward the controller, 340 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (04:41)

## Payload regions

Each block's bytes 1 to 7 are its header, `02 43 20 20 20 20 30`, and its last byte is `17`. The file bytes, identical to `files/capr.as` byte for byte:

- Record 15, bytes 8 to 519: file bytes 1 to 512.
- Record 17, bytes 8 to 519: file bytes 513 to 1,024.
- Record 22, bytes 8 to 519: file bytes 1,025 to 1,536.
- Record 24, bytes 8 to 286: file bytes 1,537 to 1,815.

The whole file was sent.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `load capr.as` in one record.
- Record 18: `30`, the answer `0`; record 20: `0d 0a`, Enter.

Record 11 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **Up to the answer, as capture (p)**: records 12 to 14 and 16 match capture (p)'s records 14 to 16 and 18 but for the program and file name. The question came with the controller's request for the next block (record 16), and KRterm answered that request with the file's second block (record 17), 1 ms later, before any answer was typed. This session had no stray keystroke like capture (p)'s records 9 and 10, so its record numbers run two below (p)'s.
3. **The answer**: 2.9 s after the question the client sent `30` (record 18), which the controller echoed (record 19), then `0d 0a` (record 20).
4. **After `0`, the load goes on**: 16 ms after Enter, CR LF and a request for the next block, `05 02 43 17` (record 21). KRterm sent the third block (record 22); the controller asked again (record 23); KRterm sent the fourth, last block, 279 file bytes (record 24); the controller asked again (record 25); KRterm sent the end-of-file block, `02 43 20 20 20 20 30 1a 17` (record 26). No error line or program line came with these requests.
5. **The end**: the `E` block (record 27), KRterm's `E    0` (record 28), and `File load completed. (1 errors)` CR LF `>` (record 29), byte-identical to capture (p)'s record 25.
6. **What was kept**: not checked; `DIRECTORY/P` was not run. Capture (n) shows that `0` keeps the program with the step made a comment.
7. **Reply times**: the controller answered every client record within 42 ms (record 2, the telnet answer). Every wait here comes out at the 10-second floor (research R9); the wait for the answer itself has no limit (Article X).

## Manual citations

- **LOAD and the question**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35): "The step format is incorrect (0: Continue load 1: Delete program and exit)". This controller printed `(0:Change to comment and continue, 1:Delete program and abort)`, and after `0` it asked for the rest of the file.
- **P0118**: 90209-1022 Appendix 4, Error Message List: "P0118 Illegal variable type."
- **The transfer framing**: not in 90209-1022.
