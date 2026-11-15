namespace RegistrationService.Models;

public enum RegistrationStatus { REGISTERED, CANCELLED }

public class Registration
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.REGISTERED;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
}
