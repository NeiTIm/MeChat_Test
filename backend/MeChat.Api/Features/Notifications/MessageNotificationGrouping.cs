using MeChat.Api.Features.Messages.Models;

namespace MeChat.Api.Features.Notifications;

public sealed record MessageNotificationCandidate(Notification Notification, Guid ConversationId, long Sequence);

public static class MessageNotificationGrouping
{
    // Group the active unread projection, retaining individual records for history and Trash.
    public static bool IsGrouped(string type) => type is "Message" or "GroupMessage";

    public static IEnumerable<IOrderedEnumerable<MessageNotificationCandidate>> Candidates(
        IEnumerable<MessageNotificationCandidate> rows) => rows
        .GroupBy(row => (row.Notification.RecipientUserId, row.ConversationId, row.Notification.Type))
        .Select(group => group.OrderBy(row => row.Sequence)
            .ThenBy(row => row.Notification.CreatedAt)
            .ThenBy(row => row.Notification.Id));

    // Keep the seed predicates inside the SQL statement: a replay of an already
    // read/deleted notification must never consume a subsequent unread cycle.
    public static IQueryable<Notification> ActionRows(
        IQueryable<Notification> notifications, IQueryable<Message> messages, Guid id, Guid actor)
    {
        var seeds = from notification in notifications
                    join message in messages on notification.ReferenceId equals (Guid?)message.Id
                    where notification.Id == id && notification.RecipientUserId == actor &&
                        notification.ReadAt == null && notification.DeletedAt == null &&
                        (notification.Type == "Message" || notification.Type == "GroupMessage")
                    select new { notification.Type, message.ConversationId };

        return notifications.Where(notification => notification.RecipientUserId == actor &&
            (notification.Id == id || (notification.ReadAt == null && seeds.Any(seed =>
                seed.Type == notification.Type && messages.Any(message =>
                    message.Id == notification.ReferenceId && message.ConversationId == seed.ConversationId)))));
    }
}
