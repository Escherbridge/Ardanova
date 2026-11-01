namespace ArdaNova.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    void ClearTrackedChanges();

    /// <summary>
    /// Accepts exactly <paramref name="writtenProperties"/> of a tracked row as what the database already holds (a
    /// compare-and-set wrote them), so no later save writes them again over a row another writer has moved since.
    /// Any OTHER pending change on the row stays pending. Throws on an empty list or a name the row does not have.
    /// </summary>
    void MarkPersisted(object entity, params string[] writtenProperties);
}
