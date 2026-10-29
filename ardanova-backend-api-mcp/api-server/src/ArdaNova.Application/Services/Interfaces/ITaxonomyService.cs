namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface ITaxonomyService
{
    /// <summary>Every ACTIVE term, optionally narrowed to one kind; refused if <paramref name="kind"/> names none.</summary>
    Task<Result<IReadOnlyList<TaxonomyTermDto>>> GetActiveTermsAsync(
        string? kind,
        CancellationToken ct = default);
}
