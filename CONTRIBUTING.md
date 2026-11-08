# Contributing to DotNetScope

Thanks for wanting to help. This project has one rule above all others:

> **It must stay zero-config.** A few lines of registration, no external service, no daemon.

## Good first issues

- A new UI affordance (saved filters, copy-as-curl, light theme).
- Better handling of one event type (for example `Microsoft.Data.SqlClient` commands next to EF Core ones).
- Documentation: a screenshot, a recipe, a "how it works" explanation.

## Development setup

Requirements: .NET 10 SDK, Node 20+.

```bash
# UI: Vite writes the bundle straight into the core project
cd src/DevDashboard.UI
npm ci
npm run build

# back at the repo root
dotnet build src/DevDashboard.Core/DevDashboard.Core.csproj
dotnet test tests/DevDashboard.Tests/DevDashboard.Tests.csproj
dotnet run --project samples/SampleWebApi
```

Then open <http://localhost:5000/_dashboard> and hit some endpoints:

```bash
curl http://localhost:5000/api/todos
curl -X POST "http://localhost:5000/api/todos?title=try%20it"
curl http://localhost:5000/api/checkout   # logs, SQL and an exception in one request
```

The built UI lives in `src/DevDashboard.Core/wwwroot` and is committed so that
`git clone && dotnet run --project samples/SampleWebApi` works without Node. If you change
the UI, rebuild it and commit the new bundle together with your source change.

## Before opening a pull request

1. `dotnet build src/DevDashboard.Core/DevDashboard.Core.csproj` — no new warnings.
2. `dotnet test tests/DevDashboard.Tests/DevDashboard.Tests.csproj` — all green.
3. `cd src/DevDashboard.UI && npx tsc --noEmit` — the UI type-checks.
4. Keep public API changes documented with XML comments (the package generates docs).
5. Add a line to `CHANGELOG.md` under “Unreleased”.

## Code style

- C#: file-scoped namespaces, nullable enabled, `var` when the type is obvious, guard clauses.
- React: function components, hooks, no global state library.
- Comments explain *why*, not *what*.
- Every public member gets an XML doc comment; the build treats missing ones as warnings.

## Commit messages

Short imperative subject, then a body explaining the reasoning if it is not obvious:

```
Add request correlation id to SQL events

EF Core's CommandExecuted fires inside the request's async flow, so an
AsyncLocal id is enough to group commands with their request.
```

## Questions

Open a discussion or an issue — see the templates in `.github/ISSUE_TEMPLATE`.
