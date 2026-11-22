using System.Net;
using System.Text;
using RegistrationService.Exceptions;
using RegistrationService.Services;

namespace RegistrationService.Tests;

/// Event Service'e giden HTTP cagrisinin hata durumlarini test eder
/// (gercek ag yok, sahte HttpMessageHandler kullanilir).
public class HttpEventClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(respond(request));
    }

    private static HttpEventClient Client(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("http://event-service:8002") });

    private static HttpResponseMessage Json(HttpStatusCode code, string body) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Status200_EtkinlikBilgisiDoner()
    {
        string? calledPath = null;
        var client = Client(req =>
        {
            calledPath = req.RequestUri!.AbsolutePath;
            return Json(HttpStatusCode.OK, """{ "id": 31, "title": "Python Atolyesi", "capacity": 100 }""");
        });

        var ev = await client.GetEventAsync(31);

        Assert.Equal("/events/31", calledPath);
        Assert.NotNull(ev);
        Assert.Equal(31, ev!.Id);
        Assert.Equal(100, ev.Capacity);
    }

    [Fact]
    public async Task Status404_NullDoner()
    {
        var client = Client(_ => Json(HttpStatusCode.NotFound, """{ "error": "EVENT_NOT_FOUND" }"""));

        Assert.Null(await client.GetEventAsync(999));
    }

    [Fact]
    public async Task Status500_503EventServiceError()
    {
        var client = Client(_ => Json(HttpStatusCode.InternalServerError, "{}"));

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => client.GetEventAsync(31));
        Assert.Equal("EVENT_SERVICE_ERROR", ex.Code);
    }

    [Fact]
    public async Task BaglantiHatasi_503EventServiceUnavailable()
    {
        var client = Client(_ => throw new HttpRequestException("Connection refused"));

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => client.GetEventAsync(31));
        Assert.Equal("EVENT_SERVICE_UNAVAILABLE", ex.Code);
    }

    [Fact]
    public async Task Timeout_503EventServiceUnavailable()
    {
        var client = Client(_ => throw new TaskCanceledException("timeout"));

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => client.GetEventAsync(31));
        Assert.Equal("EVENT_SERVICE_UNAVAILABLE", ex.Code);
    }

    [Fact]
    public async Task Status200AmaBozukJson_503InvalidResponse()
    {
        // Eskiden JsonException yakalanmiyordu ve servis 500 donuyordu
        var client = Client(_ => Json(HttpStatusCode.OK, "bu json degil"));

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => client.GetEventAsync(31));
        Assert.Equal("EVENT_SERVICE_INVALID_RESPONSE", ex.Code);
    }

    [Fact]
    public async Task Status200AmaBosGovde_503InvalidResponse()
    {
        var client = Client(_ => Json(HttpStatusCode.OK, "null"));

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => client.GetEventAsync(31));
        Assert.Equal("EVENT_SERVICE_INVALID_RESPONSE", ex.Code);
    }

    [Fact]
    public async Task CapacityAlaniYoksa_CapacityNullOlur()
    {
        var client = Client(_ => Json(HttpStatusCode.OK, """{ "id": 31, "title": "Python Atolyesi" }"""));

        var ev = await client.GetEventAsync(31);

        Assert.NotNull(ev);
        Assert.Null(ev!.Capacity);
    }
}
