using Microsoft.EntityFrameworkCore;
using RegistrationService.Data;
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

    public Task<List<Registration>> GetByUserAsync(int userId) =>
        _db.Registrations.Where(r => r.UserId == userId).OrderBy(r => r.Id).ToListAsync();

    public Task<List<Registration>> GetByEventAsync(int eventId) =>
        _db.Registrations.Where(r => r.EventId == eventId).OrderBy(r => r.Id).ToListAsync();

    public Task<int> CountActiveByEventAsync(int eventId) =>
        _db.Registrations.CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.REGISTERED);

    public async Task<Registration> AddAsync(Registration registration)
    {
        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();
        return registration;
    }

    public async Task UpdateAsync(Registration registration)
    {
        _db.Registrations.Update(registration);
        await _db.SaveChangesAsync();
    }
}
