namespace RegistrationService.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public AppException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}

public class NotFoundException(string code, string message) : AppException(404, code, message);
public class ConflictException(string code, string message) : AppException(409, code, message);
public class BadRequestException(string code, string message) : AppException(400, code, message);
public class ServiceUnavailableException(string code, string message) : AppException(503, code, message);
