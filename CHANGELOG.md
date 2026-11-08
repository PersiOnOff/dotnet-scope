# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1] - 2026-11-08

### Added

- Request bundle export: `GET /_dashboard/api/requests/{id}` returns one request with every SQL command, log record and exception it produced; the details pane copies it as markdown or JSON.
- Saved filters: the current type, search, duration and sort can be stored as a named chip and survive a reload.
- “Slowest first” sort order for the event stream.
- Database tab: `options.AddDataSource(...)` registers an ADO.NET connection whose tables can be browsed read-only, via `GET /_dashboard/api/db` and `GET /_dashboard/api/db/{source}/{table}?limit=`. Table discovery uses the provider catalog and an identifier is quoted only after matching it.
- Light theme: every colour is a CSS variable, so the palette swaps with one class on `<html>`; follows `prefers-color-scheme` until the sidebar switch is used.
- Trace correlation: `TraceId`/`SpanId` come from `Activity.Current`, so events line up with OpenTelemetry traces without an OpenTelemetry dependency. The UI filters by trace and the API accepts `?traceId=`.
- Russian README (`README.ru.md`) with a language switch at the top of both files.

### Changed

- Build toolchain: Vite 8, `@vitejs/plugin-react` 6 and TypeScript 7; the Node floor is now 20.19 (CI uses 22.12).
- CI and the release workflow run `npm run typecheck` before building the UI, so a type error fails the pipeline instead of shipping.
- Dependencies: EF Core 10.0.14, Microsoft.NET.Test.Sdk 18.11, SQLitePCLRaw 2.1.13, React 19.

## [0.1.0] - 2026-11-08

First public release — published on [nuget.org](https://www.nuget.org/packages/DotNetScope).

### Added

- HTTP capture: method, path, status code, duration, request and response bodies.
- EF Core SQL capture via `DiagnosticListener`, including parameter values.
- `ILogger` capture with framework-noise filtering.
- Request correlation: SQL commands, logs and exceptions are grouped with the HTTP request that produced them.
- Embedded React dashboard at `/_dashboard`: live event list, search, duration filter, SQL highlighting, JSON body viewer and stack-trace rendering.
- JSON API at `/_dashboard/api/events` with `type`, `q`, `minDurationMs` and `limit` filters.
- `DevDashboardOptions`: local-only access by default, in-memory capacity, body capture toggles.
- Sample app (`samples/SampleWebApi`) with EF Core + SQLite.
- Integration test suite running a real Kestrel host.

### Fixed

- Replaced the vulnerable transitive `SQLitePCLRaw` 2.1.11 in the sample with 2.1.12 (CVE-2025-6965).
