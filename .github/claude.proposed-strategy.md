# GitHub Actions CI/CD Strategy for Trunk-Based Development

## Executive Summary

This document outlines a comprehensive, reusable CI/CD strategy using GitHub Actions that adheres to the Single Responsibility Principle (SRP) and supports trunk-based development across multiple repositories following a shared SDLC.

## Core Principles

### 1. Single Responsibility Principle for Workflows
Each workflow file should have ONE clear purpose. Complex pipelines are composed of smaller, focused workflows.

### 2. Reusability First
Workflows are designed as building blocks that can be composed differently across repositories while maintaining consistency.

### 3. Trunk-Based Development Requirements
- **Main branch** is always deployable
- **Short-lived feature branches** (`feature/*`, `bug/*`, `hotfix/*`)
- **Release branches** (`release/*`) for release preparation only
- Fast feedback loops (CI runs in <10 minutes)
- Automated quality gates prevent broken code from merging

## Repository Categories

Define repositories based on their artifact output:

1. **Library Repositories** - Produce NuGet packages
2. **Service Repositories** - Produce container images and deploy to environments
3. **Shared Repositories** - Produce both packages and containers (like `hive`)

## Workflow Architecture

### Core Workflow Types (Reusable)

#### 1. **validate.yml** - Pre-merge Validation
**Responsibility:** Ensure code quality before merge to main/release branches

**Triggers:**
- Pull requests to `main` or `release/*`
- PR events: `opened`, `synchronize`, `reopened`

**Jobs:**
- Code compilation
- Unit tests
- Integration tests
- Code coverage checks
- Static analysis
- Security scanning

**Outputs:**
- Test reports
- Coverage reports
- Build artifacts (for validation only, not published)

**Characteristics:**
- Fast execution (< 10 minutes target)
- Fails fast on quality issues
- No publishing or deployment
- No version tagging

---

#### 2. **build.yml** - Artifact Creation
**Responsibility:** Build and publish versioned artifacts

**Triggers:**
- Push to `main` branch (post-merge)
- Push to `release/*` branches
- Manual dispatch

**Jobs:**
- Full compilation
- All tests (unit, integration, module)
- Version calculation (GitVersion or similar)
- Artifact packaging (NuGet, Docker images)
- Artifact publishing to staging feeds

**Outputs:**
- Versioned NuGet packages → staging feed
- Versioned container images → staging registry
- Git tags (semantic version)
- Release notes

**Characteristics:**
- Publishes to **staging/pre-release** channels
- Creates version tags
- Runs comprehensive test suite
- Generates build metadata

---

#### 3. **release.yml** - Production Promotion
**Responsibility:** Promote artifacts from staging to production

**Triggers:**
- Manual approval (workflow_dispatch)
- Or: Merge of `release/*` to `main` with tag
- Or: GitHub Release creation

**Jobs:**
- Artifact validation (smoke tests)
- Promotion to production feeds/registries
- Deployment to production environments (for services)
- Release documentation
- Changelog generation

**Outputs:**
- Packages on public NuGet feed
- Container images in production registry
- GitHub Release with notes
- Deployment confirmations

**Characteristics:**
- Requires manual approval gate
- No rebuilding (promotes existing artifacts)
- Idempotent (can retry safely)
- Audit trail of what was released when

---

#### 4. **deploy.yml** - Environment Deployment
**Responsibility:** Deploy services to specific environments (services only)

**Triggers:**
- Called by `build.yml` (dev/staging environments)
- Called by `release.yml` (production environment)
- Manual dispatch with version parameter

**Jobs:**
- Pre-deployment validation
- Infrastructure provisioning (if needed)
- Service deployment
- Health checks
- Smoke tests

**Outputs:**
- Deployment status
- Environment URL
- Deployed version metadata

**Characteristics:**
- Reusable across environments
- Parameterized (environment, version)
- Rollback capability
- Environment-specific configuration

---

#### 5. **security-scan.yml** - Security Analysis
**Responsibility:** Continuous security scanning

**Triggers:**
- Schedule (daily/weekly)
- Pull requests (for critical changes)
- Manual dispatch

**Jobs:**
- Dependency vulnerability scanning
- SAST (Static Application Security Testing)
- Secret scanning
- License compliance

**Outputs:**
- Security reports
- SARIF uploads to GitHub Security
- Alerts on critical vulnerabilities

---

#### 6. **rollback.yml** - Emergency Rollback
**Responsibility:** Quick rollback to previous version

**Triggers:**
- Manual dispatch with version parameter

**Jobs:**
- Validate rollback target version
- Deploy previous version
- Verify health
- Notify team

---

### Shared/Reusable Workflows

Create a **separate repository** (e.g., `github-workflows-shared`) containing reusable workflow templates:

```
github-workflows-shared/
├── .github/workflows/
│   ├── dotnet-validate.yml          # Reusable validation workflow
│   ├── dotnet-build.yml             # Reusable build workflow
│   ├── dotnet-release.yml           # Reusable release workflow
│   ├── docker-build.yml             # Reusable container build
│   ├── helm-deploy.yml              # Reusable K8s deployment
│   └── security-scan.yml            # Reusable security scanning
├── actions/                          # Custom composite actions
│   ├── setup-dotnet/                # Setup .NET with caching
│   ├── setup-gitversion/            # Setup versioning
│   ├── run-tests/                   # Run tests with reporting
│   └── publish-nuget/               # Publish to NuGet
└── docs/
    ├── INTEGRATION.md               # How to use in your repo
    └── EXAMPLES.md                  # Example implementations
```

Repositories call these workflows:

```yaml
# In consuming repo: .github/workflows/ci.yml
name: ci
on:
  pull_request:
    branches: [main, release/*]

jobs:
  validate:
    uses: cloud-tek/github-workflows-shared/.github/workflows/dotnet-validate.yml@v1
    with:
      dotnet-version: '10.0'
      solution-path: './Hive.sln'
    secrets: inherit
```

---

## Trunk-Based Development Flow

### Feature Development Flow

```
1. Developer creates feature/bug branch from main
   - Current main version: 10.0.0
   ↓
2. Developer commits and pushes changes
   ↓
3. Developer opens PR to main
   - PR Title: "feat: add OpenTelemetry support" (conventional commit format)
   ↓
4. pr-title-check.yml validates PR title format
   - Ensures conventional commit format
   - Blocks merge if invalid
   ↓
5. validate.yml runs automatically
   - Compiles code
   - Runs tests
   - Checks quality gates
   - Validates no manual Version.targets changes
   ↓
6. PR Review + Approval
   ↓
7. Merge to main (squash or merge commit)
   ↓
8. version-bump.yml runs automatically
   - Detects "feat:" in PR title
   - Bumps Version.targets: 10.0.0 → 10.1.0
   - Commits: "chore: bump version to 10.1.0 [skip ci]"
   - Pushes to main
   ↓
9. build.yml runs automatically (triggered by push to main)
   - Reads Version.targets: 10.1.0
   - Calculates version: 10.1.0-alpha.1 (first commit on new version)
   - Builds artifacts
   - Publishes to staging feeds with version 10.1.0-alpha.1
   - Tags commit: 10.1.0-alpha.1
   ↓
10. Artifacts available for testing in dev/staging environments
    - Developers can test 10.1.0-alpha.1 package
```

**Version Progression Example:**
```
Main branch state:
- Before merge: Version.targets = 10.0.0
- After "feat:" PR merge: Version.targets = 10.1.0
- Published: 10.1.0-alpha.1, 10.1.0-alpha.2, ... (with each subsequent merge)
- After "fix:" PR merge: Version.targets = 10.1.1
- Published: 10.1.1-alpha.1, 10.1.1-alpha.2, ...
```

---

### Release Flow

```
1. Determine release version based on main
   - Main version: 10.1.5 (from Version.targets)
   - Release version: 10.2.0 (next minor, or 11.0.0 for major)
   ↓
2. Create release branch: release/10.2.0 from main
   - Option A: Use helper script
     ./scripts/create-release-branch.sh 10.2.0
   - Option B: Manual
     git checkout -b release/10.2.0 main
     sed -i 's/<VersionPrefix>10.1.5/10.2.0/' Version.targets
     git commit -am "chore: bump version to 10.2.0"
     git push -u origin release/10.2.0
   ↓
3. release-branch-check.yml validates
   - Branch name matches pattern: release/10.2.0
   - Version.targets contains: 10.2.0
   - Version is valid semantic version
   ↓
4. build.yml runs on release branch
   - Reads Version.targets: 10.2.0
   - Calculates version: 10.2.0-rc.1
   - Publishes RC artifacts to staging
   ↓
5. Testing and bug fixes on release branch
   - Bug fix PR: "fix: resolve crash in startup"
   - pr-title-check.yml validates title
   - validate.yml ensures quality
   - Merge triggers build.yml
   - Publishes: 10.2.0-rc.2, 10.2.0-rc.3, ...
   - Version.targets stays 10.2.0 (no version bumping on release branches)
   ↓
6. When ready for production: Create GitHub Release
   - Tag: v10.2.0
   - Target: release/10.2.0 branch
   - Title: "Release 10.2.0"
   - Release notes: Auto-generated from commits
   ↓
7. release.yml triggered by GitHub Release creation
   - Validates tag (v10.2.0) matches Version.targets (10.2.0)
   - Builds final artifacts with clean version: 10.2.0
   - Publishes packages to production NuGet feed
   - Publishes container images to production registry
   - Deploys to production environment (services)
   - Updates GitHub Release with deployment status
   ↓
8. Merge release branch back to main
   - PR: "chore: merge release/10.2.0 back to main"
   - This PR will show Version.targets change (10.1.5 → 10.2.0)
   - Add label: "manual-version-bump" to bypass validation
   - Merge after approval
   ↓
9. Main branch now has Version.targets = 10.2.0
   - Next feature will bump to 10.3.0
   - Next fix will bump to 10.2.1
   ↓
10. Delete release branch (optional, after merge to main)
```

**Version Progression Example:**
```
Release branch lifecycle:
- Branch created: Version.targets = 10.2.0
- First build: 10.2.0-rc.1
- After bug fix: 10.2.0-rc.2
- After bug fix: 10.2.0-rc.3
- GitHub Release: 10.2.0 (clean, no suffix)
- Artifacts: NuGet packages with version 10.2.0
```

---

### Hotfix Flow

