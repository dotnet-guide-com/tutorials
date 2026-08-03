# Dockerized ASP.NET Core API — Minimal Sample

A focused .NET 10 companion demonstrating a multi-stage Dockerfile, locked
restore, a runtime-only final image, non-root execution, port 8080, runtime
configuration, liveness/readiness endpoints, a .NET-based Docker health probe,
a hardened Compose service, integration tests, and CI container smoke checks.

## Full tutorial

[Dockerizing ASP.NET Core: Multi-Stage Builds, Clean Images & a Production-Ready Ship Workflow](https://www.dotnet-guide.com/tutorials/cloud-native/dockerize-aspnet-core-clean-images/)

## Framework note

The tutorial explains ASP.NET Core containers on .NET 8.

This companion targets .NET 10 so it can use the DOTNET GUIDE repository's
current SDK and CI toolchain. The multi-stage-build, locked-restore, non-root,
port-8080, runtime-configuration, health-check, Docker Compose, and
container-verification practices are the same core container concepts.

## Image note

The Dockerfile uses:

```text
mcr.microsoft.com/dotnet/sdk:10.0-noble
mcr.microsoft.com/dotnet/aspnet:10.0-noble
```

These are maintained channel tags, not immutable image references.

Rebuilding can pull newer patched image content. Teams requiring byte-for-byte
base-image reproducibility can pin reviewed digests and update them through a
controlled dependency process.

## What this sample demonstrates

- restore, publish, and final Docker stages;
- committed package lock files;
- `dotnet restore --locked-mode`;
- project-file-first Docker cache ordering;
- a runtime-only final image;
- the built-in .NET `app` user via `$APP_UID`;
- port 8080;
- no SDK in the final image;
- no installed `curl` or `wget`;
- a small .NET health-probe executable;
- `/health/live`;
- `/health/ready`;
- `/health`;
- configuration through `Sample__Message`;
- a read-only container filesystem;
- dropped Linux capabilities;
- `no-new-privileges`;
- integration and container smoke tests.

## Container flow

```text
SDK restore stage
  -> SDK publish stage
  -> ASP.NET Core runtime stage
  -> numeric non-root user
  -> Kestrel on 8080
  -> .NET Docker health probe
```

## Why the health check is another .NET project

The final image doesn't install an operating-system HTTP tool solely for
`HEALTHCHECK`.

`ContainerHealthProbe` sends a short HTTP request to the local liveness endpoint
and returns exit code 0 or 1.

Docker remains responsible for scheduling, timeouts, and retry counts.

## Prerequisites

- .NET 10 SDK
- Docker Engine or Docker Desktop
- Docker Compose v2
- `curl` on the host only for manual HTTP checks

## Restore, build, and test

```powershell
dotnet restore `
  .\ContainerizedApiMinimal.slnx

dotnet build `
  .\ContainerizedApiMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\ContainerizedApiMinimal.slnx `
  --configuration Release `
  --no-build
```

## Build the image

```powershell
docker build `
  --pull `
  --tag dotnet-guide/containerized-api-minimal:local `
  .
```

## Run the hardened container

```powershell
docker run `
  --detach `
  --name containerized-api-minimal `
  --publish 127.0.0.1:5152:8080 `
  --env ASPNETCORE_ENVIRONMENT=Production `
  --env Sample__Message="Configured at docker run time" `
  --read-only `
  --tmpfs /tmp `
  --cap-drop ALL `
  --security-opt no-new-privileges:true `
  dotnet-guide/containerized-api-minimal:local
```

Open:

```text
http://127.0.0.1:5152/api/todos
http://127.0.0.1:5152/info
http://127.0.0.1:5152/health/live
http://127.0.0.1:5152/health/ready
```

## Check Docker health

```powershell
docker inspect `
  --format="{{.State.Health.Status}}" `
  containerized-api-minimal
```

## Confirm the default user

```powershell
docker image inspect `
  dotnet-guide/containerized-api-minimal:local `
  --format="{{.Config.User}}"
```

The result must not be empty, `0`, or `root`.

## Confirm the SDK is absent

```powershell
docker exec `
  containerized-api-minimal `
  dotnet --list-sdks
```

No SDK version should be returned.

## Stop the manual container

```powershell
docker rm --force containerized-api-minimal
```

Remove the manual container before starting the Docker Compose service. Both
workflows bind to `127.0.0.1:5152` and leaving a running container causes a
port conflict.

## Docker Compose

```powershell
docker compose config --quiet
docker compose up --build --detach
docker compose ps
docker compose down --remove-orphans
```

## Configuration boundary

`Sample__Message` is intentionally non-sensitive.

Environment variables can be inspected through container tooling. Use a
platform secret mechanism for real credentials.

Never place a credential in:

- the Dockerfile;
- `ARG`;
- `ENV`;
- a committed Compose file;
- `appsettings.json`;
- an image label.

## Health boundary

The liveness endpoint tests whether the process can execute.

The readiness endpoint verifies that required sample configuration is present.

Neither endpoint checks a database because this lightweight sample has no
database.

## Security boundary

The sample applies useful container defaults, but these controls don't replace:

- application authorization;
- vulnerability management;
- image signing;
- registry policy;
- network policy;
- orchestrator security context;
- secret management;
- operating-system patching.

## Project structure

```text
ContainerizedApiMinimal.slnx
Dockerfile
.dockerignore
compose.yaml
README.md
src/
|-- ContainerizedApiMinimal/
|   |-- ContainerizedApiMinimal.csproj
|   |-- Program.cs
|   |-- appsettings.json
|   `-- packages.lock.json
|-- ContainerHealthProbe/
|   |-- ContainerHealthProbe.csproj
|   |-- Program.cs
|   `-- packages.lock.json
tests/
`-- ContainerizedApiMinimal.Tests/
    |-- ContainerizedApiMinimal.Tests.csproj
    `-- ContainerizedApiTests.cs
```

## Deliberately omitted

- database health checks;
- private package feeds;
- build secrets;
- image publishing;
- cloud deployment;
- Kubernetes;
- multi-architecture publication;
- image signing;
- fixed image-size targets;
- production restart policy.

These remain in the full tutorial or a future deployment sample.

## Verification

- Companion target framework: .NET 10
- Tutorial framework: .NET 8
- Build image: .NET 10 SDK on Ubuntu Noble
- Final image: ASP.NET Core 10 runtime on Ubuntu Noble
- Container port: 8080
- Runtime user: non-root `$APP_UID`
- Application packages: none
- External services required: none
- Database required: none
- API keys required: none
- Expected integration tests: 5
- Container smoke test: required
- Last reviewed: 2026-08-03

This sample is educational and must be reviewed against the target registry,
host, orchestrator, and security requirements before production use.