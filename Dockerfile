# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy solution file
COPY src/FreeStays.sln ./
# Copy project files
COPY src/FreeStays.API/FreeStays.API.csproj ./FreeStays.API/
COPY src/FreeStays.Application/FreeStays.Application.csproj ./FreeStays.Application/
COPY src/FreeStays.Domain/FreeStays.Domain.csproj ./FreeStays.Domain/
COPY src/FreeStays.Infrastructure/FreeStays.Infrastructure.csproj ./FreeStays.Infrastructure/
# Restore dependencies
RUN dotnet restore FreeStays.API/FreeStays.API.csproj
# Copy all source files
COPY src/ ./
# Build the application
RUN dotnet build FreeStays.API/FreeStays.API.csproj -c Release -o /app/build
# Publish the application
RUN dotnet publish FreeStays.API/FreeStays.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install tzdata and curl for timezone support and healthcheck
RUN apt-get update && apt-get install -y tzdata curl && rm -rf /var/lib/apt/lists/*

# Set timezone to Istanbul
ENV TZ=Europe/Istanbul

# Copy published files from build stage
COPY --from=build /app/publish .

# Create necessary directories
RUN mkdir -p /app/logs && chmod 777 /app/logs
RUN mkdir -p /app/wwwroot/uploads && chmod 777 /app/wwwroot/uploads

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "FreeStays.API.dll"]