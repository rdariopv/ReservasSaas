# ==============================================================================
# Multi-stage Dockerfile for ReservasSaaS.Api (.NET 10)
# Hexagonal Architecture - Production Ready
# ==============================================================================

# Stage 1: Build and publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project definitions first to maximize Docker layer caching
COPY ["ReservasSaaS.sln", "./"]
COPY ["src/ReservasSaaS.Domain/ReservasSaaS.Domain.csproj", "src/ReservasSaaS.Domain/"]
COPY ["src/ReservasSaaS.Application/ReservasSaaS.Application.csproj", "src/ReservasSaaS.Application/"]
COPY ["src/ReservasSaaS.Infrastructure/ReservasSaaS.Infrastructure.csproj", "src/ReservasSaaS.Infrastructure/"]
COPY ["src/ReservasSaaS.Api/ReservasSaaS.Api.csproj", "src/ReservasSaaS.Api/"]
COPY ["tests/ReservasSaaS.UnitTests/ReservasSaaS.UnitTests.csproj", "tests/ReservasSaaS.UnitTests/"]

# Restore dependencies for all projects in solution
RUN dotnet restore "ReservasSaaS.sln"

# Copy full source tree
COPY . .

# Run unit tests during container build to ensure no regressions
#RUN dotnet test "tests/ReservasSaaS.UnitTests/ReservasSaaS.UnitTests.csproj" -c Release --no-restore --logger "console;verbosity=normal"

# Publish the API project in Release mode
WORKDIR "/src/src/ReservasSaaS.Api"
RUN dotnet publish "ReservasSaaS.Api.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true

# ==============================================================================
# Stage 2: Runtime image
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Non-root user for hardened container security (default in .NET 8+)
USER app

# Default ASP.NET Core environment and binding port
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

# Health check probing the GET /health endpoint
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

COPY --from=build --chown=app:app /app/publish .

ENTRYPOINT ["dotnet", "ReservasSaaS.Api.dll"]
