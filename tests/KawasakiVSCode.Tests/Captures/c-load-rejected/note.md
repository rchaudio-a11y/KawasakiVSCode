# Capture (c): LOAD the controller rejects, closed at the question

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. `files/capc.as` was already in `C:\KawasakiRobot\KRterm`, copied there for the first take and checked identical (SHA-256). Its step 3, file line 4, `x = 1 +`, is the syntax error.
2. K-ROSET had been restarted at about 00:04, so this session ran against a freshly started controller.
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. At `>`, the Architect pressed Ctrl+V, which KRterm sent as `16`, then entered `load capc.as` in one go and pressed Enter.
6. When the question appeared, the Architect disconnected with Com → Disconnect, pressing no key; the relay saw the client close.

The first take, kept as `c-load-rejected-ctrl-c`, was answered with an accidental Ctrl+C. A second attempt sent no LOAD and was set aside, not checked in.

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

The relay exited 0, closed by the client: 19 records, 202 bytes toward the controller, 296 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (00:07 to 00:08)

## Payload regions

Record 17, bytes 8 to 149: the 142 bytes of `files/capc.as`, identical byte for byte. Bytes 1 to 7 are the block's header, `02 43 20 20 20 20 30`, and byte 150 is `17`.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `16`, Ctrl+V, on an empty line.
- Record 11: `load capc.as` in one record.

Record 13 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **Ctrl+V on an empty line**: the controller answered `16` with `2e`, `ESC [2D`, `ESC [K`, `>` (record 10): the prompt redrawn. Nothing else changed.
3. **Enter on a LOAD line**: KRterm sent `18` (CAN), `load capc.as` and CR LF (record 13); the controller redrew the line and sent the `A` block naming `capc.as` (record 14), as in capture (b).
4. **Transfer up to the error**: KRterm's `A    0` (record 15); the `C` block with `Loading...(capc.as)` (record 16); KRterm's `C    0` with the whole file (record 17).
5. **The error and the question (record 18)**: the controller's next `C` block carried, after the ETB:

   ```text
   Program  capc()
      3 x = 1 +
               ^(P0118)Illegal variable type.

   STEP syntax error.
   (0:Change to comment and continue, 1:Delete program and abort)
   ```

   The controller names the step (3), prints its text and puts a caret under the error. It does not name the file line (4). Record 18 is byte-identical to record 40 of `c-load-rejected-ctrl-c`.
6. **KRterm did not wait for the person**: 15 ms after the question, KRterm answered the `C` block with `C    0`, `1a` (SUB), ETB (record 19). The question then waited for the person, and the session closed there with nothing more sent either way.
7. **Reply times**: the controller answered every client record within 59 ms (record 2, the telnet answer). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.
8. **The controller after the session closed at the question**: the next connection was not served at all; see `c-load-rejected-reconnect`. The same happened on the PC's first attempt, before a restart, at 00:10.

## Memory after the session closed (FR-004)

**Not produced.** K-ROSET could not show what memory held after the session closed at the question:

- A new terminal connection got no reply at all (`c-load-rejected-reconnect`).
- K-ROSET's teach pendant showed no program selected, and its Directory screen hung at "Receiving item list..." with no entries. `DIRECTORY/P` in K-ROSET's own Terminal tab got no answer.
- The only recovery found is restarting K-ROSET, and a restart empties memory: after the PC restart at 00:15, `DIRECTORY/P` listed no programs at all, `capb` from capture (b) included.

For comparison, after a Ctrl+C at the same question (`c-load-rejected-ctrl-c`), `DIRECTORY/P` showed `capc` was not kept. The Architect reports, from experience, that the controller does not keep a program that has an error; this capture could not show it either way.

## Manual citations

- **LOAD**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35).
- **The question**: §5.3 gives it as "The step format is incorrect (0: Continue load 1: Delete program and exit)". This controller printed `STEP syntax error.` and `(0:Change to comment and continue, 1:Delete program and abort)`. khi relies on the captured bytes.
- **P0118**: 90209-1022 Appendix 4, Error Message List: "P0118 Illegal variable type."
- **What happens when the session closes at the question**: not in 90209-1022.
- **Ctrl+V, CAN and the transfer framing**: not in 90209-1022; Ctrl+V is not among §2.4.1's terminal control commands.
