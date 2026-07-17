# Agent instructions

## Dependencies and vulnerability checks

This is a **.NET Framework 4.7.2** MelonLoader mod. There is no `package.json`; do not add npm tooling unless the project explicitly adopts Node.js.

### Adding dependencies

- **NuGet packages**: add only when necessary. Pin an explicit version in `BlippoChannelHopper.csproj` (no floating ranges). After adding or updating a package, run:

  ```powershell
  dotnet restore
  dotnet list package --vulnerable
  ```

- **Local DLL references** (game / MelonLoader): document the source and version in the README. Never commit proprietary game binaries or MelonLoader DLLs to the repo.

- **Do not guess versions from memory.** Look up the current release on the official source (NuGet.org, MelonLoader GitHub releases) and use that exact version.

### Regular maintenance

- Before releases, run `dotnet list package --vulnerable` and fix or document any findings.
- If NuGet packages are added later, consider `dotnet outdated` (dotnet-outdated-tool) to see what can be upgraded.

### CI

Security checks run in GitHub Actions on every push and pull request (see `.github/workflows/security.yml`):

- `gitleaks` — scans git history for leaked secrets
- `dotnet list package --vulnerable` — fails on known vulnerable NuGet packages

## Secrets

- Never commit API keys, tokens, passwords, or private paths with credentials.
- `.env`, `libs/`, `decompiled/`, and `bin/` are gitignored — keep them that way.
- A **pre-commit hook** (`.githooks/pre-commit`) runs `gitleaks protect` on staged files. Enable once per clone:

  ```powershell
  git config core.hooksPath .githooks
  ```

## Building

- Default game path is in `BlippoChannelHopper.csproj`; override with `/p:BlippoGameDir=...` for local builds.
- Do not hardcode user-specific secrets in the project file.
