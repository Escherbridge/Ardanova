using System.Collections;
using System.Text.Json;
using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace ArdaNova.API.Filters;

/// <summary>Rewrites every controller-authored 4xx/5xx body into the shared { error, statusCode, traceId } envelope. See Filters/AGENTS.md §Error envelope.</summary>
public sealed class ErrorEnvelopeResultFilter : IAlwaysRunResultFilter
{
    public const string NotFoundMessage = "The requested resource was not found.";
    public const string BadRequestMessage = "The request could not be processed.";
    public const string ConflictMessage = "The request conflicts with the current state of the resource.";

    public void OnResultExecuting(ResultExecutingContext context)
        => context.Result = Normalize(context.HttpContext, context.Result);

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    internal static IActionResult Normalize(HttpContext context, IActionResult result) => result switch
    {
        // 501/503 carry authored "not available" messages; every other 5xx may carry exception text.
        ObjectResult { StatusCode: int status and >= 500 and not (StatusCodes.Status501NotImplemented or StatusCodes.Status503ServiceUnavailable) }
            => new ObjectResult(ApiErrorEnvelope.Create(context, status, ExceptionHandlingMiddleware.ServerErrorMessage)) { StatusCode = status },
        ObjectResult { StatusCode: int status and >= 400 } objectResult
            => new ObjectResult(Envelope(context, status, objectResult.Value)) { StatusCode = status },
        StatusCodeResult { StatusCode: >= 400 } statusResult
            => new ObjectResult(ApiErrorEnvelope.Create(context, statusResult.StatusCode, DefaultMessage(statusResult.StatusCode)))
            {
                StatusCode = statusResult.StatusCode,
            },
        _ => result,
    };

    private static object Envelope(HttpContext context, int statusCode, object? body)
    {
        switch (body)
        {
            case null:
                return ApiErrorEnvelope.Create(context, statusCode, DefaultMessage(statusCode));
            case string message:
                return ApiErrorEnvelope.Create(context, statusCode, string.IsNullOrWhiteSpace(message) ? DefaultMessage(statusCode) : message);
            case ValidationProblemDetails validation:
                return new
                {
                    error = ApiErrorEnvelope.ValidationFailedMessage,
                    statusCode,
                    traceId = ApiErrorEnvelope.TraceId(context),
                    errors = validation.Errors,
                };
            case ProblemDetails problem:
                return ApiErrorEnvelope.Create(context, statusCode, problem.Detail ?? problem.Title ?? DefaultMessage(statusCode));
        }

        var fields = ReadFields(body);
        if (fields is null)
            return ApiErrorEnvelope.Create(context, statusCode, DefaultMessage(statusCode));

        if (!fields.TryGetValue("error", out var error) || error is not string { Length: > 0 })
            fields["error"] = fields.TryGetValue("message", out var message) && message is string { Length: > 0 }
                ? message
                : DefaultMessage(statusCode);
        fields["statusCode"] = statusCode;
        fields.TryAdd("traceId", ApiErrorEnvelope.TraceId(context));
        return fields;
    }

    /// <summary>Reads an anonymous object or string-keyed dictionary into camelCase fields; null for any other shape.</summary>
    private static Dictionary<string, object?>? ReadFields(object body)
    {
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (body is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not string key)
                    return null;
                fields[JsonNamingPolicy.CamelCase.ConvertName(key)] = entry.Value;
            }
            return fields;
        }

        var type = body.GetType();
        if (type.IsPrimitive || body is IEnumerable)
            return null;

        foreach (var property in type.GetProperties())
        {
            if (property.GetIndexParameters().Length == 0)
                fields[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = property.GetValue(body);
        }
        return fields;
    }

    internal static string DefaultMessage(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => BadRequestMessage,
        StatusCodes.Status401Unauthorized => ArdaNovaDenialAuthenticationHandler.AuthenticationRequiredMessage,
        StatusCodes.Status403Forbidden => ArdaNovaDenialAuthenticationHandler.ForbiddenMessage,
        StatusCodes.Status404NotFound => NotFoundMessage,
        StatusCodes.Status409Conflict => ConflictMessage,
        >= 500 => ExceptionHandlingMiddleware.ServerErrorMessage,
        _ => ReasonPhrases.GetReasonPhrase(statusCode) is { Length: > 0 } phrase ? phrase : BadRequestMessage,
    };
}
