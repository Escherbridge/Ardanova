namespace ArdaNova.Application.Validators;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Validates one request body type; the API pipeline runs every registered validator before an action. See Validators/AGENTS.md.</summary>
public interface IRequestValidator
{
    Type RequestType { get; }

    /// <summary>Field name to messages; empty when the request is acceptable.</summary>
    IReadOnlyDictionary<string, string[]> Validate(object request);
}

/// <summary>Typed base: subclasses add failures to a <see cref="ValidationErrors"/> collector.</summary>
public abstract class RequestValidator<TRequest> : IRequestValidator
{
    public Type RequestType => typeof(TRequest);

    public IReadOnlyDictionary<string, string[]> Validate(object request)
    {
        var errors = new ValidationErrors();
        Check((TRequest)request, errors);
        return errors.ToDictionary();
    }

    protected abstract void Check(TRequest request, ValidationErrors errors);
}

/// <summary>Collects field failures under camelCase keys, matching the JSON the client sent.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public void Add(string field, string message)
    {
        var key = char.ToLowerInvariant(field[0]) + field[1..];
        if (!_errors.TryGetValue(key, out var messages))
            _errors[key] = messages = [];
        messages.Add(message);
    }

    public void When(bool failed, string field, string message)
    {
        if (failed)
            Add(field, message);
    }

    public IReadOnlyDictionary<string, string[]> ToDictionary()
        => _errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
}

public static class RequestValidatorRegistration
{
    /// <summary>Registers every concrete <see cref="IRequestValidator"/> in this assembly.</summary>
    public static IServiceCollection AddRequestValidators(this IServiceCollection services)
    {
        var validators = typeof(IRequestValidator).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true } && typeof(IRequestValidator).IsAssignableFrom(type));
        foreach (var validator in validators)
            services.AddSingleton(typeof(IRequestValidator), validator);
        return services;
    }
}
