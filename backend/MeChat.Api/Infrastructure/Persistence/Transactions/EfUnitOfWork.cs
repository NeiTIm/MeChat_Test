using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace MeChat.Api.Infrastructure.Persistence.Transactions;

public sealed class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<IDbContextTransaction> BeginSerializableAsync(CancellationToken ct) =>
        db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public void ClearTracking() => db.ChangeTracker.Clear();
}