```
1. Critical bug discovered in production (v10.2.0)
   ↓
2. Create hotfix branch from main
   - Main version: 10.2.0
   - git checkout -b hotfix/security-patch main
   ↓
3. Implement fix and create PR to main
   - PR Title: "fix: resolve security vulnerability in authentication"
   - pr-title-check.yml validates title
   ↓
4. validate.yml ensures quality
   - Runs security scans
   - Runs all tests
   ↓
5. PR approved and merged to main
   ↓
6. version-bump.yml runs automatically
   - Detects "fix:" in PR title
   - Bumps Version.targets: 10.2.0 → 10.2.1
   - Commits to main
   ↓
7. build.yml runs automatically
   - Calculates version: 10.2.1-alpha.1
   - Publishes to staging
   ↓
8. Create GitHub Release immediately (hotfix urgency)
   - Tag: v10.2.1
   - Target: main (at the hotfix commit)
   - Title: "Hotfix 10.2.1 - Security Patch"
   ↓
9. release.yml triggered
   - Validates tag matches Version.targets (10.2.1)
   - Builds clean 10.2.1 artifacts
   - Publishes to production NuGet
   - Deploys to production immediately
   ↓
10. Cherry-pick to active release branches if needed
    - If release/10.3.0 exists, cherry-pick the fix
```

**Version Progression Example:**
```
Hotfix scenario:
- Production: 10.2.0
- Main before fix: 10.2.0
- PR merged with "fix:" title
- Main after version bump: 10.2.1
- Published to staging: 10.2.1-alpha.1
- GitHub Release created: v10.2.1
- Published to production: 10.2.1
```

---

### Version Bump Behavior Summary

| PR Type | PR Title Example | Before | After | Next Alpha |
|---------|------------------|--------|-------|------------|
| Feature | `feat: add metrics` | 10.0.0 | 10.1.0 | 10.1.0-alpha.1 |
| Bug Fix | `fix: memory leak` | 10.1.0 | 10.1.1 | 10.1.1-alpha.1 |
| Breaking | `feat!: redesign API` | 10.1.1 | 11.0.0 | 11.0.0-alpha.1 |
| Chore | `chore: update deps` | 11.0.0 | 11.0.0 | 11.0.0-alpha.N |
| Docs | `docs: update guide` | 11.0.0 | 11.0.0 | 11.0.0-alpha.N |

**Notes:**
- Version bumping only happens on merge to main
- Release branches maintain fixed Version.targets
- RC builds increment suffix only: 10.2.0-rc.1 → 10.2.0-rc.2
- Production releases have clean version: 10.2.0 (no suffix)

---

## Versioning Strategy

### Version Source of Truth: Version.targets

Every repository MUST contain a **Version.targets** file at the repository root:

```xml
<!-- Version.targets -->
<Project>
    <PropertyGroup>
        <VersionPrefix>10.0.0</VersionPrefix>
    </PropertyGroup>
</Project>
```

**Rules:**
- This file contains the semantic version prefix: `Major.Minor.Patch`
- All artifacts in the repository inherit this version
- This is the ONLY source of truth for version numbers
- Projects import this file via `<Import Project="$(MSBuildThisFileDirectory)../../Version.targets" />`

### Automated Version Bumping

Version bumping is automated through two mechanisms:

#### 1. Convention-Based Bumping (PR Title)

Pull request titles determine version increment using conventional commit format:

**Format:** `<type>(<scope>): <description>`

**Type determines increment:**
- `feat:` → **Minor** version bump (10.0.0 → 10.1.0)
- `fix:` → **Patch** version bump (10.0.0 → 10.0.1)
- `BREAKING CHANGE:` or `!` → **Major** version bump (10.0.0 → 11.0.0)
- `chore:`, `docs:`, `style:`, `refactor:`, `test:` → **No bump** (patch bump only in main)

**Examples:**
- `feat: add OpenTelemetry support` → Minor bump
- `fix: resolve memory leak in logging` → Patch bump
- `feat!: redesign IMicroService API` → Major bump
- `chore: update dependencies` → No bump (or patch in main)

#### 2. Manual Bumping (Release Branch)

When creating a release branch, the version is manually set:

```bash
# Create release branch
git checkout -b release/11.0.0 main

# Update Version.targets
# <VersionPrefix>11.0.0</VersionPrefix>

# Commit and push
git commit -am "chore: bump version to 11.0.0"
git push -u origin release/11.0.0
```

### Version Calculation During Build

Build workflows calculate full semantic version:

**Build-time version = VersionPrefix + Suffix + Metadata**

#### Version Suffixes by Branch

- **main branch:** `-alpha.{height}` (e.g., `10.0.0-alpha.23`)
  - Height = number of commits since last version tag or branch creation
