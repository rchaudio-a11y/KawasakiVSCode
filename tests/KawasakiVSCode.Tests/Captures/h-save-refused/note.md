# Capture (h): a SAVE the controller reports an error in

**Status**: Confirmed by the Architect, 2026-11-04. Not a refusal before the transfer: see observation 3.

## What was done

1. Memory before this session: `capb`, `capl` and `home`; no program `nosuch`.
2. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
3. At `login: `, the Architect typed `as` and pressed Enter.
4. At `>`, the Architect entered `SAVE/P caph.as=nosuch` and pressed Enter.
5. When `>` came back, KRterm disconnected; the relay saw the client close.
6. KRterm had written `C:\KawasakiRobot\KRterm\caph.as`. The agent moved it into `files/caph.as` and checked it was unchanged (SHA-256).

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\h-save-refused\exchange.txt
```

The relay exited 0, closed by the client: 19 records, 69 bytes toward the controller, 1,245 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (01:28)

## Payload regions

Records 14 to 16. Inside them are 17 `D` blocks, `05 02 44`, the file bytes, `17`; their payloads, in order, are the 912 bytes of `files/caph.as`, identical byte for byte.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `SAVE/P caph.as=nosuch` in one record.

Record 11 is what KRterm sent when the Architect pressed Enter on the SAVE line. Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **KRterm rewrites the SAVE line**, as in capture (f): `SAVE/P caph.as =nosuch`, with a space before `=` (record 11).
3. **No refusal before the transfer**: the controller opened the file with the `B` block and KRterm answered `B    0` (records 12, 13), as for a SAVE that works.
4. **The error comes mid-transfer**: the controller sent `Saving...(caph.as)`, the 15 `.*` header lines and the two `.NETCONF` lines as `D` blocks, then, outside any block:

   ```text
   nosuch:Variable (or program) does not exist.
   ```

   and then the `E` block (record 16). KRterm answered `E    0` (record 17).
5. **"completed" all the same**: the controller then sent `File save completed.` CR LF and `>` (records 18, 19). Unlike LOAD's completion line, SAVE's carries no error count. The error message is the only sign the SAVE failed.
6. **KRterm kept the file**: KRterm wrote `caph.as` with the header and `.NETCONF` lines and no program, 912 bytes.
7. **Reply times**: the controller answered every client record within 47 ms (record 12, the `B` block). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **SAVE/P with a program name**: 90209-1022 §5.3, SAVE/ (pages 5-30 to 5-32).
- **The message**: `Variable (or program) does not exist.` is not in 90209-1022 Appendix 4 as printed. The nearest entries are "P0152 Undefined variable (or program)." and "P0117 Invalid variable (or program) name." The controller printed the name, a colon and the text, with no code.
- **KRterm's rewriting of the SAVE line and the transfer framing**: not in 90209-1022.
