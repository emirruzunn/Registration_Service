using RegistrationService.Models;

namespace RegistrationService.Repositories;

public interface IRegistrationRepository
{
    Task<Registration?> GetByIdAsync(int id);
    Task<Registration?> GetByUserAndEventAsync(int userId, int eventId);
    Task<List<Registration>> GetByUserAsync(int userId);
    Task<List<Registration>> GetByEventAsync(int eventId);
    Task<int> CountActiveByEventAsync(int eventId);
    Task<Registration> AddAsync(Registration registration);
    Task UpdateAsync(Registration registration);
}
