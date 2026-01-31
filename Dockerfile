# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY src/EveCal.Api/EveCal.Api.csproj ./EveCal.Api/
RUN dotnet restore ./EveCal.Api/EveCal.Api.csproj

# Copy source and build
COPY src/EveCal.Api/ ./EveCal.Api/
WORKDIR /src/EveCal.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create data directory for token storage
RUN mkdir -p /app/data && chmod 777 /app/data

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV EVE_DATA_PATH=/app/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "EveCal.Api.dll"]
