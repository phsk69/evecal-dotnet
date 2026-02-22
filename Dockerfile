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

# Runtime stage — rootless container fr fr 🔥
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# create non-root user so we running rootless bestie 🔒
RUN groupadd -g 1654 evecal && \
    useradd -u 1654 -g evecal -s /bin/false evecal

# create data and logs directories owned by evecal user
RUN mkdir -p /app/data /app/logs && \
    chown -R evecal:evecal /app/data /app/logs

# Copy published app
COPY --from=build --chown=evecal:evecal /app/publish .

# switch to non-root user before running anything 🔐
USER evecal

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV EVE_DATA_PATH=/app/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "EveCal.Api.dll"]
