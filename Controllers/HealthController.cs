using Microsoft.AspNetCore.Mvc;

namespace RegistrationService.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() =>
        Ok(new { service = "registration-service", status = "UP", version = "1.0.0" });
}
