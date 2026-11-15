using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RegistrationService.Data;

namespace RegistrationService.Tests.Fakes;

/// Her test icin bellekte (in-memory) ayri bir SQLite veritabani.
/// Baglanti acik kaldigi surece veritabani yasar; Dispose ile silinir.
public sealed class TestDb : IDisposable
{
    public SqliteConnection Connection { get; }

    public TestDb()
    {
        Connection = new SqliteConnection("Data Source=:memory:");
        Connection.Open();
        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    public RegistrationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RegistrationDbContext>().UseSqlite(Connection).Options);

    public void Dispose() => Connection.Dispose();
}
