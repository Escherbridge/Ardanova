namespace ArdaNova.Application.Tests.Support;

using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>A list-backed repository so service flows can be driven end to end and asserted on stored rows rather than on mock calls.</summary>
public class InMemoryRepository<T> : IRepository<T> where T : class
{
    private static readonly PropertyInfo IdProperty =
        typeof(T).GetProperty("id") ?? throw new InvalidOperationException($"{typeof(T).Name} has no id");

    public List<T> Rows { get; } = new();

    /// <summary>Runs after a FindOneAsync read and before its result returns: models another request writing in between.</summary>
    public Action? AfterFindOne { get; set; }

    private static string IdOf(T entity) => (string)IdProperty.GetValue(entity)!;

    public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(Rows.FirstOrDefault(r => IdOf(r) == id));

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<T>>(Rows.ToList());

    public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<T>>(Rows.Where(predicate.Compile()).ToList());

    public Task<IReadOnlyList<TResult>> FindProjectedAsync<TResult>(
        Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TResult>>(Rows.Where(predicate.Compile()).Select(selector.Compile()).ToList());

    public Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var found = Rows.FirstOrDefault(predicate.Compile());
        AfterFindOne?.Invoke();
        return Task.FromResult(found);
    }

    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Rows.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();
        Rows.AddRange(list);
        return Task.FromResult<IEnumerable<T>>(list);
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        Rows.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities.ToList())
            Rows.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Rows.Any(predicate.Compile()));

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => Task.FromResult(predicate is null ? Rows.Count : Rows.Count(predicate.Compile()));

    public Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var all = predicate is null ? Rows : Rows.Where(predicate.Compile()).ToList();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<T>(items, all.Count, page, pageSize));
    }

    public IQueryable<T> Query() => Rows.AsQueryable();
}

/// <summary>A unit of work that enforces declared unique keys at save time, the way the database's unique indexes do.</summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly List<Func<Exception?>> _constraints = new();

    public int Saves { get; private set; }

    /// <summary>Declares a unique key over a repository's rows; a save that would store a duplicate fails with SQLSTATE 23505 and keeps the earliest row.</summary>
    public void Unique<T, TKey>(InMemoryRepository<T> repository, Func<T, TKey> key) where T : class
        => _constraints.Add(() =>
        {
            var duplicates = repository.Rows.GroupBy(key).Where(g => g.Count() > 1).ToList();
            if (duplicates.Count == 0)
                return null;
            foreach (var group in duplicates)
                foreach (var extra in group.Skip(1).ToList())
                    repository.Rows.Remove(extra);
            return new DbUpdateException("unique violation", new UniqueViolation());
        });

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var constraint in _constraints)
        {
            var failure = constraint();
            if (failure is not null)
                throw failure;
        }
        Saves++;
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void ClearTrackedChanges() { }
    public void Dispose() { }

    private sealed class UniqueViolation : DbException
    {
        public override string SqlState => "23505";
    }
}
