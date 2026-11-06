using Microsoft.EntityFrameworkCore.Storage;

namespace MeChat.Api.Infrastructure.Persistence.Transactions;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginSerializableAsync(CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
    void ClearTracking();
}
