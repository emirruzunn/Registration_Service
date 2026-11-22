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

// GET /health cevabi: { "service": "registration-service", "status": "UP", "version": "1.0.0" }
public record HealthResponse(string Service, string Status, string Version);

// Event Service'ten donen veri (GET /events/{id}).
// Capacity null ise (Event Service gondermediyse) kapasite kontrolu yapilmaz.
public record EventInfo(int Id, string? Title, int? Capacity);
