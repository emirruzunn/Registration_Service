using Microsoft.AspNetCore.Mvc;
using RegistrationService.Dtos;

namespace RegistrationService.Controllers;

[ApiController]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    /// Entegrasyonda servislerin durumunu kontrol etmek icin (kapsam madde 24)
    [HttpGet("/health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get() =>
        Ok(new HealthResponse("registration-service", "UP", "1.0.0"));
}
