# syntax=docker/dockerfile:1

# ─────────────────────────────────────────────
# Stage 1: Build Tailwind CSS
# Scans the views/C# sources so the CSS always matches the markup.
# ─────────────────────────────────────────────
FROM node:22-alpine AS css
WORKDIR /src/Daleel

COPY Daleel/package.json Daleel/package-lock.json* ./
RUN if [ -f package-lock.json ]; then npm ci --no-audit --no-fund; else npm install --no-audit --no-fund; fi

COPY Daleel/tailwind.config.js ./
COPY Daleel/Styles ./Styles
COPY Daleel/Views ./Views
COPY Daleel/Controllers ./Controllers
COPY Daleel/Models ./Models
COPY Daleel/wwwroot/js ./wwwroot/js
COPY Daleel.BAL /src/Daleel.BAL
COPY Daleel.DAL /src/Daleel.DAL
RUN npm run build:css

# ─────────────────────────────────────────────
# Stage 2: Restore & publish .NET app
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore first so this layer is cached until a csproj changes
COPY Daleel/Daleel.csproj Daleel/
COPY Daleel.BAL/Daleel.BAL.csproj Daleel.BAL/
COPY Daleel.DAL/Daleel.DAL.csproj Daleel.DAL/
RUN dotnet restore Daleel/Daleel.csproj

COPY Daleel/ Daleel/
COPY Daleel.BAL/ Daleel.BAL/
COPY Daleel.DAL/ Daleel.DAL/
COPY --from=css /src/Daleel/wwwroot/css/tailwind.css Daleel/wwwroot/css/tailwind.css

RUN dotnet publish Daleel/Daleel.csproj \
    --configuration $BUILD_CONFIGURATION \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# ─────────────────────────────────────────────
# Stage 3: Runtime
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_PRINT_TELEMETRY_MESSAGE=false

COPY --from=build /app/publish .

# Runtime-writable folders: media uploads + ASP.NET data-protection keys
RUN mkdir -p /app/wwwroot/uploads /root/.aspnet/DataProtection-Keys

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD bash -c ':> /dev/tcp/127.0.0.1/80' || exit 1

ENTRYPOINT ["dotnet", "Daleel.dll"]
