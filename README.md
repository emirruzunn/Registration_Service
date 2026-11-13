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
