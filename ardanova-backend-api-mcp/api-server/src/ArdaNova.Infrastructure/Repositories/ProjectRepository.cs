namespace ArdaNova.Infrastructure.Repositories;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>Project queries. Visibility and search-term rules live in <see cref="ProjectQueryFilters"/>.</summary>
/// <remarks>
/// Column contract for the presentation fields added to <see cref="Project"/> in the same wave:
/// <c>summary</c> is part of the search index below; the media-path column(s) and the funding-goal
/// columns need no repository code — EF maps them by convention and AutoMapper carries them to the
/// DTO — so nothing else here has to change when they land.
/// </remarks>
public class ProjectRepository : Repository<Project>, IProjectRepository
{
    private const int MaximumPageSize = 100;

    public ProjectRepository(ArdaNovaDbContext context) : base(context)
    {
    }

    /// <summary>Public project search: this overload carries no viewer, so drafts are never returned.</summary>
    public Task<PagedResult<Project>> SearchAsync(
        string? searchTerm,
        ProjectStatus? status,
        string? category,
        ProjectType? projectType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => PageAsync(
            ProjectQueryFilters.Build(ProjectViewer.Anonymous, searchTerm, status, category, projectType),
            page,
            pageSize,
            cancellationToken);

    /// <summary>Newest-first, id-stable paging so page N never repeats or skips a project.</summary>
    public override Task<PagedResult<Project>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<Project, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => PageAsync(predicate, page, pageSize, cancellationToken);

    public async Task<Project?> GetWithUserAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.id == id, cancellationToken);
    }

    public async Task<Project?> GetBySlugWithUserAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.slug == slug, cancellationToken);
    }

    /// <summary>Counts and pages entirely in SQL, so the caller never receives an over-large or unfiltered page.</summary>
    private async Task<PagedResult<Project>> PageAsync(
        Expression<Func<Project, bool>>? predicate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var requestedPage = page < 1 ? 1 : page;
        var requestedPageSize = pageSize switch
        {
            < 1 => 1,
            > MaximumPageSize => MaximumPageSize,
            _ => pageSize
        };

        var query = predicate is null ? _dbSet.AsQueryable() : _dbSet.Where(predicate);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.createdAt)
            .ThenBy(p => p.id)
            .Skip((requestedPage - 1) * requestedPageSize)
            .Take(requestedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Project>(items, totalCount, requestedPage, requestedPageSize);
    }
}
