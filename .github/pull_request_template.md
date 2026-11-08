## What changed

<!-- One or two sentences. Link the issue it closes, if any: `Closes #12`. -->

## Why

<!-- The reasoning a reviewer cannot get from the diff. -->

## Screenshots

<!-- For UI changes: before / after. -->

## Checklist

- [ ] `dotnet build src/DevDashboard.Core/DevDashboard.Core.csproj` produces no new warnings
- [ ] `dotnet test tests/DevDashboard.Tests/DevDashboard.Tests.csproj` is green
- [ ] `npx tsc --noEmit` in `src/DevDashboard.UI` passes (UI changes)
- [ ] The UI bundle in `src/DevDashboard.Core/wwwroot` was rebuilt and committed (UI changes)
- [ ] New public members have XML doc comments
- [ ] `CHANGELOG.md` has an entry under “Unreleased”
- [ ] The change keeps the “zero-config” promise
