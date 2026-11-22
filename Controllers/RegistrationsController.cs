using Microsoft.AspNetCore.Mvc;
using RegistrationService.Dtos;
using RegistrationService.Exceptions;
using RegistrationService.Services;

namespace RegistrationService.Controllers;

// ProducesResponseType'lar Swagger / docs/openapi.json'da hata cevaplarinin da gorunmesi icin.
[ApiController]
[Route("registrations")]
[Produces("application/json")]
public class RegistrationsController : ControllerBase
{
    private readonly IRegistrationService _service;
    public RegistrationsController(IRegistrationService service) => _service = service;

    /// POST /registrations -> 201 / 400 / 404 / 409 / 503
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create([FromBody] CreateRegistrationRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            throw new BadRequestException("INVALID_INPUT", "userId and eventId must be positive integers.");
        var result = await _service.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.RegistrationId }, result);
    }

    /// GET /registrations/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationResponse>> GetById(int id) =>
        Ok(await _service.GetAsync(id));

    /// GET /registrations?userId=42  veya  ?eventId=31  veya  ?userId=42&eventId=31
    [HttpGet]
    [ProducesResponseType(typeof(List<RegistrationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RegistrationResponse>>> List([FromQuery] int? userId, [FromQuery] int? eventId)
    {
        if (userId is not null && eventId is not null)
            return Ok(await _service.GetByUserAndEventAsync(userId.Value, eventId.Value));
        if (userId is not null) return Ok(await _service.GetByUserAsync(userId.Value));
        if (eventId is not null) return Ok(await _service.GetByEventAsync(eventId.Value));
        return Ok(await _service.GetAllAsync());
    }

    /// DELETE /registrations/{id}  (kaydi iptal eder)
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationResponse>> Cancel(int id) =>
        Ok(await _service.CancelAsync(id));
}
