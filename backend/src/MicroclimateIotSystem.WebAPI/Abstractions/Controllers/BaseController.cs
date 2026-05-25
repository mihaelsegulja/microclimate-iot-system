using MicroclimateIotSystem.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace MicroclimateIotSystem.WebAPI.Abstractions.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected ActionResult HandleResponse<T>(StandardResponse<T> response)
    {
        return response.Status switch
        {
            ResultStatus.Ok => Ok(response),
            ResultStatus.Created => Created(string.Empty, response),
            ResultStatus.NotFound => NotFound(response),
            ResultStatus.Conflict => Conflict(response),
            ResultStatus.Unauthorized => Unauthorized(response),
            _ => StatusCode(500, response)
        };
    }
}

