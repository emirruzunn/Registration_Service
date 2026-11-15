using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RegistrationService.Data;
using RegistrationService.Dtos;
using RegistrationService.Services;
using RegistrationService.Tests.Fakes;

namespace RegistrationService.Tests;

/// Servisi bellekte ayaga kaldirip gercek HTTP istekleriyle test eder
/// (controller + middleware + hata formati + status kodlari).
public class ApiTests : IDisposable
{
    private readonly SqliteConnection _conn = new("Data Source=:memory:");
    private readonly FakeEventClient _events = new FakeEventClient()
        .WithEvent(31, capacity: 100);
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiTests()
    {
        _conn.Open();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.ConfigureServices(services =>
            {
                // Dosya yerine bellekteki SQLite
                services.RemoveAll<DbContextOptions<RegistrationDbContext>>();
                services.AddDbContext<RegistrationDbContext>(o => o.UseSqlite(_conn));
                // Gercek Event Service yerine sahtesi
                services.RemoveAll<IEventClient>();
                services.AddSingleton<IEventClient>(_events);
            });
        });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _conn.Dispose();
    }

    private Task<HttpResponseMessage> PostRegistration(int userId, int eventId) =>
        _client.PostAsJsonAsync("/registrations", new { userId, eventId });

    [Fact]
    public async Task Health_200Up()
    {
        var res = await _client.GetAsync("/health");
        var body = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("registration-service", body!["service"]);
        Assert.Equal("UP", body["status"]);
    }

    [Fact]
    public async Task Post_Gecerli_201VeLocation()
    {
        var res = await PostRegistration(42, 31);
        var body = await res.Content.ReadFromJsonAsync<RegistrationResponse>();

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.NotNull(res.Headers.Location);
        Assert.Equal("REGISTERED", body!.Status);
    }

    [Theory]
    [InlineData(0, 31)]
    [InlineData(42, -1)]
    public async Task Post_GecersizId_400InvalidInput(int userId, int eventId)
    {
        var res = await PostRegistration(userId, eventId);
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("INVALID_INPUT", err!.Error);
    }

    [Fact]
    public async Task Post_BozukJson_400()
    {
        var res = await _client.PostAsync("/registrations",
            new StringContent("bu json degil", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Post_OlmayanEtkinlik_404OrtakHataFormati()
    {
        var res = await PostRegistration(42, 999);
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("EVENT_NOT_FOUND", err!.Error);
        Assert.Equal("Event 999 does not exist.", err.Message);
    }

    [Fact]
    public async Task Post_IkiKez_409()
    {
        await PostRegistration(42, 31);
        var res = await PostRegistration(42, 31);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Post_EventServiceKapali_503()
    {
        _events.IsDown = true;

        var res = await PostRegistration(42, 31);
        var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
        Assert.Equal("EVENT_SERVICE_UNAVAILABLE", err!.Error);
    }

    [Fact]
    public async Task Get_OlmayanKayit_404()
    {
        var res = await _client.GetAsync("/registrations/12345");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Get_UserIdIleListeleme()
    {
        await PostRegistration(42, 31);

        var list = await _client.GetFromJsonAsync<List<RegistrationResponse>>("/registrations?userId=42");

        Assert.Single(list!);
    }

    [Fact]
    public async Task Delete_KaydiIptalEder()
    {
        var created = await (await PostRegistration(42, 31)).Content.ReadFromJsonAsync<RegistrationResponse>();

        var res = await _client.DeleteAsync($"/registrations/{created!.RegistrationId}");
        var body = await res.Content.ReadFromJsonAsync<RegistrationResponse>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("CANCELLED", body!.Status);
    }
}
