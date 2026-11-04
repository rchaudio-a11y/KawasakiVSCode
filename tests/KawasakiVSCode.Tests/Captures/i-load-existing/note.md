# Capture (i): LOAD of a program name already in memory

**Status**: Draft, for the Architect to confirm.

## What was done

1. Memory before this session: `capl`, loaded in capture (l), and `capb`, loaded from `capb.as` a few minutes earlier in a session through the relay that was not checked in. That earlier session was byte-identical to this one, times aside.
2. `capb.as` was already in `C:\KawasakiRobot\KRterm`, copied there for capture (b) and checked identical (SHA-256). It is the file in `../b-load-small/files/capb.as`.
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. At `>`, the Architect entered `load capb.as` and pressed Enter.
6. When `>` came back, KRterm disconnected; the relay saw the client close.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\i-load-existing\exchange.txt
```

The relay exited 0, closed by the client: 20 records, 170 bytes toward the controller, 181 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (00:49)

## Payload regions

Record 15, bytes 8 to 110: the 103 bytes of `../b-load-small/files/capb.as`, identical byte for byte. Bytes 1 to 7 are the block's header, `02 43 20 20 20 20 30`, and byte 111 is `17`.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `load capb.as` in one record.

Record 11 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **No refusal**: with `capb` already in memory, the LOAD ran exactly as a first LOAD does. Records 11 to 20 carry the same bytes as records 33 to 42 of capture (b): the `A`, `C` and `E` blocks, `Loading...(capb.as)`, `Program  capb()` and `File load completed. (0 errors)`. There is no P1033 and no question.
2. **Replaced or kept is not shown**: the file loaded is identical to the program in memory, so this capture cannot tell whether the controller replaced `capb` or kept the copy it had.
3. **Speed**: from the LOAD line to `File load completed.` took 0.17 s.
4. **Reply times**: the controller answered every client record within 81 ms (record 10, the echo of the LOAD line). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **LOAD of a name already in memory**: 90209-1022 §5.3, LOAD (page 5-34): "Attempting to load a program name that already exists in memory results in error, and execution of the LOAD command is aborted", with the NOTE "(Programs are not overwritten.)". **This controller did not do that**: the LOAD completed with `(0 errors)`.
- **P1033**: 90209-1022 Appendix 4, Error Message List: "P1033 Program name already exists." Not seen in this capture.
- **The messages and the transfer framing**: as in capture (b).
