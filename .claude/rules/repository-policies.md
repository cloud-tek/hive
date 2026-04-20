# repository-policies.md

## Project Location Policy

All `.csproj` files MUST follow: `{module}/{src|tests|demo}/{ProjectName}/{ProjectName}.csproj`

- Module names: lowercase with dots (e.g., `hive.core`, `hive.extensions`, `hive.microservices`)
- Subfolders: `src/` (mandatory), `tests/` (optional), `demo/` (optional)
- NEVER place projects at the repository root, in `artifacts/`, or in `results/`

## When Creating or Moving Projects

1. Confirm new modules with the user; prefer existing modules when possible
2. Use `git mv` for relocations
3. Update `Hive.sln` (`dotnet sln add --solution-folder`) and all `<ProjectReference>` paths
4. Verify build: `dotnet tool run cloudtek-build --target All --skip RunChecks`
