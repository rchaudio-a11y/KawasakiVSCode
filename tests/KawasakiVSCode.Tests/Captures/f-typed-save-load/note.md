# Capture (f): SAVE and LOAD typed in one session

**Status**: Draft, for the Architect to confirm.

## What was done

1. Memory before this session: `capb` (with the content of `../i-load-existing-changed/files/capi.as`), `capl` and `home`, as in captures (d), (e) and (g). During the `i-load-existing-changed` session, the Architect's screenshot showed `capb` as the program selected on the teach pendant; that is the likely reason for observation 4, not proved here.
2. `capb.as` was in `C:\KawasakiRobot\KRterm`, copied there for capture (b) and checked identical (SHA-256). It is the file in `../b-load-small/files/capb.as`.
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. At `>`, the Architect entered `SAVE/P capf.as=capb` and pressed Enter.
6. At `>`, the Architect entered `load capb.as` and pressed Enter.
7. At `>`, the Architect entered `load capb.as` again and pressed Enter.
8. When `>` came back, KRterm disconnected; the relay saw the client close.
9. KRterm had written `C:\KawasakiRobot\KRterm\capf.as`. The agent moved it into `files/capf.as` and checked it was unchanged (SHA-256).

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\f-typed-save-load\exchange.txt
```

The relay exited 0, closed by the client: 42 records, 391 bytes toward the controller, 1,705 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (01:22 to 01:23)

## Payload regions

- **SAVE**: records 14 to 16. Inside them are 21 `D` blocks, `05 02 44`, the file bytes, `17`; their payloads, in order, are the 1,034 bytes of `files/capf.as`, identical byte for byte.
- **First LOAD**: record 25, bytes 8 to 110: the 103 bytes of `../b-load-small/files/capb.as`. Bytes 1 to 7 are `02 43 20 20 20 20 30` and byte 111 is `17`.
- **Second LOAD**: record 37, the same.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `SAVE/P capf.as=capb` in one record.
- Records 19 and 31: `load capb.as` in one record each.

Records 11, 21 and 33 are what KRterm sent when the Architect pressed Enter on those lines. Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **KRterm rewrites the SAVE line**: the Architect typed `SAVE/P capf.as=capb`; after `18` (CAN), KRterm sent `SAVE/P capf.as =capb`, with a space added before `=` (record 11). The controller echoed the rewritten line and named `capf.as` in the `B` block (record 12). On the LOAD lines, KRterm resent the line as typed.
3. **SAVE of one named program**: the protocol of capture (d): `B` and `B    0`, `Saving...(capf.as)`, the `D` blocks one per line, `E` and `E    0`, `File save completed.` (records 12 to 18), in 0.20 s. The file holds the 15 `.*` header lines, `.NETCONF`, `.NETCONF2`, and `capb` only, with its capture (i) content.
4. **LOAD refused, program in use**: each LOAD ran the protocol of capture (b) to the end: `A` block, `Loading...(capb.as)`, the whole file in one `C` block, KRterm's SUB block, `E` block. But the controller's `C` block after the file (record 26) carried:

   ```text
   Program  capb()
   Cannot execute because program already in use.
   ```

   and the load ended `File load completed. (1 errors)` (record 30). The message is printed without its error code.
5. **A second attempt changes nothing**: records 31 to 42 are byte-identical to records 19 to 30.
6. **"completed" does not mean loaded**: the completion line says `File load completed.` whether or not the load succeeded. Only the error count, `(0 errors)` or `(1 errors)`, tells them apart.
7. **No question**: the refusal is a message, not a question; the session went on at `>`.
8. **Memory after the session**: `capb` keeps its capture (i) content, as far as this capture shows; nothing listed it afterwards.
9. **Reply times**: the controller answered every client record within 36 ms (record 2, the telnet answer). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **SAVE/P with a program name**: 90209-1022 §5.3, SAVE/ (pages 5-30 to 5-32): `file name=program name` selects the programs saved; the example `SAVE/L file2=pg1, pg2`.
- **LOAD**: 90209-1022 §5.3, LOAD (pages 5-34 and 5-35).
- **The refusal**: 90209-1022 Appendix 4, Error Message List: "P1014 Cannot execute because program already in use." The controller printed the text without `P1014`.
- **KRterm's rewriting of the SAVE line, CAN and the transfer framing**: not in 90209-1022.
