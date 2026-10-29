namespace ArdaNova.Application.Common.Results;

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public ResultType Type { get; }

    /// <summary>
    /// The machine discriminator of the upstream that produced this failure, when one survived -
    /// the AZOA node's envelope <c>code</c>, or one of <see cref="UpstreamFailureCodes"/>. Null on
    /// success and whenever no upstream named the failure. See <c>Common/AGENTS.md</c> §Failure codes.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// The upstream's own id for the record a failure is about (e.g. the intent INTENT_NEEDS_REVIEW
    /// holds), when it named one. Never a success value. See <c>Common/AGENTS.md</c> §Failure codes.
    /// </summary>
    public string? Reference { get; }

    private Result(bool isSuccess, T? value, string? error, ResultType type, string? code = null, string? reference = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Type = type;
        Code = code;
        Reference = reference;
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

    /// <summary>This failure, labelled with an upstream machine code. A success is returned unchanged.</summary>
    public Result<T> WithCode(string? code) =>
        IsSuccess ? this : new(false, default, Error, Type, code, Reference);

    /// <summary>This failure, naming the upstream record it is about. A success is returned unchanged.</summary>
    public Result<T> WithReference(string? reference) =>
        IsSuccess ? this : new(false, default, Error, Type, Code, string.IsNullOrWhiteSpace(reference) ? null : reference.Trim());

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
