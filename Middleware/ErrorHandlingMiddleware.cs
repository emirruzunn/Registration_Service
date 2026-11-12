using RegistrationService.Dtos;
using RegistrationService.Exceptions;

namespace RegistrationService.Middleware;

// Butun hatalari ortak formatta ({error, message}) dondurur; servis cokmez.
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _log;
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> log)
    { _next = next; _log = log; }

    public async Task Invoke(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (AppException ex)
        {
            ctx.Response.StatusCode = ex.StatusCode;
            await ctx.Response.WriteAsJsonAsync(new ErrorResponse(ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Beklenmeyen hata");
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsJsonAsync(new ErrorResponse("INTERNAL_ERROR", "Unexpected error."));
        }
    }
}
