
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Notifications;

namespace MeChat.Api.Features.Messages;

public sealed class UnreadSummaryService(
    PrivateChatService chats,
    GroupService groups,
    MessageRequestService requests,
    MessageStatusService status,
    NotificationService notifications)
{
    // =====================================================
    // GET UNREAD SUMMARY
    // =====================================================

    public async Task<UnreadSummaryDto> GetAsync(
        Guid actor,
        CancellationToken ct)
    {
        var items = new List<ConversationUnreadDto>();

        // =================================================
        // PRIVATE CONVERSATIONS
        // =================================================

        var privateConversations =
            await chats.ListAsync(actor, ct);

        foreach (var conversation in privateConversations)
        {
            var unread =
                await status.UnreadAsync(
                    actor,
                    conversation.Id,
                    ct);

            if (unread is null)
                continue;

            items.Add(
                new ConversationUnreadDto(
                    conversation.Id,
                    "Private",
                    unread.Count,
                    unread.LastReadSequence));
        }

        // =================================================
        // GROUP CONVERSATIONS
        // =================================================

        var groupConversations =
            await groups.ListAsync(actor, ct);

        foreach (var group in groupConversations)
        {
            // Exclude memberships without an active role.
            if (group.MyRole is null)
                continue;

            var unread =
                await status.UnreadAsync(
                    actor,
                    group.Id,
                    ct);

            if (unread is null)
                continue;

            items.Add(
                new ConversationUnreadDto(
                    group.Id,
                    "Group",
                    unread.Count,
                    unread.LastReadSequence));
        }

        // =================================================
        // MAIN CHAT UNREAD COUNT
        // =================================================

        var mainCount = items.Sum(
            item => item.Count);

        // =================================================
        // MESSAGE REQUEST UNREAD COUNT
        // =================================================

        var requestCount =
            await requests.UnreadCountAsync(
                actor,
                ct);

        // =================================================
        // NOTIFICATION UNREAD COUNT
        // =================================================

        var unreadNotifications =
            await notifications.UnreadAsync(
                actor,
                ct);

        var notificationCount =
            unreadNotifications.Count;

        // =================================================
        // RESPONSE
        // =================================================

        return new UnreadSummaryDto(
            mainCount,
            requestCount,
            items,
            notificationCount);
    }
}
