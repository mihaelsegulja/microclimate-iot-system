using MicroclimateIotSystem.Application.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace MicroclimateIotSystem.WebAPI.Abstractions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unexpected error: {Message}", exception.Message);

        var response = StandardResponse<object>.Create(
            ResultStatus.InternalError,
            message: "Internal server error. Something went wrong.");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
