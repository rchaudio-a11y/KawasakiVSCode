# Capture (a): connect and login

**Status**: Confirmed by the Architect, 2026-11-03. Retaken after the relay timing fix; the retake's directions, chunks and bytes match the confirmed first take record for record, and only the times differ.

## What was done

1. KRterm connected through the relay, using its saved TCP connection to 127.0.0.1, port 9101.
2. At `login: `, the Architect typed `as` and pressed Enter.
3. At the `>` prompt nothing more was typed, and KRterm disconnected with Com → Disconnect.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`, so the login was typed.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\a-connect-login\exchange.txt
```

The relay exited 0, closed by the client: 8 records, 7 bytes toward the controller, 50 bytes from it.

## Date

2026-11-03

## Payload regions

None.

## Typed regions

Records 3 (`a`), 5 (`s`) and 7 (`0d 0a`, Enter). Records are numbered in the order they appear in `exchange.txt`.

## Observations

1. **KRterm speaks first**: record 1 is `ff fb 18`, telnet WILL TERMINAL-TYPE, before the controller sends anything.
2. **One round of negotiation**: the controller answers with `ff fd 18` (DO TERMINAL-TYPE) and `login: ` in the same record. Nothing further follows: the controller never asks for the terminal type, and KRterm never sends one.
3. **Remote echo**: the controller echoes each typed character (records 4 and 6), so the terminal does not echo locally.
4. **Enter**: KRterm sends CR LF (`0d 0a`).
5. **After login**: the controller sends LF (`0a`), then `This is AS monitor terminal "AUX2"`, CR LF, then the prompt `>` with nothing after it.
6. **Reply times**: the controller answered every client record within 52 ms (records 2, 4, 6 and 8: 52, 17, 1 and 36 ms); the longer gaps are the Architect typing. Under the plan's rule of three times the longest gap with a 10-second floor, the negotiation, login-prompt and prompt-after-login waits all come out at 10 s.
7. **On disconnect**: the controller sent nothing.
8. **Relay timing**: each record is stamped when the relay receives it, before forwarding, and the first record reads 0.000. The first take was stamped after forwarding (its first record read 0.014) and was replaced by this retake.

## Manual citations

- **The login exchange**: 90209-1022 §2.6.1 step 5 describes the `login :` message, typing `as`, and the `>` prompt. The captured bytes are `login: `, with no space before the colon.
- **The banner**: `This is AS monitor terminal "AUX2"` does not appear in 90209-1022. khi passes it through and does not rely on it.
- **Telnet negotiation**: WILL and DO TERMINAL-TYPE are not in the AS manual. They are telnet protocol (RFC 854, RFC 1091), recorded here as KRterm and the controller exchanged them.
