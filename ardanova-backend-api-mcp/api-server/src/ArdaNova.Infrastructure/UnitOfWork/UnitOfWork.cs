namespace ArdaNova.Infrastructure.UnitOfWork;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork : IUnitOfWork
{
    private readonly ArdaNovaDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ArdaNovaDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void ClearTrackedChanges()
    {
        _context.ChangeTracker.Clear();
    }

    public void MarkPersisted(object entity, params string[] writtenProperties)
    {
        if (writtenProperties.Length == 0)
            throw new ArgumentException("Name the columns the compare-and-set wrote; a whole-row accept would drop other pending changes.", nameof(writtenProperties));

        var entry = _context.Entry(entity);
        if (entry.State is not (EntityState.Modified or EntityState.Unchanged))
            return;

        // Only the CAS's own columns: Property() throws for a name the row does not have.
        foreach (var name in writtenProperties)
        {
            var property = entry.Property(name);
            property.OriginalValue = property.CurrentValue;
            property.IsModified = false;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