- **release/* branch:** `-rc.{height}` (e.g., `10.1.0-rc.5`)
  - Height = number of commits on release branch
- **feature/* branch:** `-feature.{height}+{sha}` (e.g., `10.0.0-feature.7+a1b2c3d`)
- **hotfix/* branch:** `-hotfix.{height}+{sha}` (e.g., `10.0.1-hotfix.2+e4f5g6h`)
- **GitHub Release (tag):** No suffix (e.g., `10.1.0`)

#### Version Calculation Algorithm

```
1. Read VersionPrefix from Version.targets
2. Determine branch type (main, release/*, feature/*, etc.)
3. Calculate height (commit count since reference point)
4. Apply suffix based on branch type
5. Add metadata if needed (+sha for non-main/release)
6. Result: {VersionPrefix}{Suffix}{Metadata}
```

### GitHub Release as Release Trigger

**Creating a GitHub Release automatically triggers production release workflows.**

#### Workflow

```
1. Developer completes release testing on release/X.Y.Z branch
   ↓
2. Developer creates GitHub Release via UI or CLI
   - Tag: vX.Y.Z (e.g., v10.1.0)
   - Target: release/X.Y.Z branch or main
   - Title: "Release X.Y.Z"
   - Release notes: Auto-generated or manual
   ↓
3. GitHub Release creation triggers release.yml workflow
   ↓
4. release.yml workflow:
   - Validates that tag matches Version.targets
   - Builds final artifacts with clean version (X.Y.Z)
   - Publishes packages to production NuGet feed
   - Publishes container images to production registry
   - Deploys services to production environment
   ↓
5. Merge release branch back to main
   ↓
6. Auto-bump Version.targets on main to next version
   - If released 10.1.0, bump main to 10.2.0
```

#### Version Validation

The release workflow validates:
- Git tag matches pattern `v{Major}.{Minor}.{Patch}`
- Tag version matches `Version.targets` VersionPrefix
- No pre-release suffix in tag
- Release artifacts exist and are valid

### Automated Version Bumping Workflow

Create `.github/workflows/version-bump.yml`:

```yaml
name: version-bump

on:
  pull_request:
    types: [closed]
    branches:
      - main

jobs:
  bump-version:
    if: github.event.pull_request.merged == true
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
        with:
          fetch-depth: 0
          token: ${{ secrets.GITHUB_TOKEN }}

      - name: Determine version bump
        id: bump
        run: |
          PR_TITLE="${{ github.event.pull_request.title }}"

          if [[ "$PR_TITLE" =~ ^feat!: ]] || [[ "$PR_TITLE" =~ BREAKING\ CHANGE ]]; then
            echo "type=major" >> $GITHUB_OUTPUT
          elif [[ "$PR_TITLE" =~ ^feat: ]]; then
            echo "type=minor" >> $GITHUB_OUTPUT
          elif [[ "$PR_TITLE" =~ ^fix: ]]; then
            echo "type=patch" >> $GITHUB_OUTPUT
          else
            echo "type=none" >> $GITHUB_OUTPUT
          fi

      - name: Bump version in Version.targets
        if: steps.bump.outputs.type != 'none'
        run: |
          CURRENT=$(grep -oP '<VersionPrefix>\K[^<]+' Version.targets)
          IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT"

          case "${{ steps.bump.outputs.type }}" in
            major)
              MAJOR=$((MAJOR + 1))
              MINOR=0
              PATCH=0
              ;;
            minor)
              MINOR=$((MINOR + 1))
              PATCH=0
              ;;
            patch)
              PATCH=$((PATCH + 1))
              ;;
          esac

          NEW_VERSION="$MAJOR.$MINOR.$PATCH"
          sed -i "s/<VersionPrefix>.*<\/VersionPrefix>/<VersionPrefix>$NEW_VERSION<\/VersionPrefix>/" Version.targets

          echo "Bumped version: $CURRENT → $NEW_VERSION"
          echo "NEW_VERSION=$NEW_VERSION" >> $GITHUB_ENV

      - name: Commit version bump
        if: steps.bump.outputs.type != 'none'
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add Version.targets
          git commit -m "chore: bump version to ${{ env.NEW_VERSION }} [skip ci]"
          git push
```

### Version Examples by Scenario

| Scenario | Branch | Version.targets | Calculated Version | Published Where |
|----------|--------|----------------|-------------------|-----------------|
| Feature PR merged to main | main | 10.0.0 | 10.0.0-alpha.23 | Staging NuGet |
| Release branch created | release/10.1.0 | 10.1.0 | 10.1.0-rc.1 | Staging NuGet |
| Bug fix on release branch | release/10.1.0 | 10.1.0 | 10.1.0-rc.5 | Staging NuGet |
| GitHub Release created | tag: v10.1.0 | 10.1.0 | 10.1.0 | Production NuGet |
| Hotfix merged to main | main | 10.0.1 | 10.0.1-alpha.1 | Staging NuGet |

### Conventional Commit Reference

Full list of commit types:

- `feat:` - New feature (minor bump)
- `fix:` - Bug fix (patch bump)
- `feat!:` or `BREAKING CHANGE:` - Breaking change (major bump)
- `chore:` - Maintenance (no bump)
- `docs:` - Documentation only (no bump)
- `style:` - Code style changes (no bump)
- `refactor:` - Code refactoring (no bump)
- `perf:` - Performance improvement (patch bump)
- `test:` - Adding tests (no bump)
- `build:` - Build system changes (no bump)
- `ci:` - CI configuration changes (no bump)

### Version Bumping Best Practices

1. **PR Title Enforcement:** Use GitHub Action to validate PR titles match conventional commit format
2. **Changelog Generation:** Auto-generate CHANGELOG.md from conventional commits
3. **Version Tag Protection:** Protect version tags from deletion
4. **Release Branch Naming:** Always use `release/{version}` format
5. **Hotfix Strategy:** Create from production tag, bump patch in Version.targets
6. **Version Conflicts:** CI fails if Version.targets was modified in PR to main (except by automation)

---

## Enforcement Mechanisms

To ensure the versioning strategy works correctly, several enforcement mechanisms must be in place.

### 1. PR Title Validation

**Problem:** Automated version bumping depends on conventional commit format in PR titles.

**Solution:** Create `.github/workflows/pr-title-check.yml`:

```yaml
name: pr-title-check

on:
  pull_request:
    types: [opened, edited, synchronize, reopened]
    branches:
      - main
      - release/*

jobs:
  validate-title:
    runs-on: ubuntu-latest
    steps:
      - name: Check PR title format
        uses: amannn/action-semantic-pull-request@v5
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          types: |
            feat
            fix
            docs
            style
            refactor
            perf
            test
            build
            ci
            chore
            revert
          requireScope: false
          subjectPattern: ^(?![A-Z]).+$
          subjectPatternError: |
            Subject must not start with uppercase character.
          validateSingleCommit: false
          ignoreLabels: |
            ignore-semantic-check
```

**Configuration:**
- **Blocks PR merge** if title doesn't match conventional commit format
- **Allowed types:** feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert
- **Breaking changes:** Detected via `!` suffix (e.g., `feat!:`) or `BREAKING CHANGE` in description
- **Bypass option:** Add `ignore-semantic-check` label for special cases (rare)

**GitHub Branch Protection:**
Enable in repository settings → Branches → Branch protection rule for `main`:
- ✅ Require status checks to pass before merging
- ✅ Required checks: `pr-title-check / validate-title`

**User Experience:**
```
❌ Invalid PR titles:
- "Add OpenTelemetry support"
- "Fix: memory leak" (uppercase after colon)
- "new feature for logging"

✅ Valid PR titles:
- "feat: add OpenTelemetry support"
- "fix: resolve memory leak in logging"
- "feat!: redesign IMicroService API"
- "chore: update dependencies"
- "docs: update installation guide"
```

---

### 2. Release Branch Version Validation

**Problem:** Release branches must have Version.targets matching the branch name.

**Solution:** Create `.github/workflows/release-branch-check.yml`:

```yaml
name: release-branch-check

on:
  pull_request:
    types: [opened, synchronize, reopened]
    branches:
      - 'release/**'
  push:
    branches:
      - 'release/**'

jobs:
  validate-version:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5

      - name: Extract version from branch name
        id: branch-version
        run: |
          BRANCH_NAME="${{ github.ref_name }}"

          # Extract version from release/X.Y.Z
          if [[ "$BRANCH_NAME" =~ ^release/([0-9]+\.[0-9]+\.[0-9]+)$ ]]; then
            EXPECTED_VERSION="${BASH_REMATCH[1]}"
            echo "version=$EXPECTED_VERSION" >> $GITHUB_OUTPUT
          else
            echo "ERROR: Branch name must match pattern 'release/X.Y.Z'"
            exit 1
          fi

      - name: Validate Version.targets matches branch
        run: |
          EXPECTED="${{ steps.branch-version.outputs.version }}"
          ACTUAL=$(grep -oP '<VersionPrefix>\K[^<]+' Version.targets)

          if [ "$ACTUAL" != "$EXPECTED" ]; then
            echo "❌ ERROR: Version mismatch!"
            echo "   Branch:          release/$EXPECTED"
            echo "   Version.targets: $ACTUAL"
            echo ""
            echo "Please update Version.targets to match the branch name:"
            echo "   <VersionPrefix>$EXPECTED</VersionPrefix>"
            exit 1
          fi

          echo "✅ Version.targets ($ACTUAL) matches branch (release/$EXPECTED)"

      - name: Validate version is greater than main
        run: |
          git fetch origin main
          MAIN_VERSION=$(git show origin/main:Version.targets | grep -oP '<VersionPrefix>\K[^<]+')
          RELEASE_VERSION="${{ steps.branch-version.outputs.version }}"

          # Simple version comparison (assumes semantic versioning)
          if [ "$RELEASE_VERSION" == "$MAIN_VERSION" ]; then
            echo "⚠️  WARNING: Release version ($RELEASE_VERSION) equals main version ($MAIN_VERSION)"
            echo "   This may be acceptable for patch releases."
          fi

          echo "Main version:    $MAIN_VERSION"
          echo "Release version: $RELEASE_VERSION"
```

**GitHub Branch Protection:**
Enable for pattern `release/**`:
- ✅ Require status checks: `release-branch-check / validate-version`
- ✅ Require pull request before merging
- ✅ Require approvals: 2
- ✅ Restrict who can push: Release managers only

**Workflow:**
```bash
# Developer creates release branch
git checkout -b release/10.1.0 main

# Update Version.targets
sed -i 's/<VersionPrefix>.*<\/VersionPrefix>/<VersionPrefix>10.1.0<\/VersionPrefix>/' Version.targets

# Commit and push
git add Version.targets
git commit -m "chore: bump version to 10.1.0"
git push -u origin release/10.1.0

# Workflow automatically validates:
# ✅ Branch name format: release/10.1.0
# ✅ Version.targets contains: 10.1.0
# ✅ Version is valid semantic version
```

---

### 3. Version.targets Protection on Main

**Problem:** Prevent manual version changes to Version.targets on main (only automation should modify).

**Solution:** Add validation to `validate.yml`:

```yaml
# Add to .github/workflows/validate.yml

jobs:
  validate:
    # ... existing validation job

  check-version-modification:
    runs-on: ubuntu-latest
    if: github.event_name == 'pull_request'
    steps:
      - uses: actions/checkout@v5
        with:
          fetch-depth: 0

      - name: Check if Version.targets was modified
        id: check-version
        run: |
          # Check if Version.targets was changed in this PR
          git fetch origin ${{ github.base_ref }}

          if git diff origin/${{ github.base_ref }}...HEAD --name-only | grep -q "^Version.targets$"; then
            echo "modified=true" >> $GITHUB_OUTPUT
          else
            echo "modified=false" >> $GITHUB_OUTPUT
          fi

      - name: Validate Version.targets modification
        if: steps.check-version.outputs.modified == 'true'
        run: |
          PR_TITLE="${{ github.event.pull_request.title }}"
          PR_AUTHOR="${{ github.event.pull_request.user.login }}"

          # Allow if PR is from version-bump workflow (automated)
          if [[ "$PR_TITLE" =~ ^chore:\ bump\ version\ to\ [0-9]+\.[0-9]+\.[0-9]+\ \[skip\ ci\]$ ]]; then
            echo "✅ Automated version bump detected - allowing"
            exit 0
          fi

          # Allow if PR author is github-actions bot
          if [ "$PR_AUTHOR" == "github-actions[bot]" ]; then
            echo "✅ Version bump from automation - allowing"
            exit 0
          fi

          # Allow if PR has special label (requires admin approval)
          if [[ "${{ contains(github.event.pull_request.labels.*.name, 'manual-version-bump') }}" == "true" ]]; then
            echo "⚠️  Manual version bump with approval - allowing"
            exit 0
          fi

          echo "❌ ERROR: Manual modification of Version.targets detected!"
          echo ""
          echo "Version.targets should only be modified by:"
          echo "  1. Automated version-bump workflow (after PR merge)"
          echo "  2. Release branch creation (release/* branches only)"
          echo ""
          echo "If you need to manually bump the version, add the 'manual-version-bump' label"
          echo "and get approval from a repository administrator."
          exit 1
```

**Branch Protection:**
- ✅ Require `validate / check-version-modification` to pass

---

### 4. Release Tag Validation

**Problem:** Ensure GitHub Releases have tags that match Version.targets.

**Solution:** Add validation to `release.yml` (already included in template above):

```yaml
# In release.yml
jobs:
  validate-version:
    # Validates:
    # 1. Tag format: v{Major}.{Minor}.{Patch}
    # 2. Tag version matches Version.targets
    # 3. No pre-release suffix
```

**Additional Protection:** Configure tag protection rules in GitHub:
- Repository Settings → Tags → Protected tags
- Pattern: `v*.*.*`
- ✅ Restrict tag creation to: Release managers
- ✅ Require tag signing (optional but recommended)

---

### 5. Automated Helper: Release Branch Creation Script

**Problem:** Manual process error-prone (branch naming, version updates).

**Solution:** Provide a script `scripts/create-release-branch.sh`:

```bash
#!/bin/bash
set -e

# Usage: ./scripts/create-release-branch.sh 10.1.0

VERSION=$1

if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version>"
  echo "Example: $0 10.1.0"
  exit 1
fi

# Validate version format
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "ERROR: Version must be in format X.Y.Z"
  exit 1
fi

# Ensure on main and up to date
git checkout main
git pull origin main

# Read current version
CURRENT_VERSION=$(grep -oP '<VersionPrefix>\K[^<]+' Version.targets)
echo "Current version: $CURRENT_VERSION"
echo "New version:     $VERSION"

# Create release branch
BRANCH_NAME="release/$VERSION"
echo "Creating branch: $BRANCH_NAME"
git checkout -b "$BRANCH_NAME"

# Update Version.targets
sed -i "s/<VersionPrefix>.*<\/VersionPrefix>/<VersionPrefix>$VERSION<\/VersionPrefix>/" Version.targets

# Commit and push
git add Version.targets
git commit -m "chore: bump version to $VERSION"
git push -u origin "$BRANCH_NAME"

echo ""
echo "✅ Release branch created successfully!"
echo "   Branch: $BRANCH_NAME"
echo "   Version: $VERSION"
echo ""
echo "Next steps:"
echo "  1. CI will validate the version"
echo "  2. Make any final changes needed for the release"
echo "  3. When ready, create a GitHub Release with tag v$VERSION"
```

Make it executable:
```bash
chmod +x scripts/create-release-branch.sh
```

Usage:
```bash
./scripts/create-release-branch.sh 10.1.0
```

---

### 6. Summary of Enforcement Points

| Enforcement | Mechanism | Blocks | When |
|------------|-----------|--------|------|
| PR Title Format | `pr-title-check.yml` | ✅ Yes | PR opened/edited to main/release/* |
| Release Branch Version | `release-branch-check.yml` | ✅ Yes | Push/PR to release/* branches |
| Version.targets Protection | `validate.yml` check | ✅ Yes | PR to main with Version.targets change |
| Release Tag Format | `release.yml` validation | ✅ Yes | GitHub Release published |
| Tag Protection | GitHub settings | ✅ Yes | Tag creation (v*) |
| Branch Protection | GitHub settings | ✅ Yes | Push to main/release/* |

---

### 7. GitHub Repository Configuration Checklist

To fully implement enforcement:

**Branch Protection Rules:**

For `main` branch:
```
✅ Require pull request before merging
✅ Require approvals: 1
✅ Dismiss stale approvals when new commits are pushed
✅ Require review from Code Owners (optional)
✅ Require status checks to pass:
   - pr-title-check / validate-title
   - validate / validate
   - validate / check-version-modification
✅ Require conversation resolution before merging
✅ Require linear history (optional - prevents merge commits)
✅ Do not allow bypassing the above settings
```

For `release/**` pattern:
```
✅ Require pull request before merging
✅ Require approvals: 2
✅ Require status checks to pass:
   - release-branch-check / validate-version
   - validate / validate
✅ Restrict who can push to matching branches:
   - Add: Release managers team/users
✅ Do not allow bypassing the above settings
```

**Tag Protection Rules:**
```
Tag pattern: v*.*.*
✅ Restrict tag creation to: Release managers
✅ Require tag signature (if using signed commits)
```

**Rulesets (Alternative to Branch Protection):**

GitHub now supports Rulesets as a more flexible alternative:
```
Repository Settings → Rules → Rulesets → New ruleset

Target: Default branch (main)
Rules:
✅ Require pull request before merging
✅ Require status checks
✅ Block force pushes
✅ Require linear history
```

---

### 8. Team Guidelines

Document these practices in `CONTRIBUTING.md`:

```markdown
## Versioning and Release Process

### Creating a Pull Request

1. **PR Title Format (REQUIRED):**
   - Use conventional commit format: `<type>: <description>`
   - Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`
   - Breaking changes: Add `!` after type (e.g., `feat!: redesign API`)
   - Examples:
     - ✅ `feat: add OpenTelemetry support`
     - ✅ `fix: resolve memory leak`
     - ✅ `feat!: redesign IMicroService API`
     - ❌ `Add OpenTelemetry support`

2. **Version Bumping:**
   - Versions are bumped automatically based on PR title when merged to main
   - `feat:` → minor version bump
   - `fix:` → patch version bump
   - `feat!:` or `BREAKING CHANGE` → major version bump

### Creating a Release

1. **Use the helper script:**
   ```bash
   ./scripts/create-release-branch.sh 10.1.0
   ```

2. **Or manually:**
   ```bash
   git checkout -b release/10.1.0 main
   # Edit Version.targets to set <VersionPrefix>10.1.0</VersionPrefix>
   git commit -am "chore: bump version to 10.1.0"
   git push -u origin release/10.1.0
   ```

3. **Test the release candidate:**
   - CI will build and publish `10.1.0-rc.X` to staging
   - Test thoroughly in staging environment

4. **Publish the release:**
   - Create GitHub Release with tag `v10.1.0`
   - CI will automatically publish to production
```

---

## Quality Gates

### Pre-Merge (validate.yml)
- **Compilation:** Must succeed
- **Unit Tests:** Must pass with >80% coverage
- **Integration Tests:** Must pass
- **Security Scan:** No critical vulnerabilities
- **Code Style:** Linting passes
- **PR Approval:** Required from 1+ reviewers
- **Code Coverage:** See Code Coverage Strategy below

### Post-Merge (build.yml)
- All validation checks
- **Module Tests:** Must pass
- **Artifact Creation:** Must succeed
- **Package Validation:** Metadata correct
- **Code Coverage:** See Code Coverage Strategy below

### Pre-Release (release.yml)
- **Smoke Tests:** Basic functionality works
- **Manual Approval:** Required for production
- **Artifact Integrity:** Checksums verified

---

## Code Coverage Strategy

### Overview

Code coverage is a critical quality metric that ensures our codebase maintains adequate test coverage. This section defines our coverage thresholds, enforcement mechanisms, and the GitHub Actions implementation.

### Coverage Tool: Coverlet

We use **Coverlet** for .NET code coverage collection, which integrates seamlessly with `dotnet test` and supports multiple output formats:

- **Cobertura** - XML format for reporting and analysis (recommended)
- **OpenCover** - XML format compatible with many tools
- **lcov** - Text format for visualization tools
- **JSON** - Structured data for custom processing

### Coverage Thresholds

#### Absolute Coverage Thresholds (Non-PR Builds)

Applied to builds on `main` and `release/*` branches:

| Metric | Minimum Threshold | Target |
|--------|------------------|--------|
| **Line Coverage** | 75% | 80%+ |
| **Branch Coverage** | 70% | 75%+ |
| **Method Coverage** | 75% | 80%+ |

**Enforcement:**
- ⚠️ **Warning** if coverage falls below target
- ❌ **Failure** if coverage falls below minimum threshold

#### Delta Coverage Thresholds (PR Builds)

Applied to pull requests to prevent coverage regression:

| Metric | Policy | Enforcement |
|--------|--------|-------------|
| **Coverage Rate Reduction** | No decrease in overall coverage % | ❌ Fail on any % decrease |
| **New Uncovered Statements** | No new untested code if coverage improves | ⚠️ Warn if new untested code added |
| **Uncovered Lines Increase** | No files should have more uncovered lines | ❌ Fail (strict mode) |

**Recommended Policy:** Use **Coverage Rate Reduction Failure** mode to prevent coverage percentage from decreasing while allowing architectural refactoring.

**Enforcement Modes (configurable):**
1. **Strict Mode:** Fail if any file has increased uncovered lines
2. **Moderate Mode:** Fail if new untested code appears despite overall improvement
3. **Forgiving Mode:** Fail only if overall coverage percentage decreases

### Rationale for Thresholds

**Why 75-80% for absolute coverage?**
- Balances thorough testing with practical development velocity
- Aligns with industry standards for well-tested codebases
- Allows flexibility for utility code and generated code

**Why fail on coverage rate reduction?**
- Prevents gradual erosion of code quality
- Maintains momentum toward higher coverage
- Allows refactoring as long as coverage doesn't decrease

**Why measure delta coverage?**
- Encourages test-first development practices
- Catches regressions before they reach main branch
- Provides immediate feedback to developers

### Coverage Exclusions

Exclude from coverage analysis:

```xml
<!-- In .runsettings or via Coverlet parameters -->
<Exclude>
  [*.Tests]*                    <!-- Test projects -->
  [*]*.Designer.cs              <!-- Designer-generated files -->
  [*]*.g.cs                     <!-- Generated files -->
  [*]*.g.i.cs                   <!-- Generated files -->
  [*]*AssemblyInfo.cs           <!-- Assembly info -->
  [*]Program.cs                 <!-- Simple entry points (if minimal) -->
</Exclude>
```

### Recommended GitHub Actions

Based on evaluation of available actions in the GitHub Marketplace, we recommend a combination of two actions:

#### Primary: irongut/CodeCoverageSummary

**Repository:** [Code Coverage Summary](https://github.com/marketplace/actions/code-coverage-summary)

**Key Features:**
- ✅ Supports Cobertura format (Coverlet, gcovr)
- ✅ Absolute threshold enforcement
- ✅ Customizable pass/fail thresholds
- ✅ Markdown and text output formats
- ✅ GitHub Actions console integration
- ✅ Works with PR comment actions

**Limitations:**
- ❌ No built-in delta coverage support
- ❌ No PR comment functionality (requires separate action)

**Use Case:** Absolute coverage enforcement on all builds

#### Secondary: insightsengineering/coverage-action

**Repository:** [Coverage Action](https://github.com/insightsengineering/coverage-action)

**Key Features:**
- ✅ Supports Cobertura format
- ✅ Delta coverage (branch-to-branch comparison)
- ✅ Three failure modes (strict/moderate/forgiving)
- ✅ Built-in PR comment support
- ✅ Historical coverage storage
- ✅ Per-file coverage details
- ✅ Tracks uncovered line increases

**Capabilities:**
1. **Coverage Rate Reduction:** Fails if overall % decreases
2. **New Uncovered Statements:** Fails if new untested code added during overall improvement
3. **Uncovered Lines Increase:** Fails if any file has more uncovered lines

**Use Case:** Delta coverage enforcement on PRs

### Implementation: Coverage Workflow

#### Step 1: Generate Coverage During Tests

Update test execution to collect coverage:

```yaml
# Common steps for both validate.yml and build.yml
- name: Run tests with coverage
  run: |
    dotnet test Hive.sln \
      --configuration Release \
      --no-build \
      --logger "trx;LogFileName=test-results.trx" \
      --collect:"XPlat Code Coverage" \
      -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

- name: Install ReportGenerator
  run: dotnet tool install --global dotnet-reportgenerator-globaltool

- name: Merge coverage reports
  run: |
    reportgenerator \
      -reports:"**/TestResults/**/coverage.cobertura.xml" \
      -targetdir:"./coverage-report" \
      -reporttypes:"Cobertura;HtmlInline_AzurePipelines;MarkdownSummaryGithub"

