
using MeChat.Api.Features.Files.Repositories.Interfaces;
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Messages;

namespace MeChat.Api.Features.Files;

public sealed class FileService(
    IFileRepository repository,
    IFileStorage storage,
    MessageActionService actions,
    GroupService groups,
    TimeProvider clock)
{
    // =====================================================
    // FILE SIZE LIMITS
    // =====================================================

    private const long ImageLimit =
        10L * 1024 * 1024;

    private const long FileLimit =
        25L * 1024 * 1024;

    private const long AvatarLimit =
        5L * 1024 * 1024;

    // =====================================================
    // SUPPORTED FILE EXTENSIONS
    // =====================================================

    private static readonly Dictionary<string, string> Types =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",

            [".pdf"] = "application/pdf",

            [".docx"] =
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

            [".xlsx"] =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

            [".txt"] = "text/plain",
            [".zip"] = "application/zip"
        };

    // =====================================================
    // UPLOAD FILE
    // =====================================================

    public async Task<StoredFileDto?> UploadAsync(
        Guid actor,
        IFormFile file,
        string purpose,
        CancellationToken ct)
    {
        // =================================================
        // BASIC VALIDATION
        // =================================================

        if (file is null ||
            file.Length <= 0 ||
            purpose is not ("Message" or "Avatar"))
        {
            return null;
        }

        // =================================================
        // FILE NAME VALIDATION
        // =================================================

        var name = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > 255 ||
            name.Any(char.IsControl))
        {
            return null;
        }

        // =================================================
        // EXTENSION VALIDATION
        // =================================================

        var extension = Path.GetExtension(name);

        if (!Types.TryGetValue(
                extension,
                out var contentType))
        {
            return null;
        }

        // =================================================
        // DETECT IMAGE
        // =================================================

        var isImage = contentType.StartsWith(
            "image/",
            StringComparison.Ordinal);

        // Avatar must be an image.
        if (purpose == "Avatar" && !isImage)
        {
            return null;
        }

        // =================================================
        // FILE SIZE VALIDATION
        // =================================================

        var maximum = purpose == "Avatar"
            ? AvatarLimit
            : isImage
                ? ImageLimit
                : FileLimit;

        if (file.Length > maximum)
        {
            return null;
        }

        // =================================================
        // GENERATE STORAGE KEY
        // =================================================

        // Never use the client-supplied filename as
        // a physical storage key.

        var key = Guid.NewGuid().ToString("N");

        try
        {
            // =================================================
            // SAVE PHYSICAL FILE
            // =================================================

            await using (var source = file.OpenReadStream())
            {
                await storage.SaveAsync(
                    key,
                    source,
                    maximum,
                    ct);
            }

            // =================================================
            // VALIDATE FILE CONTENT
            // =================================================

            // Verify file signatures / content using
            // the existing FileContentValidator.

            if (!ValidContent(key, extension))
            {
                storage.Delete(key);

                return null;
            }

            // =================================================
            // CREATE DATABASE ENTITY
            // =================================================

            var entity = new StoredFile
            {
                Id = Guid.NewGuid(),
                StorageKey = key,
                OriginalFileName = name,
                ContentType = contentType,
                SizeBytes = file.Length,
                UploadedByUserId = actor,
                Purpose = purpose,
                Status = "Pending",
                CreatedAt = clock.GetUtcNow().UtcDateTime
            };

            repository.AddStoredFile(entity);

            // =================================================
            // SAVE FILE METADATA
            // =================================================

            try
            {
                await repository.SaveChangesAsync(ct);
            }
            catch
            {
                // The physical file must not become an
                // orphan if saving metadata fails.

                storage.Delete(key);

                throw;
            }

            // =================================================
            // RETURN RESPONSE
            // =================================================

            return new StoredFileDto(
                entity.Id,
                name,
                contentType,
                file.Length,
                entity.Status);
        }
        catch (InvalidDataException)
        {
            storage.Delete(key);

            return null;
        }
    }

    // =====================================================
    // READ FILE
    // =====================================================

    public async Task<FileRead?> ReadAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        // =================================================
        // GET FILE METADATA
        // =================================================

        var file =
            await repository.GetAvailableFileAsync(
                id,
                ct);

        if (file is null)
        {
            return null;
        }

        // =================================================
        // OWNER CAN READ OWN PENDING FILE
        // =================================================

        var ownPending =
            file.Status == "Pending" &&
            file.UploadedByUserId == actor;

        var allowed = ownPending;

        // =================================================
        // MESSAGE REFERENCES
        // =================================================

        var references =
            await repository.GetReferencedMessageIdsAsync(
                id,
                ct);

        // =================================================
        // PUBLIC USER AVATAR
        // =================================================

        if (!allowed &&
            file.Purpose == "Avatar" &&
            file.Status == "Avatar")
        {
            allowed =
                await repository.IsActiveUserAvatarAsync(
                    id,
                    ct);
        }

        // =================================================
        // GROUP AVATAR
        // =================================================

        if (!allowed &&
            file.Purpose == "Avatar" &&
            file.Status == "Avatar")
        {
            var groupIds =
                await repository
                    .GetGroupAvatarConversationIdsAsync(
                        id,
                        ct);

            foreach (var groupId in groupIds)
            {
                if (await groups.CanReadAsync(
                        actor,
                        groupId,
                        ct))
                {
                    allowed = true;
                    break;
                }
            }
        }

        // =================================================
        // MESSAGE ATTACHMENT AUTHORIZATION
        // =================================================

        // A file may be referenced by multiple messages.
        // Access is granted if the actor can view at
        // least one referenced message.

        if (!allowed)
        {
            foreach (var messageId in references)
            {
                var canView =
                    await actions.CanViewAttachmentAsync(
                        actor,
                        messageId,
                        ct);

                if (canView)
                {
                    allowed = true;
                    break;
                }
            }
        }

        // =================================================
        // ACCESS DENIED
        // =================================================

        if (!allowed)
        {
            return null;
        }

        // =================================================
        // OPEN PHYSICAL FILE
        // =================================================

        try
        {
            var stream =
                storage.OpenRead(file.StorageKey);

            return new FileRead(
                stream,
                file.ContentType,
                file.OriginalFileName);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    // =====================================================
    // SET USER AVATAR
    // =====================================================

    public Task<bool> SetUserAvatarAsync(
        Guid actor,
        Guid id,
        CancellationToken ct)
    {
        // The repository updates StoredFile and User
        // together in a Serializable transaction.

        return repository.SetUserAvatarAtomicAsync(
            actor,
            id,
            ct);
    }

    // =====================================================
    // VALIDATE PHYSICAL FILE CONTENT
    // =====================================================

    private bool ValidContent(
        string key,
        string extension)
    {
        using var stream =
            storage.OpenRead(key);

        return FileContentValidator.IsValid(
            stream,
            extension);
    }
}
