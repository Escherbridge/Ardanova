namespace ArdaNova.Application.Services.Implementations;

using System.Collections.Frozen;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;

public class EnumLookupService : IEnumLookupService
{
    private static readonly FrozenDictionary<string, Type> EnumTypesByName = typeof(TaskPriority).Assembly
        .GetTypes()
        .Where(t => t.IsEnum && t.Namespace == "ArdaNova.Domain.Models.Enums")
        .ToFrozenDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<string> SortedEnumNames = EnumTypesByName.Keys
        .Order(StringComparer.Ordinal)
        .ToList();

    public Result<IReadOnlyList<string>> GetAllEnumNames()
    {
        return Result<IReadOnlyList<string>>.Success(SortedEnumNames);
    }

    public Result<IReadOnlyList<string>> GetEnumValues(string enumName)
    {
        if (!EnumTypesByName.TryGetValue(enumName, out var enumType))
        {
            return Result<IReadOnlyList<string>>.NotFound($"Enum '{enumName}' not found.");
        }

        var values = Enum.GetNames(enumType);
        return Result<IReadOnlyList<string>>.Success(values);
    }
}
