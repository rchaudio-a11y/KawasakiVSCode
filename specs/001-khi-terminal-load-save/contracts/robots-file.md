# Contract: khi.robots.json

The workspace's controllers. khi reads this file from the current folder only; a VS Code task runs there by default. Field rules are in [data-model.md](../data-model.md#robots).

## Shape

```json
{
  "default": "sim",
  "robots": [
    { "name": "sim",   "address": "127.0.0.1",   "port": 9105, "simulator": true  },
    { "name": "cell1", "address": "192.168.0.2", "port": 23,   "simulator": false }
  ]
}
```

| Key | Required | Rule |
|-----|----------|------|
| `robots` | yes | A list with at least one entry |
| `robots[].name` | yes | Non-empty; unique ignoring case. `--robot` and `default` match a name ignoring case too |
| `robots[].address` | yes | A host name or IP address |
| `robots[].port` | yes | A whole number from 1 to 65535 |
| `robots[].simulator` | yes | `true` or `false`; no behaviour depends on it in this feature |
| `default` | no | Must be one of the names |

Unknown keys are refused by name, so a misspelt key is reported instead of silently ignored.

## Refusals (exit 2, before connecting)

```text
khi: no khi.robots.json in <folder>
khi: khi.robots.json: <what is wrong and where>
khi: unknown robot <name>; robots are: <names>
khi: no --robot given and no default set; robots are: <names>
```

The addresses in the example above are example values for this document. The 9105 is the K-IDE manual's K-ROSET port. No address, port or credential appears in code (Article IX); tests write their own file pointing at the stand-in's port.
