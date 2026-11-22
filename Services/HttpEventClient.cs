using System.Net;
using System.Text.Json;
using RegistrationService.Dtos;
using RegistrationService.Exceptions;

namespace RegistrationService.Services;

// TODO (Berat): retry / daha detayli hata yonetimi eklenebilir.
public class HttpEventClient : IEventClient
{
    private readonly HttpClient _http;
    public HttpEventClient(HttpClient http) => _http = http;

    public async Task<EventInfo?> GetEventAsync(int eventId, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync($"/events/{eventId}", ct);
            if (res.StatusCode == HttpStatusCode.NotFound) return null;
            if (!res.IsSuccessStatusCode)
                throw new ServiceUnavailableException("EVENT_SERVICE_ERROR",
                    $"Event Service returned {(int)res.StatusCode}.");
            return await res.Content.ReadFromJsonAsync<EventInfo>(cancellationToken: ct)
                ?? throw new ServiceUnavailableException("EVENT_SERVICE_INVALID_RESPONSE",
                    "Event Service returned an empty response.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ServiceUnavailableException("EVENT_SERVICE_UNAVAILABLE",
                "Event Service is unreachable or timed out.");
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Event Service 200 dondu ama govde beklenen JSON degil: 500 ile cokmek yerine 503
            throw new ServiceUnavailableException("EVENT_SERVICE_INVALID_RESPONSE",
                "Event Service returned an invalid response.");
        }
    }
}
