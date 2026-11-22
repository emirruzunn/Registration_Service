using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RegistrationService.Data;
using RegistrationService.Dtos;
using RegistrationService.Exceptions;
using RegistrationService.Repositories;
using RegistrationService.Services;
using RegistrationService.Tests.Fakes;

namespace RegistrationService.Tests;

/// Ayni anda gelen kayit isteklerinde kapasitenin asilmadigini ve
/// cift kayit olusmadigini test eder. Gercek servisteki gibi her istek kendi
/// DbContext'i ve kendi baglantisiyla, ayni SQLite dosyasina yazar.
public class EsZamanliKayitTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"registrations-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<RegistrationDbContext> _options;

    public EsZamanliKayitTests()
    {
        _options = new DbContextOptionsBuilder<RegistrationDbContext>()
            .UseSqlite($"Data Source={_path}")
            .Options;
        using var ctx = new RegistrationDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_path); } catch (IOException) { /* gecici dosya, onemli degil */ }
    }

    // Her cagri ayri bir thread'de, ayri bir DbContext ile (ayri HTTP istekleri gibi)
    private Task<string> RegisterInParallel(FakeEventClient events, int userId, int eventId) =>
        Task.Run(async () =>
        {
            await using var ctx = new RegistrationDbContext(_options);
            var service = new RegistrationAppService(new RegistrationRepository(ctx), events);
            try
            {
                var r = await service.RegisterAsync(new CreateRegistrationRequest { UserId = userId, EventId = eventId });
                return r.Status;
            }
            catch (ConflictException ex)
            {
                return ex.Code;
            }
        });

    [Fact]
    public async Task KapasiteBirken_OnKisiAyniAnda_SadeceBiriKayitOlur()
    {
        var events = new FakeEventClient().WithEvent(7, capacity: 1);

        var results = await Task.WhenAll(
            Enumerable.Range(1, 10).Select(userId => RegisterInParallel(events, userId, 7)));

        Assert.Single(results, s => s == "REGISTERED");
        Assert.Equal(9, results.Count(s => s == "EVENT_FULL"));

        await using var ctx = new RegistrationDbContext(_options);
        Assert.Equal(1, await ctx.Registrations.CountAsync());
    }

    [Fact]
    public async Task AyniKullaniciAyniAnda_BesKezIstek_TekKayitOlusur()
    {
        var events = new FakeEventClient().WithEvent(31, capacity: 100);

        var results = await Task.WhenAll(
            Enumerable.Range(1, 5).Select(_ => RegisterInParallel(events, 42, 31)));

        Assert.Single(results, s => s == "REGISTERED");
        Assert.Equal(4, results.Count(s => s == "ALREADY_REGISTERED"));

        await using var ctx = new RegistrationDbContext(_options);
        Assert.Equal(1, await ctx.Registrations.CountAsync());
    }
}
