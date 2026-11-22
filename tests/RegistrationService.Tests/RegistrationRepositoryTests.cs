using RegistrationService.Exceptions;
using RegistrationService.Models;
using RegistrationService.Repositories;
using RegistrationService.Tests.Fakes;

namespace RegistrationService.Tests;

/// Repository katmani: veritabanindaki unique index ihlalinin 500 yerine
/// ortak formatta 409'a cevrildigini test eder.
public class RegistrationRepositoryTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AyniKullaniciAyniEtkinlik_IkinciEkleme_409AlreadyRegistered()
    {
        await new RegistrationRepository(_db.CreateContext())
            .AddAsync(new Registration { UserId = 42, EventId = 31 });

        // Servis kontrolunu atlayip dogrudan ekleme: iki istegin ayni anda gelmesi durumu
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            new RegistrationRepository(_db.CreateContext())
                .AddAsync(new Registration { UserId = 42, EventId = 31 }));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("ALREADY_REGISTERED", ex.Code);
    }

    [Fact]
    public async Task InTransaction_HataOlursa_GeriAlinir()
    {
        var repo = new RegistrationRepository(_db.CreateContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.InTransactionAsync<int>(async () =>
            {
                await repo.AddAsync(new Registration { UserId = 1, EventId = 1 });
                throw new InvalidOperationException("islem yarida kaldi");
            }));

        var all = await new RegistrationRepository(_db.CreateContext()).GetAllAsync();
        Assert.Empty(all);
    }
}
