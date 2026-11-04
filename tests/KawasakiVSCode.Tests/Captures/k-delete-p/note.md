# Capture (k): DELETE/P on a present, an absent and a refused program

**Status**: Confirmed by the Architect, 2026-11-04.

## What was done

1. Memory before this session: `capb`, `capl` and `home`. `home` is the Architect's and was not touched. `capb` was the program selected on the teach pendant, as the Architect confirmed on 2026-11-04.
2. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
3. At `login: `, the Architect typed `as` and pressed Enter.
4. At `>`, the Architect entered `DELETE/P capl` and pressed Enter; at the confirmation, typed `1` and pressed Enter.
5. At `>`, the Architect entered `DELETE/P nosuch` and pressed Enter; at the confirmation, typed `1` and pressed Enter. No program `nosuch` existed.
6. At `>`, the Architect entered `DELETE/P capb` and pressed Enter; at the confirmation, typed `1` and pressed Enter.
7. At `>`, the Architect entered `DIRECTORY/P` and pressed Enter.
8. When `>` came back, KRterm disconnected; the relay saw the client close.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed. The KRterm capture FR-036 requires; no K-IDE capture was taken.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\k-delete-p\exchange.txt
```

The relay exited 0, closed by the client: 36 records, 76 bytes toward the controller, 346 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (01:30 to 01:32)

## Payload regions

None. No file content crossed.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Records 9, 17, 25 and 33: `DELETE/P capl`, `DELETE/P nosuch`, `DELETE/P capb` and `DIRECTORY/P`, one record each.
- Records 11, 19, 27 and 35: `0d 0a`, Enter on those lines.
- Records 13, 21 and 29: `1`, the answer to each confirmation; records 15, 23 and 31: `0d 0a`, Enter after it.

Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **KRterm does not step in**: on a DELETE/P line, KRterm sent plain CR LF; there is no `18` (CAN) and no resent line, as there is for LOAD and SAVE.
3. **The confirmation**: after every DELETE/P, before anything else, the controller sent CR LF and `Are you sure ? (Yes:1, No:0) ` with a trailing space and no line end (records 12, 20 and 28, byte-identical):

   ```text
   0d 0a 41 72 65 20 79 6f 75 20 73 75 72 65 20 3f 20 28 59 65 73 3a 31 2c 20 4e 6f 3a 30 29 20
   ```

   The controller echoed the `1` (records 14, 22, 30). The answer is `1`, then CR LF.
4. **The confirmation comes even for an absent program**: the controller asked before saying the program did not exist.
5. **Present, deleted (`capl`)**: after the answer, the controller sent CR LF and `>` (record 16). No message.
6. **Absent (`nosuch`)**: after the answer, `nosuch:Variable (or program) does not exist.` CR LF `>` (record 24). This is the same text as in capture (h), and **not** "P1038 Program does not exist."
7. **Refused (`capb`, selected on the pendant)**: after the answer, CR LF, `Program`, CR LF, `capb             KILL or PCKILL to delete program.` CR LF `>` (record 32). The message is printed without its code.
8. **Memory after the session**: `DIRECTORY/P` listed `Program`, then ` capb      home   ` (record 36): `capl` was deleted, `capb` was kept, `home` untouched.
9. **Reply times**: the controller answered every client record within 66 ms (record 2, the telnet answer). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **DELETE/P**: 90209-1022 §5.2, DELETE (page 5-19): "DELETE/P command, unlike the DELETE command, deletes only the program itself, and not the subroutines and variables used by that program"; "Those programs that are in execution or selected cannot be deleted." The confirmation is not described there.
- **The confirmation**: §5.2 does not show it. Other sections of 90209-1022 show similar questions, such as "Are you sure? (Yes:1,No;0)"; this controller printed `Are you sure ? (Yes:1, No:0) `.
- **The absent program**: 90209-1022 Appendix 4 lists "P1038 Program does not exist."; this controller printed `nosuch:Variable (or program) does not exist.` instead, with no code.
- **The refusal**: 90209-1022 Appendix 4: "P1018 KILL or PCKILL to delete program." The controller printed the text without `P1018`. KILL is in §5.4 (page 5-44) and PCKILL in the PC program commands.
- **DIRECTORY/P**: 90209-1022 §5.2, DIRECTORY (page 5-17).
