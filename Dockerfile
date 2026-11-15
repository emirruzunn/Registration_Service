# ---------- 1) Build asamasi: SDK imaji ile derle ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Once sadece csproj kopyalanir -> paketler degismedikce restore katmani cache'den gelir
COPY RegistrationService.csproj ./
RUN dotnet restore RegistrationService.csproj

# Kaynak kodun geri kalani (.dockerignore tests/, mock-event-service/, bin/, obj/ vb. disarida birakir)
COPY . ./
RUN dotnet publish RegistrationService.csproj -c Release -o /app/publish --no-restore

# ---------- 2) Runtime asamasi: sadece calisma zamani, daha kucuk imaj ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# HEALTHCHECK icin curl (aspnet imajinda yok)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# SQLite dosyasi icin yazilabilir klasor (volume buraya baglanir)
RUN mkdir -p /app/data && chown -R app:app /app/data

COPY --from=build /app/publish ./

# Varsayilan ortam degiskenleri (docker run -e / compose ile ezilebilir)
ENV PORT=8004 \
    DB_PATH=/app/data/registrations.db \
    EVENT_SERVICE_URL=http://event-container:8002

EXPOSE 8004

# Root olmayan kullanici ile calis (aspnet:8.0 imajinda hazir gelen "app" kullanicisi)
USER app

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -fsS http://localhost:${PORT}/health || exit 1

ENTRYPOINT ["dotnet", "RegistrationService.dll"]
