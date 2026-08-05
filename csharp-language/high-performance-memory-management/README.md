# UTF-8 Pipelines Log Ingestor

A focused .NET 10 companion demonstrating newline framing with `PipeReader`,
segmented `ReadOnlySequence<byte>` handling, span-based numeric parsing,
bounded pooled copies, explicit buffer ownership, and deterministic tests.

## Full tutorial

[High-Performance C#: Span<T>, Memory<T>, SIMD & Pipelines](https://www.dotnet-guide.com/tutorials/csharp-language/high-performance-memory-management/)

## Framework and language note

The tutorial is framed around .NET 8.

This companion targets .NET 10 because that is the DOTNET GUIDE repository's
current SDK, but both projects explicitly set C# 12.0.

The parser keeps `ReadOnlySpan<byte>` work in synchronous methods and does not
depend on C# 13's relaxed ref-struct rules for async methods.

## Input format

```text
<unix-seconds>|<level>|<event-id>|<message>
```

Example:

```text
1785924000|2|1001|Cache warmed
```

This is a deliberately small pipe-delimited log format.

It is not a complete CSV or NDJSON implementation.

## Pipeline flow

```text
Stream
  -> PipeReader
  -> ReadOnlySequence<byte>
  -> newline framing
  -> direct single-segment parse
     or bounded pooled multi-segment copy
  -> Utf8Parser
  -> validated LogEntry
```

## Single-segment path

When a complete line occupies one sequence segment, the decoder passes
`FirstSpan` directly to the parser.

No line-copy buffer is created for that path.

The valid result still owns a message string and a `LogEntry`.

## Multi-segment path

When a line crosses sequence segments:

1. reject it if it exceeds 4,096 bytes;
2. rent from `MemoryPool<byte>.Shared`;
3. use only the requested memory prefix;
4. copy the logical line;
5. parse the copied span;
6. dispose `IMemoryOwner<byte>` before returning.

No pooled reference escapes.

## Parsing rules

- all three numeric fields must be consumed completely;
- level must be from 0 through 5;
- event ID must be positive;
- the message must be non-empty valid UTF-8;
- LF and CRLF are supported;
- the last line can omit a trailing newline;
- malformed lines are counted and skipped.

## Allocation boundary

This sample does not claim zero allocation.

A valid result creates an owned message string and `LogEntry`.

The sample avoids intermediate strings for numeric parsing and avoids copying
contiguous logical lines.

Measure the real workload before making performance claims.

## Stream ownership

The ingestor creates and completes its `PipeReader` but configures it to leave
the caller's stream open.

Caller cancellation propagates as cancellation.

## Oversized-record handling

The ingestor enforces the 4,096-byte logical line limit *before* a newline
arrives, not just when the decoder receives a complete line.

On each `PipeReader` read, after all complete LF-delimited lines are extracted,
the ingestor examines the length of the current retained, unterminated
`ReadOnlySequence<byte>`. Because the sequence already includes bytes retained
from earlier reads, its length is not accumulated again.

When the retained sequence exceeds 4,097 bytes (4,096 plus one optional CR
byte):

1. the record is counted exactly once (total and invalid);
2. the ingestor enters discard mode;
3. incoming bytes are consumed but not retained;
4. after the next LF, normal parsing resumes with the following record.

This prevents an unterminated or oversized record from causing unbounded
retained `PipeReader` data. The sample's deterministic output does not
include oversized records because the sample data stays within the limit.

## Pooled-copy accounting

`PooledCopies` counts every logical line that required the multi-segment
pooled-copy path, including malformed lines where the copy was performed
before the decoder determined the line was invalid.

## Application dependencies

The application targets `net10.0` and uses only types from the .NET 10
shared framework. It has no direct NuGet package references.

`System.IO.Pipelines` is provided by the .NET 10 target framework and does
not need a pinned package reference for this sample.

## Prerequisite

- .NET 10 SDK

## Restore, build, test, and run

```powershell
dotnet restore `
  .\HighPerformanceLogIngestor.slnx

dotnet build `
  .\HighPerformanceLogIngestor.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\HighPerformanceLogIngestor.slnx `
  --configuration Release `
  --no-build

dotnet run `
  --project .\src\HighPerformanceLogIngestor\HighPerformanceLogIngestor.csproj `
  --configuration Release `
  --no-build
```

## Expected output

```text
High-Performance UTF-8 Log Ingestor
Lines: total=4, valid=3, invalid=1
Accepted event IDs: 1001, 1002, 1004
Levels: 1=1, 2=1, 4=1
First message: Cache warmed
Last message: Worker started
```

## Project structure

```text
HighPerformanceLogIngestor.slnx
README.md
src/
`-- HighPerformanceLogIngestor/
    |-- HighPerformanceLogIngestor.csproj
    |-- Program.cs
    |-- Models/
    |   `-- LogEntry.cs
    |-- Parsing/
    |   |-- LogLineDecoder.cs
    |   `-- Utf8LogLineParser.cs
    `-- Pipelines/
        |-- LogIngestionResult.cs
        `-- LogIngestor.cs
tests/
`-- HighPerformanceLogIngestor.Tests/
    |-- HighPerformanceLogIngestor.Tests.csproj
    |-- HighPerformanceLogTests.cs
    `-- TestSupport/
        |-- ChunkedReadStream.cs
        `-- SegmentedSequence.cs
```

## Deliberately omitted

- stack allocation;
- unsafe code;
- `MemoryMarshal`;
- SIMD;
- hardware intrinsics;
- BenchmarkDotNet;
- ASP.NET Core;
- NDJSON;
- full CSV quoting;
- performance claims.

These remain in the complete tutorial or need dedicated measured samples.

## Verification

- Companion target framework: .NET 10
- Explicit language version: C# 12.0
- System.IO.Pipelines: provided by the .NET 10 shared framework
- NuGet package dependencies: none
- Maximum logical line length: 4,096 bytes
- External services required: none
- Expected tests: 8
- Last reviewed: 2026-08-05

This sample demonstrates ownership and parsing semantics. It does not establish
production throughput, allocation rates, or hardware-specific speedups.