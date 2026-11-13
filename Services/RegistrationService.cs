using RegistrationService.Dtos;
using RegistrationService.Exceptions;
using RegistrationService.Models;
using RegistrationService.Repositories;

namespace RegistrationService.Services;

public class RegistrationAppService : IRegistrationService
{
    private readonly IRegistrationRepository _repo;
    private readonly IEventClient _events;

    public RegistrationAppService(IRegistrationRepository repo, IEventClient events)
    { _repo = repo; _events = events; }

    public async Task<RegistrationResponse> RegisterAsync(CreateRegistrationRequest req, CancellationToken ct = default)
    {
        // 1) Etkinlik var mi? (baska servisin DB'sine degil, API'sine soruyoruz)
        var ev = await _events.GetEventAsync(req.EventId, ct)
            ?? throw new NotFoundException("EVENT_NOT_FOUND", $"Event {req.EventId} does not exist.");

        // 2) Mevcut kayit kontrolu
        var existing = await _repo.GetByUserAndEventAsync(req.UserId, req.EventId);
        if (existing is { Status: RegistrationStatus.REGISTERED })
            throw new ConflictException("ALREADY_REGISTERED",
                $"User {req.UserId} is already registered for event {req.EventId}.");

        // 3) Kapasite kontrolu
        var active = await _repo.CountActiveByEventAsync(req.EventId);
        if (active >= ev.Capacity)
            throw new ConflictException("EVENT_FULL", $"Event {req.EventId} is full.");

        // 4) Iptal edilmis kaydi yeniden aktif et, yoksa yeni olustur
        if (existing is not null)
        {
            existing.Status = RegistrationStatus.REGISTERED;
            existing.CancelledAt = null;
            await _repo.UpdateAsync(existing);
            return RegistrationResponse.From(existing);
        }

        var created = await _repo.AddAsync(new Registration { UserId = req.UserId, EventId = req.EventId });
        return RegistrationResponse.From(created);
    }

    public async Task<RegistrationResponse> GetAsync(int id)
    {
        var r = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("REGISTRATION_NOT_FOUND", $"Registration {id} does not exist.");
        return RegistrationResponse.From(r);
    }

    public async Task<List<RegistrationResponse>> GetAllAsync() =>
        (await _repo.GetAllAsync()).Select(RegistrationResponse.From).ToList();

    public async Task<List<RegistrationResponse>> GetByUserAsync(int userId) =>
        (await _repo.GetByUserAsync(userId)).Select(RegistrationResponse.From).ToList();

    public async Task<List<RegistrationResponse>> GetByEventAsync(int eventId) =>
        (await _repo.GetByEventAsync(eventId)).Select(RegistrationResponse.From).ToList();

    public async Task<RegistrationResponse> CancelAsync(int id)
    {
        var r = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException("REGISTRATION_NOT_FOUND", $"Registration {id} does not exist.");
        if (r.Status == RegistrationStatus.CANCELLED)
            throw new ConflictException("ALREADY_CANCELLED", $"Registration {id} is already cancelled.");
        r.Status = RegistrationStatus.CANCELLED;
        r.CancelledAt = DateTime.UtcNow;
        await _repo.UpdateAsync(r);
        return RegistrationResponse.From(r);
    }
}
