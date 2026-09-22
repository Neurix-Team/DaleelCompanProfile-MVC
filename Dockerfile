# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY Daleel/Daleel.csproj Daleel/
COPY Daleel.BAL/Daleel.BAL.csproj Daleel.BAL/
COPY Daleel.DAL/Daleel.DAL.csproj Daleel.DAL/
RUN dotnet restore Daleel/Daleel.csproj

# Copy everything and build
COPY . .
RUN dotnet publish Daleel/Daleel.csproj \
    --configuration Release \
    --output /app/publish \
    --self-contained false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80

ENTRYPOINT ["dotnet", "Daleel.dll"]