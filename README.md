# CampusHub – Registration Service

ASP.NET Core 8 (C#) ile yazilmis etkinlik kayit mikroservisi. EF Core + SQLite kullanir.

## Katmanlar
Controller -> Service -> Repository -> DbContext (SQLite). Event Service'e `IEventClient` ile HTTP uzerinden erisilir.

## Calistirma
```
cp .env.example .env
dotnet run
```
Swagger: http://localhost:8004/swagger

## Docker ile calistirma
Sadece Registration Service:
```
docker build -t registration-service .
docker run -p 8004:8004 -e EVENT_SERVICE_URL=http://host.docker.internal:8002 registration-service
```

Uctan uca lokal deneme (Registration Service + **sahte** Event Service):
```
docker compose up -d --build
curl http://localhost:8004/health
curl -X POST http://localhost:8004/registrations -H "Content-Type: application/json" -d "{\"userId\":42,\"eventId\":31}"
docker compose down
```
Grup 3'un Event Service'i hazir olana kadar `mock-event-service/` klasorundeki nginx tabanli sahte servis kullanilir (port 8002).
Hazir etkinlikler: `31` (kapasite 100), `14` (kapasite 50), `7` (kapasite 1, EVENT_FULL denemek icin).
Olmayan bir id 404, `500` id'si ise Event Service hatasi (503) dondurur. Event Service kapaliyken deneme: `docker compose stop event-service`.

- Imaj iki asamali (sdk -> aspnet), root olmayan `app` kullanicisiyla calisir.
- SQLite dosyasi `/app/data` altinda tutulur, compose'da `registration-data` volume'una baglidir.
- Container `HEALTHCHECK` ile `/health` endpoint'ini kontrol eder (`docker ps` -> healthy).

## Testler
xUnit ile `tests/RegistrationService.Tests` altinda:
- `RegistrationAppServiceTests` – is mantigi (kayit, 404, ayni etkinlige iki kez kayit 409, kapasite dolu 409, iptal, tekrar kayit, Event Service kapali 503)
- `HttpEventClientTests` – Event Service cagrisi (200, 404, 500, baglanti hatasi, timeout)
- `ApiTests` – servis bellekte ayaga kaldirilip HTTP ile test edilir (status kodlari ve ortak hata formati)

Event Service testlerde sahte (`FakeEventClient`) ile degistirilir, veritabani bellekte SQLite'tir; disariya baglanti gerekmez.
```
dotnet test tests/RegistrationService.Tests
```
.NET 8 SDK/ASP.NET 8 runtime kurulu degilse Docker icinde:
```
docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test tests/RegistrationService.Tests
```

## Ortam degiskenleri
| Degisken | Aciklama | Varsayilan |
|---|---|---|
| PORT | Servis portu | 8004 |
| DB_PATH | SQLite dosyasi | registrations.db |
| EVENT_SERVICE_URL | Event Service adresi | http://localhost:8002 |

## Endpointler
| Metod | Yol | Aciklama |
|---|---|---|
| POST | /registrations | Etkinlige kayit (201, 400, 404, 409, 503) |
| GET | /registrations/{id} | Kayit detayi |
| GET | /registrations?userId= / ?eventId= | Kullanici veya etkinlige gore liste |
| DELETE | /registrations/{id} | Kaydi iptal et |
| GET | /health | Saglik kontrolu |

Hata formati: `{ "error": "EVENT_NOT_FOUND", "message": "Event 31 does not exist." }`
