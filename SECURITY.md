# Security policy

DotNetScope is a development tool. It is designed to run on a developer machine and it
captures data that is often sensitive: SQL parameters, request/response bodies and stack
traces.

## Supported versions

| Version | Supported |
|---------|-----------|
| 0.1.x   | ✅        |

## Reporting a vulnerability

Please **do not** open a public issue for a security problem.

Use GitHub's private reporting flow:
<https://github.com/PersiOnOff/dotnet-scope/security/advisories/new>

Include the version, a reproduction, and what an attacker could do with the issue. You can
expect an initial answer within a few days.

## Built-in protections

- Every `/_dashboard` route answers `403` unless the caller is loopback
  (`DevDashboardOptions.AllowRemoteAccess = false` by default).
- `DevDashboardOptions.Enabled = false` turns the dashboard into a 404 without removing the package.
- Nothing is persisted: telemetry lives in an in-memory ring buffer and dies with the process.
- Captured bodies are truncated to `MaxBodyChars` and the copy never buffers the full response stream.

## If you expose the dashboard

`AllowRemoteAccess = true` is meant for containers and remote dev boxes. If you enable it, put
authentication and TLS in front of the app — DotNetScope does not ship an authentication layer.
