using ArdaNova.API.Middleware;
using ArdaNova.Application.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArdaNova.API.Filters;

/// <summary>Runs the registered request validator for each bound argument and answers 400 before the action. See Filters/AGENTS.md §Request validation.</summary>
public sealed class RequestValidationFilter(IEnumerable<IRequestValidator> validators) : IActionFilter
{
    private readonly Dictionary<Type, IRequestValidator> _byType = validators.ToDictionary(v => v.RequestType);

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Result is not null || _byType.Count == 0)
            return;

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null || !_byType.TryGetValue(argument.GetType(), out var validator))
                continue;
            foreach (var (field, messages) in validator.Validate(argument))
                errors[field] = messages;
        }

        if (errors.Count == 0)
            return;

        context.Result = new BadRequestObjectResult(new
        {
            error = ApiErrorEnvelope.ValidationFailedMessage,
            statusCode = StatusCodes.Status400BadRequest,
            traceId = ApiErrorEnvelope.TraceId(context.HttpContext),
            errors,
        });
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
