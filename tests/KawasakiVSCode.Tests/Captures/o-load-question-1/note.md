# Capture (o): the LOAD question answered with 1

**Status**: Draft, for the Architect to confirm.

## What was done

1. The agent wrote `files/capo.as`: program `capo`, with capture (c)'s syntax error, `x = 1 +`, at step 3, file line 4. ASCII, CR LF line endings, 148 bytes, its longest line 66 characters. The agent copied it into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), and checked the copy was identical (SHA-256).
2. Memory before this session: `capb`, `capn` (capture (n)) and `home`; no `capo`.
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. At `>`, the Architect entered `load capo.as` and pressed Enter.
6. At the STEP syntax error question, the Architect typed `1` and pressed Enter.
7. At `>`, the Architect entered `DIRECTORY/P` and pressed Enter.
8. When `>` came back, KRterm disconnected; the relay saw the client close.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\o-load-question-1\exchange.txt
```

The relay exited 0, closed by the client: 27 records, 231 bytes toward the controller, 381 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (02:19 to 02:20)

## Payload regions

Record 15, bytes 8 to 155: the 148 bytes of `files/capo.as`, identical byte for byte. Bytes 1 to 7 are the block's header, `02 43 20 20 20 20 30`, and byte 156 is `17`.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `load capo.as` in one record.
- Record 18: `31`, the answer `1`; record 20: `0d 0a`, Enter.
- Record 24: `DIRECTORY/P` in one record; record 26: `0d 0a`, Enter.

Record 11 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **Up to the question, as capture (n)**: records 11 to 14, 16 and 17 match capture (n)'s but for the program and file name, the question (record 16) included. Record 15 carries this capture's own file.
3. **The answer**: 10.2 s after the question the client sent `31` (record 18), which the controller echoed (record 19), then `0d 0a` (record 20).
4. **After the answer, as capture (n)**: 17 ms after Enter, CR LF and the `E` block (record 21), KRterm's `E    0` (record 22), and `File load completed. (1 errors)` CR LF `>` (record 23). Records 21 to 23 are byte-identical to capture (n)'s: on the wire, the answer `1` and the answer `0` end the same way.
5. **What was kept**: `DIRECTORY/P` (record 27) listed `Program`, then ` capb      capn      home   `. `capo` was not kept, as "Delete program and abort" says; `capn` from capture (n) is still there.
6. **Only memory tells the answers apart**: the completion line is `(1 errors)` after either answer. Under FR-017, a load answered `1` exits non-zero, as one answered `0` does.
7. **Reply times**: the controller answered every client record within 48 ms (record 2, the telnet answer). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s; the wait for the answer itself has no limit (Article X, v3.0.0).

## Manual citations

- **LOAD and the question**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35): "The step format is incorrect (0: Continue load 1: Delete program and exit)". This controller printed `(0:Change to comment and continue, 1:Delete program and abort)`, and `1` left no `capo` in memory.
- **P0118**: 90209-1022 Appendix 4, Error Message List: "P0118 Illegal variable type."
- **DIRECTORY/P**: 90209-1022 §5.2, DIRECTORY (page 5-17).
- **The transfer framing**: not in 90209-1022.
