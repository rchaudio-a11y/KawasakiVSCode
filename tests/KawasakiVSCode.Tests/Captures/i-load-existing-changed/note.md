# Capture (i), extra: LOAD of a name already in memory, with different content

**Status**: Draft, for the Architect to confirm.

This capture settles what capture (i) could not show: whether a LOAD of a program name already in memory replaces the program or keeps it. It replaces it.

## What was done

1. Memory before this session: `capl` (capture (l)) and `capb`, loaded from `../b-load-small/files/capb.as`, whose step 2 is `TYPE "capture b"`.
2. The agent wrote `files/capi.as`, which defines the same program name, `capb`, with different content: step 2 is `TYPE "capture i"`. ASCII, CR LF line endings, 121 bytes. The agent copied it into `C:\KawasakiRobot\KRterm`, the folder KRterm's settings name (`Folder=`), and checked the copy was identical (SHA-256).
3. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
4. At `login: `, the Architect typed `as` and pressed Enter.
5. At `>`, the Architect entered `load capi.as` and pressed Enter.
6. At `>`, the Architect entered `LIST/P capb` and pressed Enter.
7. When `>` came back, KRterm disconnected; the relay saw the client close. The teach pendant showed program `capb` with step 2 `TYPE "capture i"`.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\i-load-existing-changed\exchange.txt
```

The relay exited 0, closed by the client: 25 records, 201 bytes toward the controller, 340 bytes from it. Each record is stamped on receipt; the first reads 0.000.

## Date

2026-11-04 (01:05 to 01:06)

## Payload regions

Record 15, bytes 8 to 128: the 121 bytes of `files/capi.as`, identical byte for byte. Bytes 1 to 7 are the block's header, `02 43 20 20 20 20 30`, and byte 129 is `17`.

## Typed regions

- Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter): the login, as in capture (a).
- Record 9: `load capi.as` in one record.
- Record 21: `LIST/P capb` in one record, and record 23: `0d 0a`, Enter.

Record 11 is what KRterm sent when the Architect pressed Enter on the LOAD line, as in capture (b). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **Login**: records 1 to 8 carry the same bytes as capture (a).
2. **The LOAD ran as a first LOAD does**: the `A` block named `capi.as`; `Loading...(capi.as)`; KRterm sent the whole file in one block; the controller printed `Program  capb()`, the program name inside the file, not the file name; then the `E` block and `File load completed. (0 errors)` (records 11 to 20). There is no P1033, no question and no warning that a program was replaced.
3. **The program was replaced**: `LIST/P capb` (record 25) printed:

   ```text
        .PROGRAM capb()
      1 ; khi capture (i): the same program name as capture (b), different content
      2         TYPE "capture i"
        .END
   >
   ```

   Step 2 is the new content. The teach pendant showed the same.
4. **Reply times**: the controller answered every client record within 65 ms (record 2, the telnet answer). Under the plan's rule of three times the longest gap with a 10-second floor, every wait here comes out at 10 s.

## Manual citations

- **LOAD of a name already in memory**: 90209-1022 §5.3, LOAD (page 5-34): "Attempting to load a program name that already exists in memory results in error, and execution of the LOAD command is aborted", with the NOTE "(Programs are not overwritten.)". **This controller did the opposite**: the LOAD completed with `(0 errors)` and the program in memory was replaced.
- **P1033**: 90209-1022 Appendix 4, Error Message List: "P1033 Program name already exists." Not seen.
- **LIST/P**: 90209-1022 §5.2, LIST (page 5-18): "LIST/P command displays only the contents of the specified program itself."
- **The messages and the transfer framing**: as in capture (b).
