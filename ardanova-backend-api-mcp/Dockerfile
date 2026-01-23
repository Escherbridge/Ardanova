# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose port
EXPOSE 8080

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "ArdaNova.API.dll"]
