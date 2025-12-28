# repository-policies.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository, in regards of repository topology and organization rules.

## Critical Rule: Project Location Policy

**ALL projects (`.csproj` files) MUST be located within a module's subfolder structure.**

Projects located directly in the repository root or in temporary folders violate this policy and must be relocated.

### Valid Project Path Pattern

```
{module-name}/{subfolder}/{ProjectName}/{ProjectName}.csproj
```

Where:
- `{module-name}` is a lowercase module folder (e.g., `hive.core`, `hive.microservices`, `hive.opentelemetry`)
- `{subfolder}` is one of: `src/`, `tests/`, or `demo/`
- `{ProjectName}` is the project directory and .csproj file name

## Required Module Structure

The repository root contains folders which represent the modules hosted by this repository. Each module folder MUST follow this structure:

- `src/` (mandatory) - Contains source code projects for the module
- `tests/` (optional) - Contains test projects for the module
- `demo/` (optional) - Contains executable demo projects for the module

### Module Structure Examples

```
hive.core
    ├── src
    │   ├── Hive.Abstractions
    │   └── Hive.Testing
    └── tests
        └── Hive.Abstractions.Tests

hive.microservices
    ├── demo
    │   ├── Hive.MicroServices.Demo.Api
    │   └── Hive.MicroServices.Demo.Job
    ├── src
    │   ├── Hive.MicroServices
    │   └── Hive.MicroServices.Api
    └── tests
        └── Hive.MicroServices.Tests

hive.opentelemetry
    ├── src
    │   └── Hive.OpenTelemetry
    └── tests (reserved for future use)
```

## Module Naming Convention

Module folder names use lowercase with dots as separators:

**CORRECT:**
- `hive.core` ✓
- `hive.microservices` ✓
- `hive.opentelemetry` ✓
- `hive.logging` ✓

**INCORRECT:**
- `Hive.Core` ✗ (wrong casing)
- `hive_core` ✗ (wrong separator)
- `HiveCore` ✗ (no separator)

## Policy Violations - Examples

### INCORRECT Project Locations

These violate the repository layout policy:

```
❌ /Hive.OpenTelemetry/Hive.OpenTelemetry.csproj
   (project at repository root, not in module)

❌ /artifacts/SomeProject/SomeProject.csproj
   (project in temporary/build artifacts folder)

❌ /results/TestProject/TestProject.csproj
   (project in temporary/test results folder)

❌ /MyProject/MyProject.csproj
   (project not assigned to any module)

❌ /hive.core/Hive.Testing/Hive.Testing.csproj
   (missing required subfolder - should be in src/ or tests/)
```

### CORRECT Project Locations

These comply with the repository layout policy:

```
✓ /hive.opentelemetry/src/Hive.OpenTelemetry/Hive.OpenTelemetry.csproj
✓ /hive.core/src/Hive.Abstractions/Hive.Abstractions.csproj
✓ /hive.core/tests/Hive.Testing.Tests/Hive.Testing.Tests.csproj
✓ /hive.microservices/demo/Hive.MicroServices.Demo.Api/Hive.MicroServices.Demo.Api.csproj
```

## Excluded Locations: Never Generate Code Here

When building the repository with `CloudTek.Build.Tool`, temporary folders `artifacts/` and `results/` are created.

**Never generate code in these directories** - they contain only build artifacts, test results, and coverage reports that are excluded from source control.

## Validation Checklist

When creating or moving projects, verify:

- [ ] Project is inside a module folder (e.g., `hive.core`, `hive.microservices`, `hive.opentelemetry`)
- [ ] Project is inside a valid subfolder (`src/`, `tests/`, or `demo/`)
- [ ] Project path follows pattern: `{module}/{subfolder}/{ProjectName}/{ProjectName}.csproj`
- [ ] Project is NOT in `artifacts/` or `results/` directories
- [ ] Project is NOT at the repository root level
- [ ] Module name uses lowercase with dot separators

## Creating New Modules

Before creating a new module folder:

1. Confirm with the user if a new module is needed
2. Verify the module doesn't logically belong to an existing module
3. Create the module folder using lowercase naming with dot separators
4. Create at minimum the `src/` subfolder structure
5. Update `Hive.sln` to include the new module and its folder structure
6. Ensure all new projects follow the required path pattern

## When Moving Projects

When relocating projects to fix policy violations:

1. Use `git mv` for proper version control tracking
2. Update the solution file (`.sln`) with new project paths
3. Update all `<ProjectReference>` elements in dependent `.csproj` files
4. Verify the build succeeds after relocation (`dotnet run tool cloudtek-build --target All --skip RunChecks`)
5. Commit all changes together to maintain repository consistency
