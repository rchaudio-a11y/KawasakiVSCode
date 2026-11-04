# Capture (c), extra: Ctrl+C at the LOAD question

**Status**: Confirmed by the Architect, 2026-11-04.

This session was taken as capture (c) and kept, by the Architect's ruling of 2026-11-03, as an extra capture. At the controller's question the Architect pressed Ctrl+C to copy the screen text, and KRterm sent it to the controller as `03`, so the session did not close at the question as (c) requires. Capture (c) was retaken in `c-load-rejected`.

## What was done

1. The agent copied `files/capc.as` into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), and checked the copy was identical (SHA-256).
2. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
3. At `login: `, the Architect typed `as` and pressed Enter.
4. At `>`, the Architect typed `load capc.as` and pressed Enter.
5. At the question, the Architect pressed Ctrl+C, meaning to copy the screen.
6. At `>`, the Architect entered `DIRECTORY/P` and pressed Enter.
7. KRterm disconnected; the relay saw the client close.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\c-load-rejected\exchange.txt
```

The log was then moved here. The relay exited 0, closed by the client: 51 records, 223 bytes toward the controller, 368 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-03

## Payload regions

Record 39, bytes 8 to 149: the 142 bytes of `files/capc.as`, identical byte for byte. Bytes 1 to 7 are the block's header, `02 43 20 20 20 20 30`, and byte 150 is `17`.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Records 9 to 31, odd: `load capc.as`, one character per record.
- Record 42: `03`, Ctrl+C.
- Record 47: `DIRECTORY/P` in one record, and record 49: `0d 0a`, Enter.

Record 33 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **Enter on a LOAD line**: as in capture (b), KRterm sent `18` (CAN), the whole line and CR LF (record 33). Here the controller's answer came in two records: `2e`, `ESC [14D`, `ESC [K` (record 34), then `>load capc.as` CR LF and the `A` block naming `capc.as` (record 35). The same bytes arrive in different chunks from one session to the next.
3. **Transfer up to the error**: the `A` block and KRterm's `A    0` (records 35, 36); the `C` block with `Loading...(capc.as)`, which arrived split across records 37 and 38; KRterm's `C    0` with the whole file (record 39).
4. **The error and the question (record 40)**: the controller's next `C` block (`05 02 43 17`) carried, after the ETB:

   ```text
   Program  capc()
      3 x = 1 +
               ^(P0118)Illegal variable type.

   STEP syntax error.
   (0:Change to comment and continue, 1:Delete program and abort)
   ```

   The controller names the step (3) and prints the step's text, with a caret under the position of the error. It does not name the file line (4).
5. **KRterm did not wait for the person**: 15 ms after the question, KRterm answered the `C` block with `C    0`, `1a` (SUB), ETB (record 41), as it ends a file. The question then waited for the person.
6. **Ctrl+C at the question**: 59 s later the client sent `03` (record 42). The controller sent `2e`, `ESC [2D`, `ESC [K`, CR LF (record 43), then the `E` block (record 44). KRterm answered `E    0` (record 45), and the controller sent `File load completed. (1 errors)` CR LF and `>` (record 46).
7. **Memory after the Ctrl+C**: `DIRECTORY/P` listed `Program` and ` capb   ` only (records 50, 51). `capc` was not kept, either whole or with step 3 changed to a comment. `File load completed. (1 errors)` was printed all the same, so that line does not show that a program was loaded.
8. **Reply times**: the controller answered every client record within 52 ms (record 28, an echo); the question came 21 ms after the file (record 40). The question then waits for the person.

## Manual citations

- **LOAD**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35).
- **The question**: §5.3 gives it as "The step format is incorrect (0: Continue load 1: Delete program and exit)". This controller printed `STEP syntax error.` and `(0:Change to comment and continue, 1:Delete program and abort)`. khi relies on the captured bytes.
- **P0118**: 90209-1022 Appendix 4, Error Message List: "P0118 Illegal variable type." The controller printed it as `(P0118)Illegal variable type.`
- **Ctrl+C**: §2.4.1 lists Ctrl+C among the terminal control commands. Its effect at the LOAD question is shown here and is not described in §5.3.
- **DIRECTORY/P**: 90209-1022 §5.2, DIRECTORY (page 5-17): lists program names.
- **CAN and the transfer framing**: not in 90209-1022, as in capture (b).
