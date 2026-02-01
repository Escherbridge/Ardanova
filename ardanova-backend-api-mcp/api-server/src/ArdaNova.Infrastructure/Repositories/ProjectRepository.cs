namespace ArdaNova.Infrastructure.Repositories;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ArdaNovaDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Project>> SearchAsync(
        string? searchTerm,
        ProjectStatus? status,
        string? category,
        ProjectType? projectType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Apply search term filter (searches title, description, and tags)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.title.ToLower().Contains(term) ||
                p.description.ToLower().Contains(term) ||
                (p.tags != null && p.tags.ToLower().Contains(term)));
        }

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(p => p.status == status.Value);
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.categories.Contains(category));
        }

        // Apply project type filter
        if (projectType.HasValue)
        {
            query = query.Where(p => p.projectType == projectType.Value);
        }

        // Order by most recent first
        query = query.OrderByDescending(p => p.createdAt);

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Project>(items, totalCount, page, pageSize);
    }

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
}
