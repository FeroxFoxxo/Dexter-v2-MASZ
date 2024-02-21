using Bot.Enums;

namespace Bot.Abstractions;

public abstract class ApiException(string message, ApiError error) : Exception(message)
{
    public ApiError Error { get; set; } = error;

    public Exception WithError(ApiError error)
    {
        Error = error;
        return this;
    }
}
