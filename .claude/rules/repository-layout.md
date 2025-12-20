# repository-layout.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository, in regards of repository topology and organization rules

## Temporary Folders

When building the repository with `CloudTek.Build.Tool` temporary folders `artifacts` and `results` may be created. Code MUST NOT be generated there, as they contain only artifacts, tests and coverage results.

## Module folders

The repository root contains folders which represent the modules hosted by this repository. The folders are to contain subfolders:
- `src` (mandatory) containing source code for each module
- `tests` (optional) containing tests for each module
- `demo` (optional) containing executable projects for each module

```
hive.core
    ├── src
    └── tests
hive.microservices
    ├── demo
    ├── src
    └── tests
hive.logging
    ├── src
    └── tests
```

The repository MUST NOT contain any projects which are not assigned to a module's subfolder.