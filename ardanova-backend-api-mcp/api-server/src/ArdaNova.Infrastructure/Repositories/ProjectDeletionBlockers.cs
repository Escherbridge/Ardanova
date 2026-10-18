namespace ArdaNova.Infrastructure.Repositories;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

/// <summary>A table whose required foreign key to Project is ON DELETE RESTRICT in the database.</summary>
public sealed record ProjectDependent(string Kind, string Table, string Column);

/// <summary>Finds blocking dependents with one EXISTS-per-table query, the table list derived from the EF model. See Services/Implementations/AGENTS.md §Project delete.</summary>
public sealed class ProjectDeletionBlockers : IProjectDeletionBlockers
{
    private readonly ArdaNovaDbContext _context;

    public ProjectDeletionBlockers(ArdaNovaDbContext context) => _context = context;

    /// <summary>Every required FK to Project: Prisma emits RESTRICT for required relations and SET NULL for optional ones.</summary>
    public static IReadOnlyList<ProjectDependent> RestrictingDependents(IModel model)
        => model.FindEntityType(typeof(Project))!
            .GetReferencingForeignKeys()
            .Where(foreignKey => foreignKey.IsRequired)
            .Select(foreignKey =>
            {
                var table = foreignKey.DeclaringEntityType.GetTableName()!;
                var store = StoreObjectIdentifier.Table(table, foreignKey.DeclaringEntityType.GetSchema());
                return new ProjectDependent(
                    foreignKey.DeclaringEntityType.ClrType.Name,
                    table,
                    foreignKey.Properties.Single().GetColumnName(store)!);
            })
            .Distinct()
            .OrderBy(dependent => dependent.Kind, StringComparer.Ordinal)
            .ToList();

    public async Task<IReadOnlyList<string>> FindAsync(string projectId, CancellationToken ct = default)
    {
        // Identifiers come from the compiled EF model, never from input; only the id is a parameter.
        var probes = RestrictingDependents(_context.Model).Select(dependent =>
            $"SELECT '{dependent.Kind}' AS \"Value\" WHERE EXISTS (SELECT 1 FROM \"{dependent.Table}\" WHERE \"{dependent.Column}\" = @projectId)");
        var blockers = await _context.Database
            .SqlQueryRaw<string>(string.Join(" UNION ALL ", probes), new NpgsqlParameter("projectId", projectId))
            .ToListAsync(ct);
        return blockers.Order(StringComparer.Ordinal).ToList();
    }
}
