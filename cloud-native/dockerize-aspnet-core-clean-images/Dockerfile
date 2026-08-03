# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS restore
WORKDIR /src

COPY ["src/ContainerizedApiMinimal/ContainerizedApiMinimal.csproj", "src/ContainerizedApiMinimal/"]
COPY ["src/ContainerizedApiMinimal/packages.lock.json", "src/ContainerizedApiMinimal/"]

COPY ["src/ContainerHealthProbe/ContainerHealthProbe.csproj", "src/ContainerHealthProbe/"]
COPY ["src/ContainerHealthProbe/packages.lock.json", "src/ContainerHealthProbe/"]

RUN dotnet restore \
    "src/ContainerizedApiMinimal/ContainerizedApiMinimal.csproj" \
    --locked-mode \
    && dotnet restore \
    "src/ContainerHealthProbe/ContainerHealthProbe.csproj" \
    --locked-mode

FROM restore AS publish

COPY ["src/ContainerizedApiMinimal/", "src/ContainerizedApiMinimal/"]
COPY ["src/ContainerHealthProbe/", "src/ContainerHealthProbe/"]

RUN dotnet publish \
    "src/ContainerizedApiMinimal/ContainerizedApiMinimal.csproj" \
    --configuration Release \
    --no-restore \
    --output /out/app \
    /p:UseAppHost=false \
    && dotnet publish \
    "src/ContainerHealthProbe/ContainerHealthProbe.csproj" \
    --configuration Release \
    --no-restore \
    --output /out/probe \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=publish \
    --chown=app:app \
    /out/app/ \
    ./

COPY --from=publish \
    --chown=app:app \
    /out/probe/ \
    ./

USER $APP_UID

EXPOSE 8080

HEALTHCHECK \
    --interval=30s \
    --timeout=5s \
    --start-period=10s \
    --retries=3 \
    CMD ["dotnet", "ContainerHealthProbe.dll"]

ENTRYPOINT ["dotnet", "ContainerizedApiMinimal.dll"]