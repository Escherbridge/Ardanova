using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;

namespace ArdaNova.API.Tests.Filters;

/// <summary>A list-backed repository standing in for the database edge; queries run the same expressions EF would translate.</summary>
public sealed class InMemoryRepository<T>(List<T> rows) : IRepository<T> where T : class
{
    public Task<IReadOnlyList<TResult>> FindProjectedAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TResult>>(rows.Where(predicate.Compile()).Select(selector.Compile()).ToList());

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(rows.Any(predicate.Compile()));

    public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<T>>(rows.Where(predicate.Compile()).ToList());

    public Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(rows.FirstOrDefault(predicate.Compile()));

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => Task.FromResult(predicate is null ? rows.Count : rows.Count(predicate.Compile()));

    public IQueryable<T> Query() => rows.AsQueryable();

    public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
