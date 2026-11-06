
namespace MeChat.Api.Features.Files;

public static class FileRetentionPolicy
{
    // =====================================================
    // FILE CLEANUP ELIGIBILITY
    // =====================================================

    public static string? Reason(
        FileRetentionFacts file,
        DateTime now,
        FileRetentionOptions options)
    {
        // =================================================
        // PROTECTED FILES
        // =================================================

        // Never delete bytes while referenced by:
        // - A user or group avatar
        // - Any report evidence
        // - At least one non-recalled message

        if (file.HasAvatar ||
            file.HasEvidence ||
            file.HasLiveMessage)
        {
            return null;
        }

        // =================================================
        // EXPIRED PENDING UPLOAD
        // =================================================

        if (file.Status == "Pending" &&
            file.MessageReferences == 0 &&
            file.CreatedAt <=
                now.AddHours(-options.PendingHours))
        {
            return "ExpiredPendingUpload";
        }

        // =================================================
        // UNREFERENCED AVATAR
        // =================================================

        if (file.Status == "Avatar" &&
            file.MessageReferences == 0 &&
            file.CreatedAt <=
                now.AddDays(-options.AvatarDays))
        {
            return "UnreferencedAvatar";
        }

        // =================================================
        // ALL MESSAGE REFERENCES RECALLED
        // =================================================

        if (file.Status == "Attached" &&
            file.MessageReferences > 0 &&
            file.LastRecall is { } recalled &&
            recalled <=
                now.AddDays(-options.RecalledDays))
        {
            return "AllMessageReferencesRecalled";
        }

        return null;
    }
}
