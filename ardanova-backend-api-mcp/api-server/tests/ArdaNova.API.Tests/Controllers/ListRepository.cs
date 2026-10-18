namespace ArdaNova.API.Tests.Controllers;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;

/// <summary>List-backed repository; queries run the same expressions EF would translate.</summary>
internal sealed class ListRepository<T> : IRepository<T> where T : class
{
    public List<T> Items { get; } = [];

    private static string IdOf(T entity) => (string)typeof(T).GetProperty("id")!.GetValue(entity)!;

    public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => System.Threading.Tasks.Task.FromResult(Items.FirstOrDefault(item => IdOf(item) == id));
    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => System.Threading.Tasks.Task.FromResult<IReadOnlyList<T>>(Items.ToList());
    public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => System.Threading.Tasks.Task.FromResult<IReadOnlyList<T>>(Items.AsQueryable().Where(predicate).ToList());
    public Task<IReadOnlyList<TResult>> FindProjectedAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
        => System.Threading.Tasks.Task.FromResult<IReadOnlyList<TResult>>(Items.AsQueryable().Where(predicate).Select(selector).ToList());
    public Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => System.Threading.Tasks.Task.FromResult(Items.AsQueryable().FirstOrDefault(predicate));
    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Items.Add(entity);
        return System.Threading.Tasks.Task.FromResult(entity);
    }
    public Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var added = entities.ToList();
        Items.AddRange(added);
        return System.Threading.Tasks.Task.FromResult<IEnumerable<T>>(added);
    }
    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        Items.Remove(entity);
        return System.Threading.Tasks.Task.CompletedTask;
    }
    public Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities.ToList())
            Items.Remove(entity);
        return System.Threading.Tasks.Task.CompletedTask;
    }
    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => System.Threading.Tasks.Task.FromResult(Items.AsQueryable().Any(predicate));
    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => System.Threading.Tasks.Task.FromResult(predicate is null ? Items.Count : Items.AsQueryable().Count(predicate));
    public IQueryable<T> Query() => Items.AsQueryable();
    public Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
