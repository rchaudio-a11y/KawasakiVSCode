# Capture (n): the LOAD question answered with 0

**Status**: Draft, for the Architect to confirm.

## What was done

1. The agent wrote `files/capn.as`: program `capn`, with capture (c)'s syntax error, `x = 1 +`, at step 3, file line 4. ASCII, CR LF line endings, 148 bytes, its longest line 66 characters. The agent copied it into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), and checked the copy was identical (SHA-256).
2. Memory before this session: `capb` and `home`; no `capn`.
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. At `>`, the Architect entered `load capn.as` and pressed Enter.
6. At the STEP syntax error question, the Architect typed `0` and pressed Enter.
7. At `>`, the Architect typed `list/p capn` and pressed Enter.
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
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\n-load-question-0\exchange.txt
```

The relay exited 0, closed by the client: 47 records, 231 bytes toward the controller, 530 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (02:14)

## Payload regions

Record 15, bytes 8 to 155: the 148 bytes of `files/capn.as`, identical byte for byte. Bytes 1 to 7 are the block's header, `02 43 20 20 20 20 30`, and byte 156 is `17`.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `load capn.as` in one record.
- Record 18: `30`, the answer `0`; record 20: `0d 0a`, Enter.
- Records 24 to 44, even: `list/p capn`, one character per record; record 46: `0d 0a`, Enter.

Record 11 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **Up to the question, as capture (c)**: the `A` block, `Loading...(capn.as)`, the whole file in one `C` block (records 11 to 15). The controller's next `C` block (record 16) carries `Program  capn()`, the step `3 x = 1 +`, `^(P0118)Illegal variable type.`, `STEP syntax error.` and `(0:Change to comment and continue, 1:Delete program and abort)`: byte-identical to capture (c)'s record 18 but for the program name. KRterm answered with its end-of-file `C` block, SUB (record 17), and the question waited.
3. **The answer**: 13.3 s later the client sent `30` (record 18), which the controller echoed (record 19), then `0d 0a` (record 20). The answer is the digit and Enter, sent as typed.
4. **After the answer**: 32 ms after Enter, the controller sent CR LF and the `E` block, `05 02 45 17`, in one record (21). KRterm answered `E    0` (record 22), and the controller sent `File load completed. (1 errors)` CR LF `>` (record 23).
5. **What was kept**: `list/p capn` (record 47) printed:

   ```text
        .PROGRAM capn()
      1 ; khi capture (n): syntax error at step 3, file line 4; answer 0
      2         TYPE "capture n"
      3 ;?x = 1 +
      4         TYPE "after the error"
        .END
   >
   ```

   The program was kept, with step 3 changed to a comment marked `;?`, and every other step loaded.
6. **The error count stays**: although the program is in memory, the completion line says `(1 errors)`. Under FR-017, a load answered this way exits non-zero.
7. **Lower case at the prompt**: `list/p` in lower case was accepted.
8. **Reply times**: the controller answered every client record within 61 ms (record 2, the telnet answer). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s; the wait for the answer itself has no limit (Article X, v3.0.0).

## Manual citations

- **LOAD and the question**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35): "If the operation is continued by entering "0", use the editor (Edit mode) to correct the step after the program has been loaded." This controller printed the question as `(0:Change to comment and continue, 1:Delete program and abort)`, and `0` kept the program with the step commented out.
- **P0118**: 90209-1022 Appendix 4, Error Message List: "P0118 Illegal variable type."
- **LIST/P**: 90209-1022 §5.2, LIST (page 5-18).
- **The `;?` mark on the commented step and the transfer framing**: not in 90209-1022.
