namespace ArdaNova.API.Middleware;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>Last-resort handler: unexpected exceptions become an opaque 500 envelope. See AGENTS.md §Error envelope.</summary>
public class ExceptionHandlingMiddleware
{
    public const string ServerErrorMessage = "An error occurred processing your request.";
    public const string BadRequestMessage = "The request could not be read.";
    public const string InvalidValueMessage = "The request contained an invalid value.";
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller disconnected; there is nobody left to answer.
        }
        catch (Exception ex)
        {
            var (statusCode, message) = Classify(ex);
            if (statusCode >= StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning(ex, "Rejected request for {Method} {Path} with {StatusCode}", context.Request.Method, context.Request.Path, statusCode);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started for {Method} {Path}; aborting instead of writing an error envelope", context.Request.Method, context.Request.Path);
                throw;
            }

            context.Response.Clear();
            await ApiErrorEnvelope.WriteAsync(context, statusCode, message);
        }
    }

    /// <summary>Maps an escaped exception to a status and a fixed message; exception text is never echoed.</summary>
    internal static (int StatusCode, string Message) Classify(Exception exception) => exception switch
    {
        BadHttpRequestException badRequest when badRequest.StatusCode is >= 400 and < 500
            => (badRequest.StatusCode, BadRequestMessage),
        ArgumentNullException
            => (StatusCodes.Status500InternalServerError, ServerErrorMessage),
        ArgumentException
            => (StatusCodes.Status400BadRequest, InvalidValueMessage),
        _ => (StatusCodes.Status500InternalServerError, ServerErrorMessage)
    };
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
