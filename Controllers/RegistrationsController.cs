using Microsoft.AspNetCore.Mvc;
using RegistrationService.Dtos;
using RegistrationService.Exceptions;
using RegistrationService.Services;

namespace RegistrationService.Controllers;

[ApiController]
[Route("registrations")]
public class RegistrationsController : ControllerBase
{
    private readonly IRegistrationService _service;
    public RegistrationsController(IRegistrationService service) => _service = service;

    /// POST /registrations -> 201 / 400 / 404 / 409 / 503
    [HttpPost]
    [ProducesResponseType(typeof(RegistrationResponse), 201)]
    public async Task<IActionResult> Create([FromBody] CreateRegistrationRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            throw new BadRequestException("INVALID_INPUT", "userId and eventId must be positive integers.");
        var result = await _service.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.RegistrationId }, result);
    }

    /// GET /registrations/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RegistrationResponse>> GetById(int id) =>
        Ok(await _service.GetAsync(id));

    /// GET /registrations?userId=42  veya  /registrations?eventId=31
    [HttpGet]
    public async Task<ActionResult<List<RegistrationResponse>>> List([FromQuery] int? userId, [FromQuery] int? eventId)
    {
        if (userId is not null) return Ok(await _service.GetByUserAsync(userId.Value));
        if (eventId is not null) return Ok(await _service.GetByEventAsync(eventId.Value));
        return Ok(await _service.GetAllAsync());
    }

    /// DELETE /registrations/{id}  (kaydi iptal eder)
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<RegistrationResponse>> Cancel(int id) =>
        Ok(await _service.CancelAsync(id));
}