- name: Upload coverage report
  uses: actions/upload-artifact@v4
  with:
    name: coverage-report
    path: ./coverage-report/
    retention-days: 30
```

#### Step 2: Enforce Absolute Coverage Thresholds

For all builds (PRs and main/release branches):

```yaml
# In both validate.yml and build.yml
- name: Code Coverage Summary
  uses: irongut/CodeCoverageSummary@v1.3.0
  with:
    filename: coverage-report/Cobertura.xml
    badge: true
    fail_below_min: true
    format: markdown
    hide_branch_rate: false
    hide_complexity: true
    indicators: true
    output: both
    thresholds: '75 80'  # minimum: 75%, target: 80%

- name: Add Coverage PR Comment
  uses: marocchino/sticky-pull-request-comment@v2
  if: github.event_name == 'pull_request'
  with:
    recreate: true
    path: code-coverage-results.md
```

#### Step 3: Enforce Delta Coverage (PR Builds Only)

For pull requests only:

```yaml
# In validate.yml only
- name: Code Coverage Delta Check
  uses: insightsengineering/coverage-action@v2
  with:
    # Path to Cobertura XML report
    path: coverage-report/Cobertura.xml

    # Minimum overall coverage threshold
    threshold: 75

    # Fail build if threshold not met
    fail: true

    # Enable delta coverage comparison
    diff: true
    diff-branch: main
    diff-storage: _xml_coverage_reports

    # Publish detailed report to PR
    publish: true
    coverage-summary-title: "📊 Code Coverage Delta Report"

    # Failure mode: fail if overall coverage % decreases
    coverage-rate-reduction-failure: true

    # Optional: warn if new untested code despite improvement
    new-uncovered-statements-failure: false

    # Optional: strict mode - fail on any file regression
    uncovered-statements-increase-failure: false
```

**Failure Modes Explained:**

| Mode | When It Fails | Recommended For |
|------|--------------|----------------|
| `coverage-rate-reduction-failure` | Overall coverage % decreases | ✅ **Default recommendation** - Balances quality with flexibility |
| `new-uncovered-statements-failure` | New untested code added while coverage improves | Teams transitioning to higher coverage |
| `uncovered-statements-increase-failure` | Any file has more uncovered lines | Very strict teams requiring zero regression |

**Recommended Configuration:**
- **For mature projects:** Enable `coverage-rate-reduction-failure` only
- **For improving coverage:** Enable `coverage-rate-reduction-failure` + `new-uncovered-statements-failure`
- **For strict enforcement:** Enable all three modes

#### Step 4: Store Baseline Coverage for Main

After successful builds on `main`, the `insightsengineering/coverage-action` automatically stores coverage reports in the `diff-storage` branch for future comparisons.

No additional steps needed - this is handled automatically by the action.

### Complete Workflow Examples

#### validate.yml with Coverage Checks (PR Builds)

```yaml
name: validate

