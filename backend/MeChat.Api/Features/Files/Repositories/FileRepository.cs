
using System.Data;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Features.Files.Repositories.Interfaces;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Api.Features.Files.Repositories;

public sealed class FileRepository(
    AppDbContext db) : IFileRepository
{
    // =====================================================
    // ADD STORED FILE
    // =====================================================

    public void AddStoredFile(StoredFile file)
    {
        db.StoredFiles.Add(file);
    }

    // =====================================================
    // SAVE CHANGES
    // =====================================================

    public Task<int> SaveChangesAsync(
        CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }

    // =====================================================
    // GET AVAILABLE FILE
    // =====================================================

    public Task<StoredFile?> GetAvailableFileAsync(
        Guid fileId,
        CancellationToken ct)
    {
        return db.StoredFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                file =>
                    file.Id == fileId &&
                    file.DeletedAt == null,
                ct);
    }

    // =====================================================
    // MESSAGE ATTACHMENT REFERENCES
    // =====================================================

    public Task<List<Guid>> GetReferencedMessageIdsAsync(
        Guid fileId,
        CancellationToken ct)
    {
        return db.MessageAttachments
            .AsNoTracking()
            .Where(attachment =>
                attachment.StoredFileId == fileId)
            .Select(attachment =>
                attachment.MessageId)
            .ToListAsync(ct);
    }

    // =====================================================
    // ACTIVE USER AVATAR
    // =====================================================

    public Task<bool> IsActiveUserAvatarAsync(
        Guid fileId,
        CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.AvatarFileId == fileId &&
                    user.EmailVerifiedAt != null &&
                    user.DeactivatedAt == null,
                ct);
    }

    // =====================================================
    // GROUP AVATAR REFERENCES
    // =====================================================

    public Task<List<Guid>>
        GetGroupAvatarConversationIdsAsync(
            Guid fileId,
            CancellationToken ct)
    {
        return db.GroupProfiles
            .AsNoTracking()
            .Where(group =>
                group.AvatarFileId == fileId)
            .Select(group =>
                group.ConversationId)
            .ToListAsync(ct);
    }

    // =====================================================
    // SET USER AVATAR - ATOMIC TRANSACTION
    // =====================================================

    public async Task<bool> SetUserAvatarAtomicAsync(
        Guid actor,
        Guid fileId,
        CancellationToken ct)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        try
        {
            // =================================================
            // VALIDATE PENDING AVATAR FILE
            // =================================================

            var file =
                await db.StoredFiles.SingleOrDefaultAsync(
                    stored =>
                        stored.Id == fileId &&
                        stored.UploadedByUserId == actor &&
                        stored.Purpose == "Avatar" &&
                        stored.Status == "Pending" &&
                        stored.DeletedAt == null,
                    ct);

            // =================================================
            // VALIDATE USER
            // =================================================

            var user =
                await db.Users.SingleOrDefaultAsync(
                    account =>
                        account.Id == actor &&
                        account.DeactivatedAt == null,
                    ct);

            if (file is null || user is null)
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();

                return false;
            }

            // =================================================
            // UPDATE FILE STATUS
            // =================================================

            file.Status = "Avatar";

            // =================================================
            // UPDATE USER AVATAR
            // =================================================

            user.AvatarFileId = fileId;

            // =================================================
            // SAVE BOTH ENTITIES
            // =================================================

            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return true;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);
            }
            finally
            {
                db.ChangeTracker.Clear();
            }

            throw;
        }
    }
}
