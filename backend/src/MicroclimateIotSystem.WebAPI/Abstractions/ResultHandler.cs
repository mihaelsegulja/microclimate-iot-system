using MicroclimateIotSystem.Application.Models;
using Microsoft.AspNetCore.Http;

namespace MicroclimateIotSystem.WebAPI.Abstractions;

public static class ResultHandler
{
    public static IResult Handle<T>(StandardResponse<T> response)
    {
        return response.Status switch
        {
            ResultStatus.Ok => TypedResults.Ok(response),
            ResultStatus.Created => TypedResults.Created(string.Empty, response),
            ResultStatus.NotFound => TypedResults.NotFound(response),
            ResultStatus.Conflict => TypedResults.Conflict(response),
            ResultStatus.Unauthorized => TypedResults.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            ResultStatus.Forbidden => TypedResults.Json(response, statusCode: StatusCodes.Status403Forbidden),
            _ => TypedResults.Json(response, statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
