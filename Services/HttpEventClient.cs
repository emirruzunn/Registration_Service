using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RegistrationService.Dtos;
using RegistrationService.Exceptions;

namespace RegistrationService.Services;

public class HttpEventClient : IEventClient
{
    private readonly HttpClient _http;
    private const int MaxRetries = 3;

    public HttpEventClient(HttpClient http) => _http = http;

    public async Task<EventInfo?> GetEventAsync(int eventId, CancellationToken ct = default)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var res = await _http.GetAsync($"/events/{eventId}", ct);

                if (res.StatusCode == HttpStatusCode.NotFound)
                    return null;

                if (!res.IsSuccessStatusCode)
                {
                    // Sadece 5xx sunucu hatalarinda veya 408/429 durumlarinda yeniden dene
                    bool isTransientStatus = (int)res.StatusCode >= 500
                        || res.StatusCode == HttpStatusCode.RequestTimeout
                        || res.StatusCode == HttpStatusCode.TooManyRequests;

                    if (isTransientStatus && attempt < MaxRetries)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), ct);
                        continue;
                    }

                    throw new ServiceUnavailableException("EVENT_SERVICE_ERROR",
                        $"Event Service returned {(int)res.StatusCode}.");
                }

                return await res.Content.ReadFromJsonAsync<EventInfo>(cancellationToken: ct)
                    ?? throw new ServiceUnavailableException("EVENT_SERVICE_INVALID_RESPONSE",
                        "Event Service returned an empty response.");
            }
            catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
            {
                if (attempt == MaxRetries)
                {
                    throw new ServiceUnavailableException("EVENT_SERVICE_UNAVAILABLE",
                        "Event Service is unreachable or timed out after retries.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), ct);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                // Event Service 200 dondu ama govde beklenen JSON degil: retry yapmadan 503 don
                throw new ServiceUnavailableException("EVENT_SERVICE_INVALID_RESPONSE",
                    "Event Service returned an invalid response.");
            }
        }

        throw new ServiceUnavailableException("EVENT_SERVICE_UNAVAILABLE",
            "Event Service is unavailable.");
    }
}