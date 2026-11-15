using RegistrationService.Dtos;

namespace RegistrationService.Services;

/// Event Service ile iletisim (Berat Yusuf Beyazit'in sorumlulugu).
/// Etkinlik yoksa null, servis ulasilamazsa ServiceUnavailableException firlatir.
public interface IEventClient
{
    Task<EventInfo?> GetEventAsync(int eventId, CancellationToken ct = default);
}
