using RegistrationService.Dtos;

namespace RegistrationService.Services;

public interface IRegistrationService
{
    Task<RegistrationResponse> RegisterAsync(CreateRegistrationRequest request, CancellationToken ct = default);
    Task<RegistrationResponse> GetAsync(int id);
    Task<List<RegistrationResponse>> GetAllAsync();
    Task<List<RegistrationResponse>> GetByUserAsync(int userId);
    Task<List<RegistrationResponse>> GetByEventAsync(int eventId);
    Task<RegistrationResponse> CancelAsync(int id);
}
