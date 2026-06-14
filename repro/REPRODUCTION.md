# Reproduction: `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES` (issue #2185)

This branch is **reproduction evidence only** — it is *not* part of the eventual pull request.
The actual integration test lands on the separate `add-legacy-sources-integration-test` branch.

## What is being demonstrated

`OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES` registers "legacy" activity sources via
`AddLegacySource(...)`. A legacy activity is one created the old way — `new Activity("name")` — with
**no** `ActivitySource`. The SDK only collects it when its operation name is listed in this variable.
The feature works at runtime but has no automated integration test; #2185 asks for one.

## How to reproduce

Prerequisites: .NET 8 & 9 SDKs, **.NET 10 SDK** (the pinned `nuke` 10.1.0 tool needs it), Docker, Xcode CLT.

1. Build the instrumentation from the repo root:
   ```sh
   dotnet tool restore && dotnet nuke BuildTracer   # produces bin/tracer-home
   ```
2. Build this minimal app (emits one legacy activity — see `legacy-repro/Program.cs`):
   ```sh
   dotnet build -c Release repro/legacy-repro
   ```
3. Run it instrumented with the console exporter. On macOS the default log dir
   (`/var/log/opentelemetry`) is not writable, so redirect it:
   ```sh
   export OTEL_DOTNET_AUTO_HOME="$(pwd)/bin/tracer-home"
   export OTEL_DOTNET_AUTO_LOG_DIRECTORY=/tmp/otel-logs; mkdir -p "$OTEL_DOTNET_AUTO_LOG_DIRECTORY"
   export OTEL_TRACES_EXPORTER=console OTEL_METRICS_EXPORTER=none OTEL_LOGS_EXPORTER=none
   export OTEL_SERVICE_NAME=legacy-repro
   . "$OTEL_DOTNET_AUTO_HOME/instrument.sh"

   # WITH the variable -> span IS exported
   OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES=ManualSpan \
     dotnet repro/legacy-repro/bin/Release/net8.0/legacy-repro.dll

   # WITHOUT the variable -> span is NOT exported
   dotnet repro/legacy-repro/bin/Release/net8.0/legacy-repro.dll
   ```

## Observed output

### Scenario 1 — WITH `OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES=ManualSpan`

```
Activity.TraceId:            3bedba81ee609a342e2d89c82a02545a
Activity.SpanId:             69aa7d1535dee066
Activity.TraceFlags:         Recorded
Activity.DisplayName:        ManualSpan
Activity.Kind:               Internal
Instrumentation scope (ActivitySource):
    Name:                       <-- EMPTY for legacy activities
    service.name: legacy-repro
[app] emitted legacy activity 'ManualSpan', recorded=True
```

The legacy span **is exported**.

### Scenario 2 — WITHOUT the variable

```
[app] emitted legacy activity 'ManualSpan', recorded=False
```

No `Activity.*` output — the span is **not exported**.

## Confirming the test gap

```sh
grep -rn "ADDITIONAL_LEGACY_SOURCES\|AddLegacySource" test/IntegrationTests/   # -> no matches
```

## Key finding driving the test design

Legacy activities export under an **empty instrumentation scope name** (note `Name:` is blank above).
So the integration test must assert against an empty scope:

```csharp
collector.Expect("", span => span.Name == "ManualSpan");
```
