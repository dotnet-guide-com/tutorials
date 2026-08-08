# OpenTelemetry Correlated Signals &mdash; Minimal .NET 10 Companion

> Full tutorial: [.NET 8 Observability with OpenTelemetry: Tracing, Metrics & Structured Logging](https://www.dotnet-guide.com/tutorials/dotnet-8-essentials/observability-opentelemetry/)

## What this sample demonstrates

- ASP.NET Core Minimal API with OpenTelemetry 1.17.0
- Custom `ActivitySource` for manual tracing via `Checkout.Process` spans
- Custom `Meter` with a `counter` (`checkout.requests`) and a `histogram` (`checkout.duration`)
- Low-cardinality metric tags (`checkout.channel`, `checkout.outcome`)
- Source-generated structured `ILogger` events via `LoggerMessage`
- Automatic log-to-trace correlation from the active `Activity`
- Learning-only OpenTelemetry Console Exporter output
- Input validation with rejection telemetry
- `ActivityListener` and `MeterListener` unit tests
- Deterministic API smoke tests without external backends

## Architecture

```
POST /checkout
      ↓
ASP.NET Core request Activity
      ↓
custom Checkout.Process Activity
      ↓
structured ILogger event
      ↓
checkout.requests counter
      ↓
checkout.duration histogram
      ↓
OpenTelemetry SDK
      ↓
Console Exporter
```

## File structure

```
dotnet-8-essentials/observability-opentelemetry/
├── OpenTelemetrySignalsMinimal.slnx
├── README.md
├── src/OpenTelemetrySignalsMinimal/
│   ├── OpenTelemetrySignalsMinimal.csproj
│   ├── Program.cs
│   ├── Models/
│   │   └── CheckoutModels.cs
│   └── Telemetry/
│       ├── CheckoutLog.cs
│       └── CheckoutTelemetry.cs
└── tests/OpenTelemetrySignalsMinimal.Tests/
    ├── OpenTelemetrySignalsMinimal.Tests.csproj
    └── OpenTelemetrySignalsTests.cs
```

## Prerequisites

- .NET 10.0 SDK or later
- No Docker, collector, or external backend required

## Run

```bash
cd dotnet-8-essentials/observability-opentelemetry
dotnet restore OpenTelemetrySignalsMinimal.slnx
dotnet build --configuration Release
dotnet run --project src/OpenTelemetrySignalsMinimal --configuration Release --urls http://127.0.0.1:5099
```

## Verify

```bash
# Root endpoint — fixed metadata
curl --silent http://127.0.0.1:5099/
# {"sample":"correlated-opentelemetry-signals","exporter":"console","activitySource":"DotNetGuide.Observability.Checkout","meter":"DotNetGuide.Observability.Checkout"}

# Successful checkout — returns trace ID
curl --silent --request POST \
  --header "Content-Type: application/json" \
  --data '{"channel":"WEB","itemCount":2}' \
  http://127.0.0.1:5099/checkout
# {"status":"accepted","channel":"web","itemCount":2,"traceId":"<32 hex chars>"}

# Invalid channel — returns 400
curl --silent --request POST \
  --header "Content-Type: application/json" \
  --data '{"channel":"customer-92823","itemCount":2}' \
  http://127.0.0.1:5099/checkout

# Out-of-range item count — returns 400
curl --silent --request POST \
  --header "Content-Type: application/json" \
  --data '{"channel":"mobile","itemCount":11}' \
  http://127.0.0.1:5099/checkout
```

## Tests

```bash
dotnet test --configuration Release --no-build
```

## Important boundary

- **Console Exporter** is for learning and debugging only. It is **not** recommended for production use, and its text output is not a standardized transport contract.
- OTLP, Collector, Docker Compose, Jaeger, Prometheus, Grafana, and cloud exporters are intentionally excluded.
- EF Core, HttpClient instrumentation, external service calls, queues, sampling, baggage, and health checks are not included.
- The full tutorial at [dotnet-guide.com](https://www.dotnet-guide.com/tutorials/dotnet-8-essentials/observability-opentelemetry/) covers production observability architecture with OTLP &rarr; Collector &rarr; backends.

## Verification table

| Item | Value |
|------|-------|
| Target framework | `net10.0` |
| OpenTelemetry.Extensions.Hosting | 1.17.0 |
| OpenTelemetry.Exporter.Console | 1.17.0 |
| OpenTelemetry.Instrumentation.AspNetCore | 1.17.0 |
| External services | None |
| Last reviewed | 2026-08-08 |

## License

This sample is provided for educational purposes as part of the [DOTNET GUIDE](https://www.dotnet-guide.com) tutorial series.