using System.ComponentModel.DataAnnotations;
using RegistrationService.Models;

namespace RegistrationService.Dtos;

public class CreateRegistrationRequest
{
    [Range(1, int.MaxValue)] public int UserId { get; set; }
    [Range(1, int.MaxValue)] public int EventId { get; set; }
}

public record RegistrationResponse(int RegistrationId, int UserId, int EventId, string Status)
{
    public static RegistrationResponse From(Registration r) =>
        new(r.Id, r.UserId, r.EventId, r.Status.ToString());
}

// Ortak hata formati: { "error": "...", "message": "..." }
public record ErrorResponse(string Error, string Message);

// Event Service'ten donen veri (GET /events/{id})
public record EventInfo(int Id, string Title, int Capacity);
