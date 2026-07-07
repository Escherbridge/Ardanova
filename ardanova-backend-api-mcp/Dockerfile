# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better caching
COPY ardanova.sln .
COPY api-server/src/ArdaNova.Domain/ArdaNova.Domain.csproj api-server/src/ArdaNova.Domain/
COPY api-server/src/ArdaNova.Application/ArdaNova.Application.csproj api-server/src/ArdaNova.Application/
COPY api-server/src/ArdaNova.Infrastructure/ArdaNova.Infrastructure.csproj api-server/src/ArdaNova.Infrastructure/
COPY api-server/src/ArdaNova.API/ArdaNova.API.csproj api-server/src/ArdaNova.API/
COPY api-server/src/ArdaNova.MCP/ArdaNova.MCP.csproj api-server/src/ArdaNova.MCP/

# Restore dependencies
RUN dotnet restore api-server/src/ArdaNova.API/ArdaNova.API.csproj

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/api-server/src/ArdaNova.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose port
EXPOSE 8080

# Run as the non-root 'app' user (UID 1654) pre-created in the .NET 8+ runtime image.
# Railway performs its own /health healthcheck (see railway.toml); no in-image curl needed.
USER app

# Start the application
ENTRYPOINT ["dotnet", "ArdaNova.API.dll"]
