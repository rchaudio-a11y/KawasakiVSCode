# Contract: templates/tasks.json

The tasks a program repository copies into `.vscode/tasks.json` (FR-029 to FR-031). Each task runs `khi` from the workspace folder, which is where it reads `khi.robots.json`.

## Tasks

| Label | Command | Problem matcher |
|-------|---------|-----------------|
| khi: Load current file | `khi load "${file}"` | both patterns below |
| khi: Save controller to file | `khi save "${input:khiSaveFile}" ${input:khiSaveQualifier}` | none |
| khi: Open terminal | `khi term` | none |

**The load task takes input**: its `presentation` reveals and focuses its terminal (`"reveal": "always"`, `"focus": true`), so the person can answer a question the controller asks during the load (FR-021).

## Inputs

- **`khiSaveFile`**: a `promptString` for the file name, relative to the workspace folder.
- **`khiSaveQualifier`**: a `pickString` offering exactly the supported forms:

| Option label | Passes |
|--------------|--------|
| full | `--qualifier FULL` |
| programs only | `--qualifier P` |
| programs and data (bare SAVE) | nothing |

## Problem matcher

```json
{
  "owner": "khi",
  "fileLocation": ["autoDetect", "${workspaceFolder}"],
  "severity": "error",
  "pattern": [
    {
      "regexp": "^((?:[A-Za-z]:)?[^:*?\"<>|\\r\\n]*\\.[A-Za-z0-9]+):(\\d+): (.*)$",
      "file": 1, "line": 2, "message": 3
    }
  ]
}
```

A second matcher with the same owner and location settings catches the file-only form, using `"kind": "file"`:

```json
"regexp": "^((?:[A-Za-z]:)?[^:*?\"<>|\\r\\n]*\\.[A-Za-z0-9]+): (.*)$",
"file": 1, "message": 2, "kind": "file"
```

**Why it is anchored on a path with an extension**: the result line starts `khi load`, which has no dot before its colon, and ordinary controller lines such as `Loading done.` have no colon. Windows file names cannot contain a colon, so the drive letter is the only one before the separator.

**Acceptance check**: a rejected load lands in the Problems list on the named line, or on the file where no line is named (User Story 6).
