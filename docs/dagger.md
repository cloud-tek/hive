# Dagger 128MB `File.Contents()` Limit

## Problem

The `validate` CI workflow uses a Dagger module (`cloud-tek/dagger/dotnet/cloudtek-build`) with `--auto-apply`, which exports build outputs (compiled binaries, test results, coverage data) back to the host.

When `--auto-apply` is used, the Dagger CLI internally calls `Changeset.asPatch()` → `File.contents()` to **preview** the diff before applying it. When the patch exceeds **128MB** (134,217,728 bytes), the engine rejects it:

```
File.contents ERROR
! file size 134250496 exceeds limit 134217728
```

This is a **hard-coded, non-configurable** constant (`MaxFileContentsSize`) in [`engine/buildkit/ref.go`](https://github.com/dagger/dagger/blob/main/engine/buildkit/ref.go):

```go
MaxFileContentsSize = 128 << 20
```

The limit exists because `File.Contents()` loads the entire file into memory and serializes it as a base64-encoded GraphQL string. The Dagger team considers this an intentional guardrail ([dagger/dagger#6861](https://github.com/dagger/dagger/issues/6861)).

## Root Cause

The 128MB limit is hit in the **preview step**, not the export step. The `--auto-apply` flow is:

1. `Changeset.asPatch()` → generates a unified diff (no size limit here)
2. `File.contents()` → reads the patch into memory (**fails at 128MB**)
3. `Changeset.export()` → streams to disk via BuildKit (no size limit)

Step 2 fails before step 3 ever executes.

## Solution

Replace `--auto-apply` with an explicit `export` call in the CI workflow's dagger args:

```bash
# Before (hits 128MB limit in preview step):
dagger call build --source . ... --auto-apply

# After (bypasses preview, streams directly to disk):
dagger call build --source . ... export --path .
```

`Changeset.export()` uses BuildKit's local filesystem exporter (streaming) and completely bypasses the `File.Contents()` path. It also handles file deletions correctly.

### Where to change

The dagger args are defined in the reusable workflow:
`cloud-tek/.github/.github/workflows/dagger-cloudtek-build.yml` — replace `--auto-apply` with `export --path .`

## References

- [dagger/dagger#6861](https://github.com/dagger/dagger/issues/6861) — Maintainers confirmed the limit is intentional
- [dagger/dagger#5123](https://github.com/dagger/dagger/pull/5123) — PR that introduced the 128MB constant
- [dagger/dagger#6772](https://github.com/dagger/dagger/pull/6772) — Related fix for large file handling
- [dagger/dagger#10946](https://github.com/dagger/dagger/pull/10946) — Changeset APIs + CLI exporting