on:
  pull_request:
    types: [opened, synchronize, reopened]
    branches:
      - main
      - release/*

jobs:
  test-and-coverage:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
        with:
          fetch-depth: 0  # Required for coverage-action diff

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore Hive.sln

      - name: Build
        run: dotnet build Hive.sln --configuration Release --no-restore

      - name: Run tests with coverage
        run: |
          dotnet test Hive.sln \
            --configuration Release \
            --no-build \
            --logger "trx;LogFileName=test-results.trx" \
            --collect:"XPlat Code Coverage" \
            -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura \
            DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/*.Designer.cs,**/*.g.cs,**/*.g.i.cs"

      - name: Install ReportGenerator
        run: dotnet tool install --global dotnet-reportgenerator-globaltool

      - name: Generate merged coverage report
        run: |
          reportgenerator \
            -reports:"**/TestResults/**/coverage.cobertura.xml" \
            -targetdir:"./coverage-report" \
            -reporttypes:"Cobertura;HtmlInline_AzurePipelines;MarkdownSummaryGithub"

      - name: Code Coverage Summary
        uses: irongut/CodeCoverageSummary@v1.3.0
        with:
          filename: coverage-report/Cobertura.xml
          badge: true
          fail_below_min: true
          format: markdown
          hide_branch_rate: false
          hide_complexity: true
          indicators: true
          output: both
          thresholds: '75 80'

      - name: Code Coverage Delta Check
        uses: insightsengineering/coverage-action@v2
        with:
          path: coverage-report/Cobertura.xml
          threshold: 75
          fail: true
          diff: true
          diff-branch: main
          diff-storage: _xml_coverage_reports
          publish: true
          coverage-summary-title: "📊 Code Coverage Delta Report"
          coverage-rate-reduction-failure: true
          new-uncovered-statements-failure: false
          uncovered-statements-increase-failure: false

      - name: Add Coverage Summary PR Comment
        uses: marocchino/sticky-pull-request-comment@v2
        if: github.event_name == 'pull_request'
        with:
          recreate: true
          path: code-coverage-results.md

      - name: Upload coverage report
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: coverage-report-pr
          path: ./coverage-report/
          retention-days: 30
```

#### build.yml with Coverage Checks (Main/Release Builds)

```yaml
name: build

on:
  push:
    branches:
      - main
      - release/*
  workflow_dispatch:

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore Hive.sln

      - name: Build
        run: dotnet build Hive.sln --configuration Release --no-restore

      - name: Run tests with coverage
        run: |
          dotnet test Hive.sln \
            --configuration Release \
            --no-build \
            --logger "trx;LogFileName=test-results.trx" \
            --collect:"XPlat Code Coverage" \
            -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura \
            DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/*.Designer.cs,**/*.g.cs,**/*.g.i.cs"

      - name: Install ReportGenerator
        run: dotnet tool install --global dotnet-reportgenerator-globaltool

      - name: Generate merged coverage report
        run: |
          reportgenerator \
            -reports:"**/TestResults/**/coverage.cobertura.xml" \
            -targetdir:"./coverage-report" \
            -reporttypes:"Cobertura;HtmlInline_AzurePipelines;MarkdownSummaryGithub;Badges"

      - name: Code Coverage Summary
        uses: irongut/CodeCoverageSummary@v1.3.0
        with:
          filename: coverage-report/Cobertura.xml
          badge: true
          fail_below_min: true
          format: markdown
          hide_branch_rate: false
          hide_complexity: true
          indicators: true
          output: both
          thresholds: '75 80'

      - name: Store coverage for main branch
        if: github.ref == 'refs/heads/main'
        uses: insightsengineering/coverage-action@v2
        with:
          path: coverage-report/Cobertura.xml
          threshold: 75
          fail: true
          diff: false  # No diff on main, just store baseline
          diff-storage: _xml_coverage_reports
          publish: false

      - name: Upload coverage report
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report-main
          path: ./coverage-report/
          retention-days: 90

      # Continue with artifact building and publishing...
```

### Coverage Reporting and Visibility

#### PR Comments

The `insightsengineering/coverage-action` automatically generates PR comments with:
- Overall coverage percentage
- Coverage delta from target branch
- Per-file coverage breakdown
- Detailed uncovered line ranges
- Pass/fail status based on thresholds

Example output:
```markdown
## 📊 Code Coverage Delta Report

**Overall Coverage:** 78.5% (↑ 1.2%)

### Modified Files
| File | Coverage | Change | Uncovered Lines |
|------|----------|--------|-----------------|
| src/Extension.cs | 85.0% | ↑ 5.0% | 42-45, 89 |
| src/Startup.cs | 70.0% | ↓ 3.0% | 12-18, 34-40 |

