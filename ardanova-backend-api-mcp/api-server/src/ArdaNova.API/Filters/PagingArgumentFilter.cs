using ArdaNova.Application.Common.Results;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArdaNova.API.Filters;

/// <summary>Clamps bound page/pageSize/limit arguments into the shared paging bounds before any action runs. See Filters/AGENTS.md §Paging bounds.</summary>
public sealed class PagingArgumentFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var arguments = context.ActionArguments;
        var pageSize = Read(arguments, "pageSize");
        if (pageSize is not null)
            arguments[Key(arguments, "pageSize")!] = PageRequest.ClampPageSize(pageSize.Value);

        var limit = Read(arguments, "limit");
        if (limit is not null)
            arguments[Key(arguments, "limit")!] = PageRequest.ClampPageSize(limit.Value);

        var page = Read(arguments, "page");
        if (page is not null)
            arguments[Key(arguments, "page")!] = PageRequest.ClampPage(
                page.Value,
                PageRequest.ClampPageSize(pageSize ?? PageRequest.MaximumPageSize));
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static int? Read(IDictionary<string, object?> arguments, string name)
        => Key(arguments, name) is { } key && arguments[key] is int value ? value : null;

    private static string? Key(IDictionary<string, object?> arguments, string name)
        => arguments.Keys.FirstOrDefault(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));
}
