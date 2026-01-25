# dotnet-artifacts.md

All artifacts of this repository are .NET projects which are defined by their respective *.*sproj files
Project files are located in */src/**/*.*sproj

### Critical Rule: Versioning source of truth

The root of the repository contains Version.targets file which contains shared version information for all artifacts and is to be treated as the only source of truth about the artifacts' version prefix.

### Critical Rules : Packages

- Artifacts located under */src/** which are not executable projects are Packages.
- Packages MUST contain <IsPackable>true</IsPackable> in their project files.
- Packages MUST import the Version.targets file via <Import Project="$(MSBuildThisFileDirectory)..\..\..\Version.targets" /> (depends on the level on nesting)

### Critical Rules: Adding new projects

- When adding a new project to the solution, package projects need to follow the rules for package artifacts.