✅ Coverage check passed
```

#### README Badge

Add a coverage badge to [README.md](README.md) using the badge generated by `CodeCoverageSummary`:

```markdown
![Code Coverage](https://img.shields.io/badge/Code%20Coverage-80%25-success?style=flat)
```

Or use the badge from ReportGenerator:

```markdown
![Line Coverage](./coverage-report/badge_linecoverage.svg)
![Branch Coverage](./coverage-report/badge_branchcoverage.svg)
```

#### GitHub Pages for Coverage Reports

Optionally publish detailed coverage reports to GitHub Pages:

```yaml
- name: Deploy coverage to GitHub Pages
  if: github.ref == 'refs/heads/main'
  uses: peaceiris/actions-gh-pages@v4
  with:
    github_token: ${{ secrets.GITHUB_TOKEN }}
    publish_dir: ./coverage-report
    destination_dir: coverage
    keep_files: false
```

Access reports at: `https://<org>.github.io/<repo>/coverage/`

### Handling Coverage Violations

#### When Coverage Fails in PR

**Developer Actions:**
1. Review the coverage report in PR comments to identify untested code
2. Add missing unit tests for uncovered lines
3. If coverage decrease is justified:
   - Add a comment in the PR explaining why (e.g., removing dead code)
   - Request reviewer approval for coverage exception
4. If coverage drop is temporary (multi-step refactoring):
   - Split work into smaller PRs that maintain coverage
   - Or, temporarily adjust thresholds with team approval

**Reviewer Actions:**
1. Review coverage report in PR comments
2. Verify that coverage decrease is justified or tests are added
3. Ensure critical paths are tested
4. Approve only when coverage meets standards or has valid justification

**Bypassing Coverage Checks (Emergency Only):**
If absolutely necessary, maintainers can:
1. Add a `skip-coverage-check` label to the PR
2. Update workflow to check for this label:
```yaml
- name: Code Coverage Delta Check
  if: ${{ !contains(github.event.pull_request.labels.*.name, 'skip-coverage-check') }}
  uses: insightsengineering/coverage-action@v2
  # ... rest of config
```

#### When Coverage Fails on Main/Release

This should be rare due to PR checks, but if it occurs:

**Immediate Actions:**
1. Investigate why PR checks didn't catch the issue
2. Create hotfix branch to restore coverage
3. Review merge process for gaps

**Prevention:**
- Ensure branch protection requires PR checks to pass
- Verify coverage actions run on all PRs
- Add required status checks to branch protection rules
- Set up alerts for coverage drops on main

### Summary of Coverage Enforcement

| Build Type | Absolute Threshold | Delta Check | Failure Mode | Blocks Merge? |
|------------|-------------------|-------------|--------------|---------------|
| **PR to main** | ≥75% line, ≥70% branch | ✅ Enabled | Coverage rate reduction | ✅ Yes |
| **PR to release/** | ≥75% line, ≥70% branch | ✅ Enabled | Coverage rate reduction | ✅ Yes |
| **Push to main** | ≥75% line, ≥70% branch | ❌ Disabled | N/A | ✅ Yes |
| **Push to release/** | ≥75% line, ≥70% branch | ❌ Disabled | N/A | ✅ Yes |

### GitHub Actions Configuration Summary

**Required Actions:**

1. **irongut/CodeCoverageSummary@v1.3.0**
   - Purpose: Absolute coverage threshold enforcement
   - Where: Both `validate.yml` (PR) and `build.yml` (main/release)
   - Fails build if: Overall coverage < 75%

2. **insightsengineering/coverage-action@v2**
   - Purpose: Delta coverage enforcement and PR reporting
   - Where: `validate.yml` (PR only)
   - Fails build if: Coverage percentage decreases

3. **marocchino/sticky-pull-request-comment@v2**
   - Purpose: Post coverage summary to PR
   - Where: `validate.yml` (PR only)
   - Displays: Summary from `CodeCoverageSummary`

**Optional Actions:**

- **peaceiris/actions-gh-pages@v4** - Publish coverage reports to GitHub Pages
- **actions/upload-artifact@v4** - Store coverage reports as artifacts

### Branch Protection Configuration

To enforce coverage checks, configure branch protection rules:

**For `main` branch:**
```
✅ Require status checks to pass before merging
✅ Required status checks:
   - test-and-coverage / Code Coverage Summary
   - test-and-coverage / Code Coverage Delta Check
✅ Require branches to be up to date before merging
```

**For `release/**` branches:**
```
✅ Require status checks to pass before merging
✅ Required status checks:
   - test-and-coverage / Code Coverage Summary
   - test-and-coverage / Code Coverage Delta Check
✅ Require branches to be up to date before merging
```

### Monitoring and Continuous Improvement

**Track Coverage Trends:**
- Monitor coverage percentage over time on main branch
- Set a team goal to gradually increase to 85%+
- Review coverage reports in sprint retrospectives

**Coverage Debt:**
- Identify modules with <70% coverage
- Create backlog items to improve coverage
- Prioritize testing critical business logic paths

**Team Education:**
- Share coverage reports in team meetings
- Recognize improvements in code reviews
- Provide guidance on writing effective tests

---

## Environment Strategy

### Environment Tiers
1. **Development (dev)** - Auto-deploy from `main`
2. **Staging (staging)** - Auto-deploy from `release/*`
3. **Production (prod)** - Manual deploy via `release.yml`

### Environment Configuration
- Use **GitHub Environments** for each tier
- Apply protection rules:
  - `dev`: No approval required
  - `staging`: Optional approval
  - `prod`: Required approval + deployment branch rules

### Secrets Management
- Secrets stored in GitHub Secrets at Environment level
- Use different credentials per environment
- Never use production credentials in non-production environments

---

## Workflow Composition Examples

### Library Repository (NuGet packages only)

```
.github/workflows/
├── validate.yml          # PR validation
├── build.yml            # Build and publish to staging
└── release.yml          # Promote to production NuGet
```

### Service Repository (Container + K8s deployment)

```
.github/workflows/
├── validate.yml          # PR validation
├── build.yml            # Build image, push to staging
├── deploy.yml           # Reusable deployment workflow
├── release.yml          # Promote to prod, deploy to prod env
├── rollback.yml         # Emergency rollback
└── security-scan.yml    # Scheduled security scanning
```

### Monorepo (Mixed artifacts like Hive)

```
.github/workflows/
├── validate.yml          # PR validation (all projects)
├── build.yml            # Build packages + images
├── deploy.yml           # Deploy demo services
├── release.yml          # Promote packages + images
└── security-scan.yml    # Security scanning
```

---

## Dagger Integration Pattern

Your current setup uses Dagger for build orchestration. This fits well with the strategy:

### Reusable Dagger Workflow
Create `.github/workflows/dagger-base.yml` as a reusable workflow:

```yaml
name: dagger-base
on:
  workflow_call:
    inputs:
      target:
        description: "Build target"
        type: string
        required: true
      skip:
        description: "Targets to skip"
        type: string
        default: ""
      publish:
        description: "Publish artifacts"
        type: boolean
        default: false
    secrets:
      NUGET_API_KEY:
        required: false
      DAGGER_CLOUD_TOKEN:
        required: false
      # ... other secrets

jobs:
  dagger:
    runs-on: ubuntu-latest
    steps:
      # ... setup steps
      - name: dagger call
        uses: dagger/dagger-for-github@v8.2.0
        with:
          module: ${{ inputs.DaggerModule }}
          args: build --target ${{ inputs.target }} --skip "${{ inputs.skip }}"
          # ... other args
```

### Compose Workflows Using Dagger

**validate.yml:**
```yaml
jobs:
  validate:
    uses: ./.github/workflows/dagger-base.yml
    with:
      target: 'All'
      skip: 'Push'  # Never publish in validation
      publish: false
```

**build.yml:**
```yaml
jobs:
  build:
    uses: ./.github/workflows/dagger-base.yml
    with:
      target: 'All'
      skip: ''
      publish: true
    secrets: inherit
```

---

## Migration Path

### Phase 1: Refactor Current Workflows
1. Split `ci.yml` into `validate.yml` and `build.yml`
2. Extract Dagger execution to `dagger-base.yml` reusable workflow
3. Create `release.yml` for production promotion
4. Update branch protection rules

### Phase 2: Create Shared Workflow Repository
1. Create `github-workflows-shared` repository
2. Move reusable workflows to shared repo
3. Version shared workflows (use tags like `v1`, `v1.1`)
4. Update consuming repos to reference shared workflows

### Phase 3: Standardize Across Repositories
1. Apply workflow pattern to all repositories
2. Standardize environment names
3. Implement consistent versioning strategy
4. Document repository-specific customizations

### Phase 4: Enhance Observability
1. Add workflow monitoring/alerting
2. Implement deployment tracking
3. Create CI/CD dashboard
4. Collect metrics (build times, success rates, etc.)

---

## Repository Structure Requirements

For this strategy to work effectively across repositories:

### Required Files
```
.github/
├── workflows/
│   ├── validate.yml         # PR validation
│   ├── build.yml           # Artifact building
│   └── release.yml         # Production release
├── environments/           # Environment-specific configs
│   ├── dev.env
│   ├── staging.env
│   └── prod.env
└── WORKFLOWS.md           # Repo-specific workflow docs

.gitversion.yml             # Or version config
Version.targets             # Version source of truth
```

### Branch Protection Rules
Configure in GitHub repo settings:

**main branch:**
- Require PR before merging
- Require status checks (validate workflow)
- Require approvals (1+)
- Require conversation resolution
- Require linear history (optional)

**release/* branches:**
- Require PR before merging
- Require status checks
- Require approvals (2+)
- Restrict who can push (release managers only)

---

## Success Metrics

Track these metrics to measure CI/CD effectiveness:

1. **Lead Time:** Time from commit to production
2. **Deployment Frequency:** How often code reaches production
3. **Mean Time to Recovery (MTTR):** How quickly rollbacks happen
4. **Change Failure Rate:** Percentage of deployments causing failures
5. **Build Success Rate:** Percentage of builds that pass
6. **PR Cycle Time:** Time from PR creation to merge
7. **Test Coverage:** Code coverage percentage

Target benchmarks:
- Lead Time: < 1 day for libraries, < 4 hours for critical fixes
- Deployment Frequency: Multiple times per day (main branch)
- MTTR: < 1 hour
- Change Failure Rate: < 15%

---

## Security Considerations

### Secrets Management
- Use GitHub Secrets, never hardcode
- Rotate secrets regularly
- Use environment-level secrets for isolation
- Consider external secret managers (Azure KeyVault, AWS Secrets Manager)

### Access Control
- Limit who can approve production deployments
- Use environment protection rules
- Enable SAML SSO if available
- Audit workflow runs regularly

### Artifact Security
- Sign NuGet packages
- Sign container images
- Scan dependencies for vulnerabilities
- Maintain SBOM (Software Bill of Materials)

---

## Tooling Recommendations

### Required
- **GitHub Actions** - CI/CD orchestration
- **Dagger** - Build orchestration (current choice)
- **GitVersion** - Semantic versioning
- **xUnit** - Testing framework

### Recommended
- **Dependabot** - Dependency updates
- **CodeQL** - Security scanning
- **SonarCloud** - Code quality
- **Snyk** - Vulnerability scanning
- **Docker Scout** - Container security
- **Artifact Registry** - Private NuGet feed (Azure Artifacts, GitHub Packages, or ProGet)

### Optional Enhancements
- **Terraform/Pulumi** - Infrastructure as Code
- **ArgoCD** - GitOps deployments
- **Grafana** - CI/CD metrics visualization
- **PagerDuty** - Deployment alerting

---

## Example: Hive Repository Implementation

### Current State Analysis
- Uses Dagger for build orchestration ✓
- Has reusable `dagger.yml` workflow ✓
- Single `ci.yml` handles multiple concerns ✗
- Missing release workflow ✗
- No environment-based deployments ✗

### Proposed Structure
```
.github/workflows/
├── validate.yml           # NEW: PR validation only
├── build.yml             # NEW: Post-merge builds
├── release.yml           # NEW: Production releases
├── dagger-base.yml       # REFACTOR: Reusable Dagger workflow
└── security-scan.yml     # NEW: Scheduled security scans
```

### Implementation Steps for Hive

1. **Create `validate.yml`**
   - Trigger on PRs to `main` and `release/*`
   - Call `dagger-base.yml` with target `All`, skip `Push`
   - No artifact publishing

2. **Create `build.yml`**
   - Trigger on push to `main` and `release/*`
   - Call `dagger-base.yml` with target `All`, enable publishing
   - Publish to staging NuGet feed
   - Tag commits with version

3. **Create `release.yml`**
   - Manual trigger only (workflow_dispatch)
   - Input: version to release
   - Promote packages from staging to production NuGet
   - Create GitHub Release
   - Deploy demo services to production

4. **Extract `dagger-base.yml`**
   - Make current `dagger.yml` more parameterized
   - Add `publish` boolean parameter
   - Conditional publishing based on parameter

5. **Update branch protection**
   - Require `validate.yml` to pass for PRs
   - Require 1+ approvals for main
   - Require 2+ approvals for release branches

---

## Appendix: Workflow Template Examples

### A.1 validate.yml Template

```yaml
name: validate

on:
  pull_request:
    types: [opened, synchronize, reopened]
    branches:
      - main
      - release/*

jobs:
  validate:
    uses: ./.github/workflows/dagger-base.yml
    with:
      target: 'All'
      skip: 'Push'
      publish: false
    secrets: inherit
```

### A.2 build.yml Template

```yaml
name: build

on:
  push:
    branches:
      - main
      - release/*
  workflow_dispatch:

jobs:
  version:
    runs-on: ubuntu-latest
    outputs:
      semver: ${{ steps.gitversion.outputs.semVer }}
    steps:
      - uses: actions/checkout@v5
        with:
          fetch-depth: 0
      - uses: gittools/actions/gitversion/setup@v0.12
        with:
          versionSpec: '5.x'
      - id: gitversion
        uses: gittools/actions/gitversion/execute@v0.12

  build:
    needs: version
    uses: ./.github/workflows/dagger-base.yml
    with:
      target: 'All'
      skip: ''
      publish: true
      version: ${{ needs.version.outputs.semver }}
    secrets: inherit

  tag:
    needs: [version, build]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - name: Create tag
        run: |
          git tag ${{ needs.version.outputs.semver }}
          git push origin ${{ needs.version.outputs.semver }}
```

### A.3 release.yml Template

```yaml
name: release

on:
  release:
    types: [published]
  workflow_dispatch:
    inputs:
      version:
        description: 'Version to release (e.g., 10.1.0)'
        required: true
        type: string

jobs:
  validate-version:
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.extract.outputs.version }}
    steps:
      - uses: actions/checkout@v5
        with:
          ref: ${{ github.event.release.tag_name || format('v{0}', inputs.version) }}

      - name: Extract version
        id: extract
        run: |
          if [ "${{ github.event_name }}" == "release" ]; then
            TAG="${{ github.event.release.tag_name }}"
            VERSION="${TAG#v}"  # Remove 'v' prefix
          else
            VERSION="${{ inputs.version }}"
          fi
          echo "version=$VERSION" >> $GITHUB_OUTPUT

      - name: Validate version matches Version.targets
        run: |
          VERSION_PREFIX=$(grep -oP '<VersionPrefix>\K[^<]+' Version.targets)
          if [ "$VERSION_PREFIX" != "${{ steps.extract.outputs.version }}" ]; then
            echo "ERROR: Tag version (${{ steps.extract.outputs.version }}) does not match Version.targets ($VERSION_PREFIX)"
            exit 1
          fi

      - name: Validate version format
        run: |
          if ! [[ "${{ steps.extract.outputs.version }}" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
            echo "ERROR: Version must be in format X.Y.Z"
            exit 1
          fi

  build-release-artifacts:
    needs: validate-version
    uses: ./.github/workflows/dagger-base.yml
    with:
      target: 'All'
      skip: ''
      publish: false
      version: ${{ needs.validate-version.outputs.version }}
    secrets: inherit

  publish-production:
    needs: [validate-version, build-release-artifacts]
    runs-on: ubuntu-latest
    environment: production
    steps:
      - name: Download artifacts
        uses: actions/download-artifact@v4
        with:
          name: packages
          path: ./artifacts

      - name: Publish to production NuGet
        run: |
          dotnet nuget push ./artifacts/*.nupkg \
            --source https://api.nuget.org/v3/index.json \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --skip-duplicate

      - name: Publish container images to production
        run: |
          # Tag and push container images to production registry
          # Example for ACR/ECR/Docker Hub
          docker tag myimage:${{ needs.validate-version.outputs.version }} \
            production.azurecr.io/myimage:${{ needs.validate-version.outputs.version }}
          docker push production.azurecr.io/myimage:${{ needs.validate-version.outputs.version }}

  update-release-notes:
    needs: [validate-version, publish-production]
    runs-on: ubuntu-latest
    if: github.event_name == 'release'
    steps:
      - uses: actions/checkout@v5
      - name: Generate changelog
        run: |
          # Generate changelog from commits since last release
          # Append to release notes

      - name: Update GitHub Release
        uses: actions/github-script@v7
        with:
          script: |
            await github.rest.repos.updateRelease({
              owner: context.repo.owner,
              repo: context.repo.repo,
              release_id: ${{ github.event.release.id }},
              body: `${{ github.event.release.body }}\n\n## Artifacts Published\n- NuGet packages: ✅\n- Container images: ✅\n- Production deployment: ✅`
            });
```

---

## Centralized Git Hooks Strategy

Managing Git hooks across hundreds of repositories requires a scalable, centralized approach. This section outlines strategies to maximize reuse and minimize duplication.

### Problem Statement

**Challenges with Traditional Git Hooks:**
1. **Duplication** - Every repository has its own copy of hook scripts
2. **Maintenance Overhead** - Updates require changes to hundreds of repositories
3. **Inconsistency** - Repositories may have different hook versions
4. **Onboarding Friction** - New repositories need manual hook setup
5. **No Central Control** - No way to enforce organization-wide standards

**Goal:** Centralize hook logic in one location, share across all repositories with minimal per-repo configuration.

---

### Solution Architecture

#### Option 1: Centralized Hook Repository (Recommended)

Create a dedicated repository that all other repositories reference for shared hook logic.

**Repository Structure:**
```
github.com/cloud-tek/git-hooks (or cloud-tek/standards)
├── hooks/
│   ├── pre-commit.d/
│   │   ├── 01-conventional-commit-msg.sh
│   │   ├── 02-prevent-secrets.sh
│   │   ├── 03-dotnet-format.sh
│   │   └── 04-version-targets-check.sh
│   ├── commit-msg.d/
│   │   └── 01-conventional-format.sh
│   ├── pre-push.d/
│   │   └── 01-prevent-force-push-main.sh
│   └── shared/
│       ├── utils.sh                  # Shared utilities
│       └── config.sh                 # Shared configuration
├── install.sh                         # Hook installation script
├── update.sh                          # Hook update script
├── README.md                          # Documentation
└── VERSION                            # Hook version tracking
```

**Per-Repository Setup:**

Each repository contains minimal configuration:

```
your-repo/
├── .git-hooks/
│   ├── config.json                    # Repo-specific hook config
│   └── .version                       # Installed hooks version
└── .githooks-repo                     # Points to centralized repo
```

---

### Implementation: Centralized Hook System

#### Step 1: Create Centralized Hook Repository

**Repository:** `cloud-tek/git-hooks` or `cloud-tek/standards`

**File:** `hooks/shared/utils.sh`
```bash
#!/bin/bash
# Shared utilities for all hooks

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running in CI
is_ci() {
    [ -n "$CI" ] || [ -n "$GITHUB_ACTIONS" ] || [ -n "$GITLAB_CI" ]
}

# Get repository root
get_repo_root() {
    git rev-parse --show-toplevel
}

# Check if file exists in staging
is_staged() {
    local file=$1
    git diff --cached --name-only | grep -q "^${file}$"
}

# Load repo-specific configuration
load_config() {
    local repo_root=$(get_repo_root)
    local config_file="${repo_root}/.git-hooks/config.json"

    if [ -f "$config_file" ]; then
        # Export config as environment variables
        # For simplicity, you could use jq to parse JSON
        export GITHOOKS_CONFIG_LOADED=true
    fi
}
```

**File:** `hooks/pre-commit.d/04-version-targets-check.sh`
```bash
#!/bin/bash
# Check if Version.targets was modified manually (not by automation)

set -e

# Source shared utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "${SCRIPT_DIR}/shared/utils.sh"

# Skip in CI
if is_ci; then
    exit 0
fi

# Check if Version.targets is staged
if ! is_staged "Version.targets"; then
    exit 0
fi

# Get current branch
BRANCH=$(git rev-parse --abbrev-ref HEAD)

# Allow on release branches
if [[ "$BRANCH" =~ ^release/ ]]; then
    log_info "Version.targets modification allowed on release branch"
    exit 0
fi

# Block on main or feature branches
if [[ "$BRANCH" == "main" ]] || [[ "$BRANCH" =~ ^feature/ ]] || [[ "$BRANCH" =~ ^hotfix/ ]]; then
    log_error "Manual modification of Version.targets detected!"
    echo ""
    echo "Version.targets should only be modified:"
    echo "  1. Automatically by version-bump workflow (after PR merge to main)"
    echo "  2. Manually when creating release branches (release/*)"
    echo ""
    echo "Current branch: $BRANCH"
    echo ""
    echo "If you're creating a release branch, rename your branch to release/X.Y.Z"
    exit 1
fi

exit 0
```

**File:** `hooks/commit-msg.d/01-conventional-format.sh`
```bash
#!/bin/bash
# Validate commit message follows conventional commit format

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "${SCRIPT_DIR}/shared/utils.sh"

COMMIT_MSG_FILE=$1
COMMIT_MSG=$(cat "$COMMIT_MSG_FILE")

# Skip merge commits
if [[ "$COMMIT_MSG" =~ ^Merge ]]; then
    exit 0
fi

# Skip automated commits
if [[ "$COMMIT_MSG" =~ \[skip\ ci\] ]] || [[ "$COMMIT_MSG" =~ \[ci\ skip\] ]]; then
    exit 0
fi

# Conventional commit pattern
PATTERN="^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\(.+\))?!?: .{1,100}"

if ! [[ "$COMMIT_MSG" =~ $PATTERN ]]; then
    log_error "Commit message does not follow Conventional Commits format!"
    echo ""
    echo "Format: <type>(<scope>): <description>"
    echo ""
    echo "Valid types: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert"
    echo "Breaking changes: Add ! after type (e.g., feat!:)"
    echo ""
    echo "Examples:"
    echo "  ✅ feat: add OpenTelemetry support"
    echo "  ✅ fix: resolve memory leak in logging"
    echo "  ✅ feat!: redesign IMicroService API"
    echo ""
    echo "Your message:"
    echo "  ❌ $COMMIT_MSG"
    exit 1
fi

log_info "Commit message follows Conventional Commits format ✓"
exit 0
```

**File:** `install.sh` (Hook Installer)
```bash
#!/bin/bash
# Install centralized hooks into a repository

set -e

HOOKS_REPO="${GITHOOKS_REPO:-https://github.com/cloud-tek/git-hooks.git}"
HOOKS_VERSION="${GITHOOKS_VERSION:-main}"
INSTALL_DIR=".git-hooks"
HOOKS_CACHE_DIR="${HOME}/.cache/cloud-tek-git-hooks"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}Installing cloud-tek Git Hooks...${NC}"

# Create hooks directory
mkdir -p "$INSTALL_DIR"

# Clone or update hooks repository to cache
if [ ! -d "$HOOKS_CACHE_DIR" ]; then
    echo "Cloning hooks repository to cache..."
    git clone --depth 1 --branch "$HOOKS_VERSION" "$HOOKS_REPO" "$HOOKS_CACHE_DIR"
else
    echo "Updating hooks repository cache..."
    cd "$HOOKS_CACHE_DIR"
    git fetch origin "$HOOKS_VERSION"
    git reset --hard "origin/$HOOKS_VERSION"
    cd - > /dev/null
fi

# Create config if it doesn't exist
if [ ! -f "$INSTALL_DIR/config.json" ]; then
    cat > "$INSTALL_DIR/config.json" <<EOF
{
  "version": "1.0.0",
  "enabled_hooks": {
    "pre-commit": true,
    "commit-msg": true,
    "pre-push": true
  },
  "rules": {
    "conventional_commits": true,
    "version_targets_check": true,
    "prevent_secrets": true,
    "dotnet_format": false
  }
}
EOF
    echo -e "${GREEN}Created default config: $INSTALL_DIR/config.json${NC}"
fi

# Install Git hooks that execute centralized scripts
install_hook() {
    local hook_name=$1
    local hook_path=".git/hooks/$hook_name"

    cat > "$hook_path" <<EOF
#!/bin/bash
# Auto-generated hook - Do not edit manually
# Managed by cloud-tek/git-hooks

HOOKS_CACHE="\${HOME}/.cache/cloud-tek-git-hooks"
HOOK_DIR="\${HOOKS_CACHE}/hooks/${hook_name}.d"

# Update cache if needed (once per day)
CACHE_AGE=\$(find "\$HOOKS_CACHE/.git/FETCH_HEAD" -mtime +1 2>/dev/null | wc -l)
if [ "\$CACHE_AGE" -gt 0 ]; then
    cd "\$HOOKS_CACHE" && git fetch origin ${HOOKS_VERSION} && git reset --hard origin/${HOOKS_VERSION} >/dev/null 2>&1 || true
    cd - >/dev/null
fi

# Execute all hook scripts in order
if [ -d "\$HOOK_DIR" ]; then
    for script in "\$HOOK_DIR"/*; do
        if [ -x "\$script" ]; then
            "\$script" "\$@" || exit \$?
        fi
    done
fi

exit 0
EOF

    chmod +x "$hook_path"
    echo -e "${GREEN}Installed: $hook_name${NC}"
}

# Install hooks
install_hook "pre-commit"
install_hook "commit-msg"
install_hook "pre-push"

# Save installed version
echo "$HOOKS_VERSION" > "$INSTALL_DIR/.version"

echo ""
echo -e "${GREEN}✅ Git hooks installed successfully!${NC}"
echo ""
echo "Hooks are managed centrally at: $HOOKS_REPO"
echo "Installed version: $HOOKS_VERSION"
echo "Configuration: $INSTALL_DIR/config.json"
echo ""
echo "To update hooks, run: .git-hooks/update.sh"
```

**File:** `update.sh` (Hook Updater)
```bash
#!/bin/bash
# Update hooks to latest version

set -e

HOOKS_CACHE_DIR="${HOME}/.cache/cloud-tek-git-hooks"
HOOKS_VERSION="${GITHOOKS_VERSION:-main}"

if [ ! -d "$HOOKS_CACHE_DIR" ]; then
    echo "Hooks not installed. Run install.sh first."
    exit 1
fi

echo "Updating hooks to version: $HOOKS_VERSION"
cd "$HOOKS_CACHE_DIR"
git fetch origin "$HOOKS_VERSION"
git reset --hard "origin/$HOOKS_VERSION"
cd - > /dev/null

echo "$HOOKS_VERSION" > ".git-hooks/.version"

echo "✅ Hooks updated successfully!"
```

---

#### Step 2: Per-Repository Integration

**Automated Installation via GitHub Actions**

Create `.github/workflows/setup-hooks.yml` in the centralized hooks repository:

```yaml
name: setup-hooks-template

# This workflow is designed to be copied to consuming repositories
# It ensures hooks are installed for all contributors

on:
  push:
    branches:
      - main
  workflow_dispatch:

jobs:
  setup-hooks:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5

      - name: Install Git Hooks
        run: |
          curl -fsSL https://raw.githubusercontent.com/cloud-tek/git-hooks/main/install.sh | bash

      - name: Commit hook configuration if changed
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"

          if [ -n "$(git status --porcelain .git-hooks/)" ]; then
            git add .git-hooks/
            git commit -m "chore: update git hooks configuration [skip ci]"
            git push
          fi
```

**Developer Setup Script**

Add to repository README or `CONTRIBUTING.md`:

```markdown
## Developer Setup

After cloning the repository, install Git hooks:

```bash
# One-time setup
curl -fsSL https://raw.githubusercontent.com/cloud-tek/git-hooks/main/install.sh | bash

# Or if you prefer to review first:
curl -fsSL https://raw.githubusercontent.com/cloud-tek/git-hooks/main/install.sh -o install-hooks.sh
chmod +x install-hooks.sh
./install-hooks.sh
```

Hooks will auto-update daily from the centralized repository.
```

---

### Option 2: Git Hook Templates (Alternative)

Use Git's built-in template directory feature:

**Setup Organization Template:**

```bash
# On developer machine or in onboarding script
git config --global init.templateDir ~/.git-template

# Clone centralized hooks
git clone https://github.com/cloud-tek/git-hooks.git ~/.git-template

# All new repositories automatically get hooks
git init new-repo  # Hooks are automatically copied
```

**Limitations:**
- Only applies to new repositories
- Existing repositories need manual migration
- Updates require re-running setup

---

### Option 3: Husky-Style Package Manager Integration

For repositories with package managers (npm, NuGet):

**Create NuGet Package: `CloudTek.GitHooks`**

```xml
<!-- CloudTek.GitHooks.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>true</IsPackable>
    <PackageId>CloudTek.GitHooks</PackageId>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <None Include="hooks/**/*" Pack="true" PackagePath="hooks/" />
    <None Include="install.sh" Pack="true" PackagePath="/" />
  </ItemGroup>

  <Target Name="InstallHooks" AfterTargets="Restore">
    <Exec Command="bash $(MSBuildThisFileDirectory)../CloudTek.GitHooks.*/install.sh" />
  </Target>
</Project>
```

**Per-Repository:**
```xml
<!-- Directory.Build.props -->
<Project>
  <ItemGroup>
    <PackageReference Include="CloudTek.GitHooks" Version="1.0.0" />
  </ItemGroup>
</Project>
```

Hooks install automatically on `dotnet restore`.

---

### Centralized Configuration Management

**Repository-Specific Overrides:**

`.git-hooks/config.json` in each repository:
```json
{
  "version": "1.0.0",
  "extends": "https://github.com/cloud-tek/git-hooks/config/default.json",
  "enabled_hooks": {
    "pre-commit": true,
    "commit-msg": true,
    "pre-push": true
  },
  "rules": {
    "conventional_commits": true,
    "version_targets_check": true,
    "prevent_secrets": true,
    "dotnet_format": true,
    "dotnet_test": false  // Override: disable for this repo
  },
  "overrides": {
    "version_targets_check": {
      "allowed_branches": ["main", "release/*"]
    }
  }
}
```

**Centralized Defaults:**

`cloud-tek/git-hooks/config/default.json`:
```json
{
  "version": "1.0.0",
  "enabled_hooks": {
    "pre-commit": true,
    "commit-msg": true,
    "pre-push": true
  },
  "rules": {
    "conventional_commits": true,
    "version_targets_check": true,
    "prevent_secrets": true,
    "dotnet_format": true,
    "dotnet_test": true
  },
  "thresholds": {
    "commit_message_length": 100,
    "line_length": 120
  }
}
```

---

### Enforcement and Compliance

**1. Repository Audit Workflow**

Create a centralized workflow to audit hook installation across all repos:

`.github/workflows/audit-hooks.yml` in `cloud-tek/standards`:
```yaml
name: audit-git-hooks

on:
  schedule:
    - cron: '0 0 * * 0'  # Weekly
  workflow_dispatch:

jobs:
  audit:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5

      - name: Audit all repositories
        run: |
          # List all repositories
          gh repo list cloud-tek --limit 1000 --json name -q '.[].name' > repos.txt

          # Check each repository for hooks
          while read repo; do
            if ! gh api "repos/cloud-tek/$repo/contents/.git-hooks/.version" >/dev/null 2>&1; then
              echo "❌ $repo - Hooks not installed"
            else
              version=$(gh api "repos/cloud-tek/$repo/contents/.git-hooks/.version" -q '.content' | base64 -d)
              echo "✅ $repo - Hooks version: $version"
            fi
          done < repos.txt
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**2. Required Hooks Check in CI**

Add to every repository's `validate.yml`:
```yaml
- name: Verify Git Hooks Installed
  run: |
    if [ ! -f ".git-hooks/.version" ]; then
      echo "❌ Git hooks not installed!"
      echo "Run: curl -fsSL https://raw.githubusercontent.com/cloud-tek/git-hooks/main/install.sh | bash"
      exit 1
    fi

    EXPECTED_VERSION="main"
    ACTUAL_VERSION=$(cat .git-hooks/.version)

    if [ "$ACTUAL_VERSION" != "$EXPECTED_VERSION" ]; then
      echo "⚠️  Hooks version mismatch: expected $EXPECTED_VERSION, got $ACTUAL_VERSION"
      echo "Run: .git-hooks/update.sh"
    fi
```

**3. Organization-Wide Policy Enforcement**

Use GitHub Rulesets to enforce hooks:

```yaml
# .github/rulesets/require-hooks.yml
name: Require Git Hooks
target: all
enforcement: active

rules:
  - type: required-file
    parameters:
      path: .git-hooks/.version

  - type: required-file
    parameters:
      path: .git-hooks/config.json
```

---

### Scaling to Hundreds of Repositories

#### Mass Deployment Strategy

**1. Create Migration Script**

`migrate-all-repos.sh`:
```bash
#!/bin/bash
# Migrate all repositories to centralized hooks

ORG="cloud-tek"
HOOK_INSTALLER_URL="https://raw.githubusercontent.com/cloud-tek/git-hooks/main/install.sh"

# Get all repositories
gh repo list "$ORG" --limit 1000 --json name -q '.[].name' | while read repo; do
    echo "Migrating: $repo"

    # Clone repository
    git clone "https://github.com/$ORG/$repo.git" "/tmp/$repo"
    cd "/tmp/$repo"

    # Install hooks
    curl -fsSL "$HOOK_INSTALLER_URL" | bash

    # Commit and push
    git checkout -b chore/install-git-hooks
    git add .git-hooks/
    git commit -m "chore: install centralized git hooks [skip ci]"
    git push -u origin chore/install-git-hooks

    # Create PR
    gh pr create \
        --title "chore: install centralized git hooks" \
        --body "Installs hooks from https://github.com/$ORG/git-hooks" \
        --base main

    # Cleanup
    cd -
    rm -rf "/tmp/$repo"

    sleep 2  # Rate limiting
done
```

**2. Phased Rollout**

Migrate repositories in phases:

- **Phase 1 (Week 1):** Pilot with 5-10 repositories
- **Phase 2 (Week 2):** Core infrastructure repositories (20-30)
- **Phase 3 (Week 3-4):** All active development repositories
- **Phase 4 (Week 5+):** Archived/low-activity repositories

**3. Communication Plan**

- Announce centralized hooks strategy
- Provide training/documentation
- Create Slack/Teams channel for support
- Monitor adoption via audit workflow

---

### Monitoring and Maintenance

**1. Hook Version Dashboard**

Create a dashboard showing hook versions across all repos:

```yaml
# .github/workflows/dashboard.yml
name: hooks-dashboard

on:
  schedule:
    - cron: '0 */6 * * *'  # Every 6 hours

jobs:
  dashboard:
    runs-on: ubuntu-latest
    steps:
      - name: Generate Dashboard
        run: |
          # Query all repos and generate markdown dashboard
          # Publish to GitHub Pages or wiki
```

**2. Automated Updates**

Create a workflow to auto-update hooks across all repos:

```yaml
# In cloud-tek/git-hooks repository
name: propagate-updates

on:
  push:
    tags:
      - 'v*'

jobs:
  update-all-repos:
    runs-on: ubuntu-latest
    steps:
      - name: Update all repositories
        run: |
          # Trigger update workflow in all consuming repos
          gh workflow run update-hooks.yml --repo cloud-tek/repo1
          gh workflow run update-hooks.yml --repo cloud-tek/repo2
          # ... or use GitHub API to trigger all at once
```

**3. Metrics and KPIs**

Track:
- % of repositories with hooks installed
- Hook version distribution
- Hook execution failures
- Commit message compliance rate
- Version.targets violation attempts

---

### Comparison Matrix

| Approach | Scalability | Maintenance | Developer UX | Control | Recommended For |
|----------|-------------|-------------|--------------|---------|-----------------|
| **Centralized Repo** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | **Organizations with 50+ repos** |
| **Git Templates** | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | Small teams, new repos only |
| **NuGet Package** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | .NET-only organizations |
| **Per-Repo Hooks** | ⭐ | ⭐ | ⭐⭐⭐ | ⭐⭐ | Single repository |

---

### Best Practices

1. **Version Your Hooks** - Use semantic versioning for hook releases
2. **Test Before Release** - Run hooks against test repositories
3. **Provide Escape Hatches** - Allow temporary bypass with justification
4. **Document Everything** - Clear README, troubleshooting guides
5. **Monitor Compliance** - Regular audits of hook installation
6. **Gradual Rollout** - Pilot changes before organization-wide deployment
7. **Backward Compatibility** - Maintain compatibility for 2-3 versions
8. **Performance** - Keep hooks fast (<2 seconds total)

---

## Conclusion

This strategy provides:

✓ **Clear separation of concerns** - Each workflow has single responsibility
✓ **Reusability** - Shared workflows across repositories
✓ **Trunk-based development** - Fast feedback, main always deployable
✓ **Quality gates** - Prevent broken code from reaching production
✓ **Flexibility** - Adapt to library, service, or monorepo structures
✓ **Security** - Built-in scanning and approval gates
✓ **Observability** - Clear audit trail and metrics

This is a living document that should evolve as your team's needs change and GitHub Actions capabilities expand.
