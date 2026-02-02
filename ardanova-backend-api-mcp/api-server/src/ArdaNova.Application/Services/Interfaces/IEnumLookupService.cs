namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;

public interface IEnumLookupService
{
    Result<IReadOnlyList<string>> GetEnumValues(string enumName);
    Result<IReadOnlyList<string>> GetAllEnumNames();
}
