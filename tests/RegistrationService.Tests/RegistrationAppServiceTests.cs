using RegistrationService.Dtos;
using RegistrationService.Exceptions;
using RegistrationService.Repositories;
using RegistrationService.Services;
using RegistrationService.Tests.Fakes;

namespace RegistrationService.Tests;

/// Is mantigi testleri: RegistrationAppService + gercek repository (in-memory SQLite)
/// + sahte Event Service.
public class RegistrationAppServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeEventClient _events = new FakeEventClient()
        .WithEvent(31, capacity: 100)
        .WithEvent(7, capacity: 1);

    private RegistrationAppService CreateService() =>
        new(new RegistrationRepository(_db.CreateContext()), _events);

    private static CreateRegistrationRequest Req(int userId, int eventId) =>
        new() { UserId = userId, EventId = eventId };

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Register_EtkinlikVarsa_KayitOlusur()
    {
        var result = await CreateService().RegisterAsync(Req(42, 31));

        Assert.True(result.RegistrationId > 0);
        Assert.Equal(42, result.UserId);
        Assert.Equal(31, result.EventId);
        Assert.Equal("REGISTERED", result.Status);
    }

    [Fact]
    public async Task Register_EtkinlikYoksa_404EventNotFound()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().RegisterAsync(Req(42, 999)));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("EVENT_NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task Register_AyniEtkinligeIkinciKez_409AlreadyRegistered()
    {
        await CreateService().RegisterAsync(Req(42, 31));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => CreateService().RegisterAsync(Req(42, 31)));

        Assert.Equal("ALREADY_REGISTERED", ex.Code);
    }

    [Fact]
    public async Task Register_KapasiteDoluysa_409EventFull()
    {
        await CreateService().RegisterAsync(Req(1, 7)); // kapasite 1 -> doldu

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => CreateService().RegisterAsync(Req(2, 7)));

        Assert.Equal("EVENT_FULL", ex.Code);
    }

    [Fact]
    public async Task Register_EventServiceKapaliysa_503()
    {
        _events.IsDown = true;

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => CreateService().RegisterAsync(Req(42, 31)));

        Assert.Equal(503, ex.StatusCode);
    }

    [Fact]
    public async Task Cancel_KayitIptalEdilir()
    {
        var created = await CreateService().RegisterAsync(Req(42, 31));

        var cancelled = await CreateService().CancelAsync(created.RegistrationId);

        Assert.Equal("CANCELLED", cancelled.Status);
    }

    [Fact]
    public async Task Cancel_IkinciKez_409AlreadyCancelled()
    {
        var created = await CreateService().RegisterAsync(Req(42, 31));
        await CreateService().CancelAsync(created.RegistrationId);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => CreateService().CancelAsync(created.RegistrationId));

        Assert.Equal("ALREADY_CANCELLED", ex.Code);
    }

    [Fact]
    public async Task Cancel_OlmayanKayit_404()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().CancelAsync(12345));

        Assert.Equal("REGISTRATION_NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task Register_IptalSonrasiTekrarKayit_AyniKayitYenidenAktifOlur()
    {
        var first = await CreateService().RegisterAsync(Req(42, 31));
        await CreateService().CancelAsync(first.RegistrationId);

        var again = await CreateService().RegisterAsync(Req(42, 31));

        Assert.Equal(first.RegistrationId, again.RegistrationId);
        Assert.Equal("REGISTERED", again.Status);
    }

    [Fact]
    public async Task Kapasite_IptalEdilenKayitlariSaymaz()
    {
        var first = await CreateService().RegisterAsync(Req(1, 7));
        await CreateService().CancelAsync(first.RegistrationId); // yer acildi

        var second = await CreateService().RegisterAsync(Req(2, 7));

        Assert.Equal("REGISTERED", second.Status);
    }

    [Fact]
    public async Task GetByUser_SadeceOKullanicininKayitlariniDoner()
    {
        await CreateService().RegisterAsync(Req(42, 31));
        await CreateService().RegisterAsync(Req(43, 31));

        var list = await CreateService().GetByUserAsync(42);

        Assert.Single(list);
        Assert.Equal(42, list[0].UserId);
    }
}
