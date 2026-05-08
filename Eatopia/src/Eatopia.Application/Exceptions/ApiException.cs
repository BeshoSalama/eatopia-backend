namespace Eatopia.Application.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; set; }
    public string Code { get; set; }

    public ApiException(string message, int statusCode = 400, string code = "BAD_REQUEST")
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}
