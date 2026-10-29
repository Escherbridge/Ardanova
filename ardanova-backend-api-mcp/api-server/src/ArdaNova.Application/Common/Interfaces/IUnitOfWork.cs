namespace ArdaNova.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    void ClearTrackedChanges();

    /// <summary>
    /// Accepts a tracked row's current values as what the database already holds (a compare-and-set wrote
    /// them), so no later save writes them again over a row another writer has moved since.
    /// </summary>
    void MarkPersisted(object entity);
}
