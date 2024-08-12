using Microsoft.AspNetCore.Diagnostics;

namespace TraefikForwardAuth.Helpers;
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public ValueTask<bool> TryHandleAsync(HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var exceptionMessage = exception.Message;
        logger.LogError(exception,
            "Error Message: {exceptionMessage}", exceptionMessage);

        return ValueTask.FromResult(true);
    }
}