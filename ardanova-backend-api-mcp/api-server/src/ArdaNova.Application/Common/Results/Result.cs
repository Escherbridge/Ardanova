namespace ArdaNova.Application.Common.Results;

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public ResultType Type { get; }

    private Result(bool isSuccess, T? value, string? error, ResultType type)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Type = type;
    }

    public static Result<T> Success(T value) =>
        new(true, value, null, ResultType.Success);

    public static Result<T> Failure(string error) =>
        new(false, default, error, ResultType.Failure);

    public static Result<T> NotFound(string error) =>
        new(false, default, error, ResultType.NotFound);

    public static Result<T> ValidationError(string error) =>
        new(false, default, error, ResultType.ValidationError);

    public static Result<T> Unauthorized(string error) =>
        new(false, default, error, ResultType.Unauthorized);

    public static Result<T> Forbidden(string error) =>
        new(false, default, error, ResultType.Forbidden);

    public static Result<T> Conflict(string error) =>
        new(false, default, error, ResultType.Conflict);

    public static Result<T> BadRequest(string error) =>
        new(false, default, error, ResultType.BadRequest);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

public enum ResultType
{
    Success,
    Failure,
    NotFound,
    ValidationError,
    Unauthorized,
    Forbidden,
    Conflict,
    BadRequest
}
