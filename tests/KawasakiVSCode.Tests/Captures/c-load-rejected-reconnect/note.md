# Capture (c), extra: reconnecting after a session closed at the LOAD question

**Status**: Confirmed by the Architect, 2026-11-04.

This capture backs capture (c)'s finding that the controller serves no new connection after a session closes at the LOAD question.

## What was done

1. With K-ROSET freshly started after a PC restart, KRterm connected through the relay, logged in with `as`, entered `load capc.as`, and disconnected at the STEP syntax error question, pressing no key. That session matched capture (c) byte for byte from the LOAD line on, and was not checked in. It closed at 00:26.
2. At 00:27, KRterm connected through the relay again. The relay connected to 127.0.0.1:9105, and the controller accepted the connection.
3. No `login: ` came, and the Architect typed nothing into KRterm.
4. While the connection was open, the Architect looked at K-ROSET: its Terminal tab showed only `>`, with no question; the teach pendant showed no program selected; the pendant's Directory screen hung at "Receiving item list..." with no entries. `DIRECTORY/P` typed in K-ROSET's Terminal tab got no answer either.
5. At about 00:34, KRterm disconnected; the relay saw the client close.
6. At about 00:36 the Architect restarted K-ROSET. Afterwards the controller's side of the held connection was gone from the PC's TCP table, and port 9105 was listening again.

## Against what

- K-ROSET 1.8.6.19269 (the `KrHsMain.exe` file version), project RCHLab.
- Robot R01 on controller C01: product type RS006L-A001, E controller, AS version 10300Z5P, as K-ROSET's property panel showed them.
- The controller is an E controller simulated in K-ROSET, so this capture records E-series behaviour. Manual 90209-1022 (E series) is the reference.
- Port 9105, K-ROSET's first robot.

## Tools

KRterm 1.2.8 (`C:\KawasakiRobot\KRterm\KRterm.exe`, file version 1.2.8.0), with `IsAutoLogin=0`.

## Route

```text
tools\Start-CaptureRelay.ps1 -ListenPort 9101 -TargetHost 127.0.0.1 -TargetPort 9105 -OutFile tests\KawasakiVSCode.Tests\Captures\c-load-rejected-reconnect\exchange.txt
```

The relay exited 0, closed by the client: 1 record, 3 bytes toward the controller, 0 bytes from it.

## Date

2026-11-04 (00:27 to 00:34)

## Payload regions

None.

## Typed regions

None. Record 1, `ff fb 18`, is KRterm's own opening.

## Observations

1. **No reply**: KRterm sent `ff fb 18` (WILL TERMINAL-TYPE). In about 7 minutes the controller sent nothing: no `ff fd 18` and no `login: `. In every other capture the answer came within 105 ms.
2. **The controller held the connection**: while it was open, the PC's TCP table showed the connection established on the controller's side (`asproc`, port 9105). After KRterm closed it, the controller's side stayed in CloseWait: the controller neither read the connection nor closed it.
3. **Not only the terminal**: the teach pendant's Directory hung, and K-ROSET's own Terminal tab did not answer `DIRECTORY/P`, so the stall is not limited to the AUX2 terminal. That the pending LOAD question holds it is the likely reading; nothing on the wire shows the cause.
4. **First seen before the PC restart**: at 00:10, after capture (c) closed at the question at 00:08, a connection through the relay also got no reply. KRterm sent `ff fb 18`, then `DIRECTORY/P` and five `0` keys, and the controller sent 0 bytes and held the connection in CloseWait. That log was not checked in.
5. **Recovery**: both stalls ended only with a restart: the first when the PC, and K-ROSET with it, was restarted, which emptied the controller's memory; the second when K-ROSET was restarted at 00:36, after which capture (l)'s login answered normally. Nothing else was tried.

## Manual citations

- **What the controller does when a session closes at the LOAD question**: not in 90209-1022. §5.3 gives the question and its two answers only.
- **Telnet negotiation**: WILL TERMINAL-TYPE is telnet protocol (RFC 854, RFC 1091), as in capture (a).
