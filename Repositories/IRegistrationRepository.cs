using RegistrationService.Models;

namespace RegistrationService.Repositories;

public interface IRegistrationRepository
{
    Task<Registration?> GetByIdAsync(int id);
    Task<Registration?> GetByUserAndEventAsync(int userId, int eventId);
    Task<List<Registration>> GetAllAsync();
    Task<List<Registration>> GetByUserAsync(int userId);
    Task<List<Registration>> GetByEventAsync(int eventId);
    Task<int> CountActiveByEventAsync(int eventId);
    Task<Registration> AddAsync(Registration registration);
    Task UpdateAsync(Registration registration);

    /// work'u tek bir veritabani transaction'i icinde calistirir. SQLite'ta bu
    /// "BEGIN IMMEDIATE" demektir: ayni anda gelen kayit istekleri kontrol + ekleme
    /// adimlarini sirayla yapar, boylece kapasite asilmaz ve cift kayit olusmaz.
    Task<T> InTransactionAsync<T>(Func<Task<T>> work);
}
