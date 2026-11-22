# Registration Service – API Sözleşmesi (Grup 4)

Bu belge diğer gruplarla entegrasyon için hazırlanmıştır:

- **Grup 1 (Web + API Gateway):** Bizi nasıl çağıracağınız → [Bölüm 2](#2-endpointler), [Bölüm 4](#4-gateway-için-notlar-grup-1)
- **Grup 3 (Club / Event Service):** Sizden ne beklediğimiz → [Bölüm 5](#5-event-serviceten-beklediklerimiz-grup-3)
- **Entegrasyon reposu (`campushub-integration`):** Compose tanımı → [Bölüm 6](#6-entegrasyon-compose-tanımı)

Makinece okunabilir sözleşme (OpenAPI 3):
- Servis çalışırken: Swagger UI `http://localhost:8004/swagger`, JSON `http://localhost:8004/swagger/v1/swagger.json`
- Servisi çalıştırmadan: [Actions](https://github.com/emirruzunn/Registration_Service/actions/workflows/ci.yml) → main'deki son başarılı çalışma → **openapi** artifact'ı. CI her çalışmada bunu koddan yeniden üretir, yani her zaman günceldir. Entegrasyon reposundaki `api-contracts/` klasörüne bu dosya konabilir.

---

## 1. Servis bilgileri

| | |
|---|---|
| Dil / framework | C# / ASP.NET Core 8, EF Core + SQLite |
| Port | `8004` |
| Container adı | `registration-container` |
| Entegrasyondaki adres | `http://registration-container:8004` |
| Sağlık kontrolü | `GET /health` |
| Kimlik doğrulama | Yok. Servis `userId`'yi olduğu gibi kabul eder (bkz. [Bölüm 4](#4-gateway-için-notlar-grup-1)) |
| Bağımlılık | Club / Event Service: `GET /events/{id}` |

## 2. Endpointler

Bütün gövdeler JSON, alan adları **camelCase**.

### 2.1 `POST /registrations` – etkinliğe kayıt

İstek:
```json
{ "userId": 42, "eventId": 31 }
```

Akış: Event Service'e `GET /events/31` gönderilir → etkinlik yoksa 404 → kullanıcı zaten kayıtlıysa 409 → kapasite doluysa 409 → kayıt kendi veritabanımıza yazılır.

Başarılı cevap: **201 Created**, `Location: /registrations/912`
```json
{ "registrationId": 912, "userId": 42, "eventId": 31, "status": "REGISTERED" }
```

| Durum | `error` | Ne zaman |
|---|---|---|
| 400 | `INVALID_INPUT` | `userId` / `eventId` pozitif tam sayı değil ya da JSON bozuk |
| 404 | `EVENT_NOT_FOUND` | Event Service etkinliği bulamadı |
| 409 | `ALREADY_REGISTERED` | Kullanıcı bu etkinliğe zaten kayıtlı |
| 409 | `EVENT_FULL` | Etkinliğin kapasitesi dolu |
| 503 | `EVENT_SERVICE_UNAVAILABLE` | Event Service kapalı ya da zaman aşımı (3 deneme sonra) |
| 503 | `EVENT_SERVICE_ERROR` | Event Service 404 dışında bir hata kodu döndü |
| 503 | `EVENT_SERVICE_INVALID_RESPONSE` | Event Service 200 döndü ama gövde geçerli JSON değil |

İptal edilmiş bir kayıt için tekrar `POST` yapılırsa **aynı `registrationId`** yeniden `REGISTERED` olur (yeni kayıt açılmaz).

### 2.2 `GET /registrations/{id}` – tek kayıt

200 → yukarıdaki kayıt nesnesi. Kayıt yoksa **404** `REGISTRATION_NOT_FOUND`.

### 2.3 `GET /registrations` – listeleme

| Sorgu | Döner |
|---|---|
| `?userId=42` | Kullanıcının bütün kayıtları (iptal edilenler dahil) |
| `?eventId=31` | Etkinliğin bütün kayıtları (iptal edilenler dahil) |
| `?userId=42&eventId=31` | Bu kullanıcının bu etkinlikteki kaydı: `[]` ya da tek elemanlı dizi |
| (parametresiz) | Bütün kayıtlar, en yeni önce |

Her zaman **200** ve dizi döner:
```json
[ { "registrationId": 912, "userId": 42, "eventId": 31, "status": "REGISTERED" } ]
```

### 2.4 `DELETE /registrations/{id}` – kaydı iptal et

200 → `status` alanı `"CANCELLED"` olan kayıt nesnesi. Kayıt yoksa **404** `REGISTRATION_NOT_FOUND`, zaten iptalse **409** `ALREADY_CANCELLED`.

### 2.5 `GET /health`

```json
{ "service": "registration-service", "status": "UP", "version": "1.0.0" }
```

## 3. Ortak hata formatı

Bütün hatalar kapsamdaki ortak formatta döner:
```json
{ "error": "EVENT_NOT_FOUND", "message": "Event 31 does not exist." }
```
Beklenmeyen bir hata olursa servis çökmez, **500** `INTERNAL_ERROR` döner.

## 4. Gateway için notlar (Grup 1)

- **`userId`'yi kullanıcıdan değil, token'dan alın.** Registration Service kimlik doğrulama yapmıyor; isteği hangi `userId` ile gönderirseniz o kullanıcı adına kayıt açılır. Gateway, Auth Service'in verdiği JWT'deki `userId`'yi koyarak bize iletmeli.
- **Ekranlara göre hangi çağrı:**

  | Ekran (kapsam 7.2) | Çağrı |
  |---|---|
  | Ana sayfa – kayıtlı olduğum etkinlikler | `GET /registrations?userId={ben}` → `status == "REGISTERED"` olanlar. Etkinlik adları için Event Service'e sorun. |
  | Etkinlik ekranı – kayıt durumu | `GET /registrations?userId={ben}&eventId={id}` → boş dizi: kayıtlı değil; `status` alanı: `REGISTERED` / `CANCELLED` |
  | Etkinlik ekranı – dolu yer sayısı | `GET /registrations?eventId={id}` → `status == "REGISTERED"` olanların sayısı |
  | Kayıt ol butonu | `POST /registrations` |
  | Kaydı iptal et | `DELETE /registrations/{registrationId}` |

- **Durum kodlarını kullanıcıya olduğu gibi yansıtabilirsiniz.** `message` alanı İngilizce; kullanıcıya Türkçe göstermek için `error` koduna göre kendi metninizi seçin (örn. `EVENT_FULL` → "Etkinlik dolu").
- **Zaman aşımı:** Event Service hiç cevap vermezse servisimiz her denemede 5 sn bekler ve 3 kez dener. En kötü durumda cevabımız (503) **yaklaşık 15 sn** sürer. Gateway'deki timeout bundan kısaysa kullanıcı bizim 503 mesajımız yerine gateway'in timeout hatasını görür.
- **CORS ayarı yok.** Tarayıcı bizi doğrudan değil, Gateway üzerinden çağırmalı.

## 5. Event Service'ten beklediklerimiz (Grup 3)

Registration Service sizin veritabanınıza erişmez, sadece şu endpoint'i çağırır:

```
GET {EVENT_SERVICE_URL}/events/{id}
```

| Sizin cevabınız | Bizim davranışımız |
|---|---|
| **200** ve JSON gövde | Etkinlik var kabul edilir |
| **404** | `404 EVENT_NOT_FOUND` döneriz |
| **5xx**, 408, 429 | 100 ms / 200 ms arayla toplam 3 kez deneriz, yine olmazsa `503 EVENT_SERVICE_ERROR` |
| Bağlantı hatası / 5 sn içinde cevap yok | 3 kez deneriz, yine olmazsa `503 EVENT_SERVICE_UNAVAILABLE` |
| 200 ama gövde geçerli JSON değil | `503 EVENT_SERVICE_INVALID_RESPONSE` |

200 cevabındaki alanlar (kapsamdaki `GET /events/31` örneğiyle aynı):

| Alan | Tip | Zorunlu mu? | Kullanımı |
|---|---|---|---|
| `id` | int | evet | — |
| `title` | string | hayır | — |
| `capacity` | int | hayır | Aktif kayıt sayısı `capacity`'ye ulaşınca `EVENT_FULL`. Alan **yoksa ya da `null` ise kapasite kontrolü yapılmaz**. |

Diğer alanlar (`clubId`, `description`, `startTime` …) yok sayılır. Alan adları büyük/küçük harfe duyarsızdır ama **camelCase** öneriyoruz.

**Grup 3'ten teyit beklediğimiz konular:**
1. Entegrasyonda servisinizin adresi: varsayılan olarak `http://event-container:8002` kullanıyoruz.
2. Olmayan etkinlik için gerçekten **404** dönüyor musunuz? (200 + boş gövde dönerseniz bunu hata sayarız.)
3. `capacity` alanının adı ve tipi kapsamdaki örnekteki gibi mi?

## 6. Entegrasyon compose tanımı

`campushub-integration/docker-compose.yml` içine eklenecek kısım:

```yaml
services:
  registration-service:
    # Repo klonlanmadan doğrudan GitHub'dan derlenebilir:
    build: https://github.com/emirruzunn/Registration_Service.git#main
    container_name: registration-container
    ports:
      - "8004:8004"
    environment:
      PORT: "8004"
      DB_PATH: /app/data/registrations.db
      EVENT_SERVICE_URL: http://event-container:8002   # Grup 3'ün container adı/portu
    volumes:
      - registration-data:/app/data
    depends_on:
      - event-service          # Grup 3'ün compose'daki servis adı
    restart: unless-stopped

volumes:
  registration-data:
```

- İmaj kendi `HEALTHCHECK`'ini içerir (`docker ps` → `healthy`).
- Veritabanı (SQLite) `registration-data` volume'unda tutulur; container silinse de kayıtlar kalır.
- Event Service henüz hazır değilse bu repodaki `docker-compose.yml` sahte bir Event Service ile birlikte çalışır (bkz. README).

## 7. Ortam değişkenleri

| Değişken | Açıklama | Varsayılan (Docker imajı) |
|---|---|---|
| `PORT` | Servis portu | `8004` |
| `DB_PATH` | SQLite dosyası | `/app/data/registrations.db` |
| `EVENT_SERVICE_URL` | Event Service adresi | `http://event-container:8002` |
