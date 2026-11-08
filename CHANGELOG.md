# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `GET /_dashboard/api/requests/{id}`: one HTTP request together with the SQL, logs and exceptions it produced.
- “Copy markdown” and “Copy JSON” in the details pane, exporting the selected request or the request a SQL/log event belongs to.
- Saved filters: the current type, search, duration and sort can be stored as a named chip (persisted in `localStorage`).
- “Slowest first” sort order for the event stream.
- Russian README (`README.ru.md`) with a language switch at the top of both files.
- Database tab: register an ADO.NET connection with `options.AddDataSource(...)` and browse its tables and first rows read-only. Table discovery uses the provider catalog (SQLite, SQL Server, `GetSchema` fallback); identifiers are quoted only after matching that catalog.
- `GET /_dashboard/api/db` and `GET /_dashboard/api/db/{source}/{table}?limit=`.
- Light theme: every colour is a CSS variable, so the palette swaps with one class on `<html>`. Follows `prefers-color-scheme` until the sidebar switch is used, and the choice is remembered. No flash of the wrong theme on load.
- Trace correlation: `TelemetryEvent.TraceId`/`SpanId` are filled from `Activity.Current`, so events line up with OpenTelemetry traces without an OpenTelemetry dependency. The details pane shows the trace and filters the stream by it; the API accepts `?traceId=`.

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
