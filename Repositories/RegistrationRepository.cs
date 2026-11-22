using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RegistrationService.Data;
using RegistrationService.Exceptions;
using RegistrationService.Models;

namespace RegistrationService.Repositories;

public class RegistrationRepository : IRegistrationRepository
{
    private readonly RegistrationDbContext _db;
    public RegistrationRepository(RegistrationDbContext db) => _db = db;

    public Task<Registration?> GetByIdAsync(int id) =>
        _db.Registrations.FirstOrDefaultAsync(r => r.Id == id);

    public Task<Registration?> GetByUserAndEventAsync(int userId, int eventId) =>
        _db.Registrations.FirstOrDefaultAsync(r => r.UserId == userId && r.EventId == eventId);

    public Task<List<Registration>> GetAllAsync() =>
        _db.Registrations.OrderByDescending(r => r.Id).ToListAsync();

    public Task<List<Registration>> GetByUserAsync(int userId) =>
        _db.Registrations.Where(r => r.UserId == userId).OrderBy(r => r.Id).ToListAsync();

    public Task<List<Registration>> GetByEventAsync(int eventId) =>
        _db.Registrations.Where(r => r.EventId == eventId).OrderBy(r => r.Id).ToListAsync();

    public Task<int> CountActiveByEventAsync(int eventId) =>
        _db.Registrations.CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.REGISTERED);

    public async Task<Registration> AddAsync(Registration registration)
    {
        _db.Registrations.Add(registration);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintError })
        {
            // (UserId, EventId) unique index'i ihlal edildi: 500 yerine 409 dondur.
            throw new ConflictException("ALREADY_REGISTERED",
                $"User {registration.UserId} is already registered for event {registration.EventId}.");
        }
        return registration;
    }

    public async Task UpdateAsync(Registration registration)
    {
        _db.Registrations.Update(registration);
        await _db.SaveChangesAsync();
    }

    public async Task<T> InTransactionAsync<T>(Func<Task<T>> work)
    {
        // Microsoft.Data.Sqlite varsayilan olarak "BEGIN IMMEDIATE" ile baslatir;
        // ikinci istek ilki commit edene kadar bekler. Hata olursa Dispose rollback yapar.
        await using var tx = await _db.Database.BeginTransactionAsync();
        var result = await work();
        await tx.CommitAsync();
        return result;
    }

    // SQLITE_CONSTRAINT hata kodu
    private const int SqliteConstraintError = 19;
}
