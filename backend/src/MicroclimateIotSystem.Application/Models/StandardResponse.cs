namespace MicroclimateIotSystem.Application.Models;

public enum ResultStatus
{
    Ok,
    Created,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict,
    InternalError
}

public class StandardResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ResultStatus Status { get; set; }
    public string? Message { get; set; } = string.Empty;
    public IEnumerable<string>? Errors { get; set; }

    public static StandardResponse<T> Create(ResultStatus status, T? data = default, string? message = null, IEnumerable<string>? errors = null)
    {
        return new StandardResponse<T>
        {
            Status = status,
            Success = status == ResultStatus.Ok || status == ResultStatus.Created,
            Data = data,
            Message = message,
            Errors = errors
        };
    }
}

