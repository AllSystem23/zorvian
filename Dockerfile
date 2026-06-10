# ══════════════════════════════════════════════════════════════
# Zorvian ERP — Optimized Multi-Stage Dockerfile
# ══════════════════════════════════════════════════════════════

# ── Stage 1: Restore dependencies ──
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY src/Zorvian.Web/Zorvian.Web.csproj src/Zorvian.Web/
COPY src/Zorvian.Application/Zorvian.Application.csproj src/Zorvian.Application/
COPY src/Zorvian.Core/Zorvian.Core.csproj src/Zorvian.Core/
COPY src/Zorvian.Infrastructure/Zorvian.Infrastructure.csproj src/Zorvian.Infrastructure/
RUN dotnet restore src/Zorvian.Web/Zorvian.Web.csproj --runtime linux-x64

# ── Stage 2: Build & Publish ──
FROM restore AS build
COPY . .
RUN dotnet publish src/Zorvian.Web/Zorvian.Web.csproj \
    -c Release \
    -o /app \
    --no-restore \
    --runtime linux-x64 \
    --self-contained false \
    /p:PublishTrimmed=false \
    /p:PublishReadyToRun=true

# ── Stage 3: Runtime ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Security: run as non-root user
RUN addgroup -S zorvian && adduser -S zorvian -G zorvian

# Install only what's needed for health checks
RUN apk add --no-cache curl \
    && rm -rf /var/cache/apk/*

WORKDIR /app

# Copy published output
COPY --from=build /app .

# Set ownership
RUN chown -R zorvian:zorvian /app

USER zorvian

# Environment
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_gcServer=1 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Zorvian.Web.dll"]