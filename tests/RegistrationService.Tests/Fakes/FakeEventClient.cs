using RegistrationService.Dtos;
using RegistrationService.Exceptions;
using RegistrationService.Services;

namespace RegistrationService.Tests.Fakes;

/// Event Service'i taklit eder. Testte hangi etkinliklerin "var" oldugunu
/// ve servisin "kapali" olup olmadigini biz belirleriz.
public class FakeEventClient : IEventClient
{
    private readonly Dictionary<int, EventInfo> _events = new();

    /// true ise Event Service'e ulasilamiyormus gibi davranir (503).
    public bool IsDown { get; set; }

    public FakeEventClient WithEvent(int id, int capacity, string title = "Test Etkinligi")
    {
        _events[id] = new EventInfo(id, title, capacity);
        return this;
    }

    public Task<EventInfo?> GetEventAsync(int eventId, CancellationToken ct = default)
    {
        if (IsDown)
            throw new ServiceUnavailableException("EVENT_SERVICE_UNAVAILABLE",
                "Event Service is unreachable or timed out.");
        return Task.FromResult(_events.GetValueOrDefault(eventId));
    }
}
