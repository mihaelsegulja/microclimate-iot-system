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
    
    public static StandardResponse<T> SuccessOk(T data, string? message = null) 
        => Create(ResultStatus.Ok, data, message);

    public static StandardResponse<T> SuccessCreated(T data, string? message = null) 
        => Create(ResultStatus.Created, data, message);

    public static StandardResponse<T> NotFound(string message = "Resource not found.") 
        => Create(ResultStatus.NotFound, default, message);

    public static StandardResponse<T> Unauthorized(string message = "You are not authorized.") 
        => Create(ResultStatus.Unauthorized, default, message);

    public static StandardResponse<T> Failure(ResultStatus status, string message, IEnumerable<string>? errors = null) 
        => Create(status, default, message, errors);
}

public class PaginatedResponse<T> : StandardResponse<IEnumerable<T>>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public PaginatedResponse() 
    {
        Data = [];
    }

    public PaginatedResponse(ResultStatus status, IEnumerable<T> data, int page, int pageSize, int totalCount, string? message = null, IEnumerable<string>? errors = null)
    {
        Status = status;
        Success = status == ResultStatus.Ok || status == ResultStatus.Created;
        Data = data ?? [];
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        Message = message;
        Errors = errors;
    }

    public static PaginatedResponse<T> Create(
        ResultStatus status, IEnumerable<T> data, int page, int pageSize, int totalCount,
        string? message = null, IEnumerable<string>? errors = null)
    {
        return new PaginatedResponse<T>(status, data, page, pageSize, totalCount, message, errors);
    }

    public static PaginatedResponse<T> SuccessOk(IEnumerable<T> data, int page, int pageSize, int totalCount, string? message = null)
    {
        return Create(ResultStatus.Ok, data, page, pageSize, totalCount, message);
    }
}
