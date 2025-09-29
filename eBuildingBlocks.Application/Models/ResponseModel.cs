using System.Net;

namespace eBuildingBlocks.Application.Features;

public record ResponseModel
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

    // Backing field for explicit override
    private string? _message;
    public string? Message
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_message))
                return _message;

            if (Success && Successes.Count > 0)
                return Successes[0];

            if (!Success && Errors.Count > 0)
                return Errors[0];

            return null;
        }
        init => _message = value;
    }

    public List<string> Successes { get; init; } = [];
    public List<string> Errors { get; init; } = [];
    public Dictionary<string, string[]> ValidationErrors { get; init; } = [];

    // ---------- Static factories ----------
    public static ResponseModel Ok(string? message = null)
        => new()
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = message,
            Successes = message is null ? [] : [message]
        };

    public static ResponseModel Fail(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
        => new()
        {
            Success = false,
            StatusCode = status,
            Message = message,
            Errors = string.IsNullOrWhiteSpace(message) ? [] : [message]
        };
    // Multi-error overload
    public static ResponseModel Fail(IEnumerable<string> messages, HttpStatusCode status = HttpStatusCode.BadRequest)
        => new()
        {
            Success = false,
            StatusCode = status,
            Message = messages?.FirstOrDefault(),
            Errors = messages?.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList() ?? []
        };

    public static ResponseModel ValidationFail(Dictionary<string, string[]> errors, string? message = null)
        => new()
        {
            Success = false,
            StatusCode = HttpStatusCode.BadRequest,
            Message = message,
            Errors = message is null ? [] : [message],
            ValidationErrors = errors ?? []
        };

    // ---------- Fluent adders ----------
    public ResponseModel WithSuccess(string message)
        => this with
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Successes = AddDistinct(Successes, message)
        };

    public ResponseModel WithError(string message, HttpStatusCode? status = null)
        => this with
        {
            Success = false,
            StatusCode = status ?? HttpStatusCode.BadRequest,
            Errors = AddDistinct(Errors, message)
        };

    protected static List<string> AddDistinct(List<string> list, string message)
    {
        if (!string.IsNullOrWhiteSpace(message) && !list.Contains(message))
            list = [.. list, message];
        return list;
    }
}

public record ResponseModel<T> : ResponseModel
{
    public T? Data { get; init; }

    // ---------- Static factories ----------
    public static ResponseModel<T> Ok(T data, string? message = null)
        => new()
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Data = data,
            Message = message,
            Successes = message is null ? [] : [message]
        };

    public static ResponseModel<T> Created(T data, string? message = null)
        => new()
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Data = data,
            Message = message,
            Successes = message is null ? [] : [message]
        };

    public static ResponseModel<T> Fail(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
        => new()
        {
            Success = false,
            StatusCode = status,
            Message = message,
            Errors = string.IsNullOrWhiteSpace(message) ? [] : [message]
        };

    // Multi-error overload
    public static ResponseModel<T> Fail(IEnumerable<string> messages, HttpStatusCode status = HttpStatusCode.BadRequest)
        => new()
        {
            Success = false,
            StatusCode = status,
            Message = messages?.FirstOrDefault(),
            Errors = messages?.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList() ?? []
        };

    public static ResponseModel<T> ValidationFail(Dictionary<string, string[]> errors, string? message = null)
        => new()
        {
            Success = false,
            StatusCode = HttpStatusCode.BadRequest,
            Message = message,
            Errors = message is null ? [] : [message],
            ValidationErrors = errors ?? []
        };

    // ---------- Fluent variants ----------
    public ResponseModel<T> WithData(T value)
        => this with { Data = value };

    public new ResponseModel<T> WithSuccess(string message)
        => (ResponseModel<T>)base.WithSuccess(message);

    public new ResponseModel<T> WithError(string message, HttpStatusCode? status = null)
        => (ResponseModel<T>)base.WithError(message, status);

    // ---------- Convenience conversion ----------
    public static implicit operator ResponseModel<T>(T value) => Ok(value);
}
