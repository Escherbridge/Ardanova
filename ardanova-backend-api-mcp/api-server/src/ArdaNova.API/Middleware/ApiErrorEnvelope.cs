using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ArdaNova.API.Middleware;

/// <summary>The single JSON error shape the pipeline writes: { error, statusCode, traceId[, errors] }. See AGENTS.md §Error envelope.</summary>
public static class ApiErrorEnvelope
{
    public const string ValidationFailedMessage = "One or more validation errors occurred.";
    private const string InvalidInputMessage = "The input was not valid.";

    public static object Create(HttpContext context, int statusCode, string error)
        => new { error, statusCode, traceId = TraceId(context) };

    public static Task WriteAsync(HttpContext context, int statusCode, string error)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(Create(context, statusCode, error));
    }

    /// <summary>Replaces MVC's default ProblemDetails 400 with the shared envelope and drops framework/type-name text.</summary>
    public static IMvcBuilder AddArdaNovaErrorEnvelope(this IMvcBuilder mvc)
    {
        mvc.AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false);
        mvc.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = actionContext =>
            {
                var errors = actionContext.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors
                            .Select(error => error.Exception is not null || string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? InvalidInputMessage
                                : error.ErrorMessage)
                            .Distinct()
                            .ToArray());
                return new BadRequestObjectResult(new
                {
                    error = ValidationFailedMessage,
                    statusCode = StatusCodes.Status400BadRequest,
                    traceId = TraceId(actionContext.HttpContext),
                    errors,
                });
            };
        });
        return mvc;
    }

    public static string TraceId(HttpContext context)
        => Activity.Current?.Id ?? context.TraceIdentifier;
}
