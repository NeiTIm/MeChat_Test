
using System.Collections.Concurrent;
using MeChat.Api.Features.Conversations;
using MeChat.Api.Features.Groups;
using MeChat.Api.Features.Presence.Repositories.Interfaces;
using MeChat.Api.Realtime.Interfaces;

namespace MeChat.Api.Features.Presence;

public sealed class PresenceService(
    IPresenceRepository repository,
    TimeProvider clock,
    PrivateChatService privateChats,
    GroupService groups,
    IRealtimePublisher publisher,
    ILogger<PresenceService> logger)
{
    // =====================================================
    // IN-MEMORY CONNECTION STATE
    // =====================================================

    // One user may have multiple SignalR connections.
    // A user is online while at least one connection exists.

    private static readonly ConcurrentDictionary<
        Guid,
        ConcurrentDictionary<string, byte>> Connections = new();

    private static readonly object ConnectionGate = new();

    // =====================================================
    // TYPING THROTTLE STATE
    // =====================================================

    private static readonly ConcurrentDictionary<
        (Guid UserId, Guid ConversationId),
        DateTime> LastTyping = new();

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // =====================================================
    // USER CONNECTED
    // =====================================================

    public async Task ConnectedAsync(
        Guid actor,
        string connectionId,
        CancellationToken ct)
    {
        bool wasOffline;

        lock (ConnectionGate)
        {
            // Create a connection collection if necessary.
            var devices = Connections.GetOrAdd(
                actor,
                _ => new ConcurrentDictionary<string, byte>());

            // Was this the user's first active connection?
            wasOffline = devices.IsEmpty;

            // Register the SignalR connection.
            devices[connectionId] = 0;
        }

        // Do not broadcast every time a new device connects.
        // Only broadcast Offline -> Online.
        if (wasOffline)
        {
            await BroadcastPresenceAsync(
                actor,
                ct);
        }
    }

    // =====================================================
    // USER DISCONNECTED
    // =====================================================

    public async Task DisconnectedAsync(
        Guid actor,
        string connectionId,
        CancellationToken ct)
    {
        lock (ConnectionGate)
        {
            // No registered connections.
            if (!Connections.TryGetValue(
                    actor,
                    out var devices))
            {
                return;
            }

            // Remove only the specified connection.
            devices.TryRemove(connectionId, out _);

            // Other devices are still connected.
            if (!devices.IsEmpty)
            {
                return;
            }

            // Last device disconnected.
            Connections.TryRemove(actor, out _);
        }

        // =================================================
        // UPDATE LAST SEEN
        // =================================================

        await repository.UpdateLastSeenAsync(
            actor,
            Now,
            ct);

        // =================================================
        // BROADCAST OFFLINE STATUS
        // =================================================

        await BroadcastPresenceAsync(
            actor,
            ct);
    }

    // =====================================================
    // GET USER PRESENCE
    // =====================================================

    public async Task<PresenceDto?> GetAsync(
        Guid viewer,
        Guid target,
        CancellationToken ct)
    {
        // =================================================
        // ACTIVE USER VALIDATION
        // =================================================

        var viewerActive =
            await repository.IsActiveUserAsync(
                viewer,
                ct);

        if (!viewerActive)
            return null;

        var targetActive =
            await repository.IsActiveUserAsync(
                target,
                ct);

        if (!targetActive)
            return null;

        // =================================================
        // SUSPENSION
        // =================================================

        var suspended =
            await repository.HasActiveSuspensionAsync(
                viewer,
                target,
                Now,
                ct);

        if (suspended)
            return null;

        // =================================================
        // RELATIONSHIP / BLOCK VALIDATION
        // =================================================

        if (viewer != target)
        {
            var blocked =
                await repository.IsBlockedBetweenAsync(
                    viewer,
                    target,
                    ct);

            if (blocked)
                return null;

            // Presence is visible only to friends or
            // users with accepted private chat access.

            var friendship =
                await repository.AreFriendsAsync(
                    viewer,
                    target,
                    ct);

            var accepted = false;

            if (!friendship)
            {
                accepted =
                    await repository
                        .HasAcceptedPrivateChatAccessAsync(
                            viewer,
                            target,
                            ct);
            }

            if (!friendship && !accepted)
                return null;
        }

        // =================================================
        // PRIVACY SETTINGS
        // =================================================

        var privacy =
            await repository.GetPrivacyAsync(
                target,
                ct);

        // =================================================
        // IN-MEMORY ONLINE STATUS
        // =================================================

        bool online;

        lock (ConnectionGate)
        {
            online =
                Connections.TryGetValue(
                    target,
                    out var devices) &&
                !devices.IsEmpty;
        }

        // =================================================
        // LAST SEEN
        // =================================================

        var lastSeen =
            await repository.GetLastSeenAsync(
                target,
                ct);

        // =================================================
        // PRIVACY-AWARE RESPONSE
        // =================================================

        // Users can always see their own status.
        // Other users only see enabled privacy fields.

        bool? visibleOnline =
            viewer == target ||
            privacy.ShowOnline
                ? online
                : null;

        DateTime? visibleLastSeen =
            viewer == target ||
            privacy.ShowLastSeen
                ? lastSeen
                : null;

        return new PresenceDto(
            target,
            visibleOnline,
            visibleLastSeen);
    }

    // =====================================================
    // TYPING INDICATOR
    // =====================================================

    public async Task<bool> TypingAsync(
        Guid actor,
        Guid sessionId,
        Guid conversationId,
        bool started,
        CancellationToken ct)
    {
        // =================================================
        // VALIDATE ACTIVE SESSION
        // =================================================

        var sessionActive =
            await repository.HasActiveSessionAsync(
                actor,
                sessionId,
                ct);

        if (!sessionActive)
            return false;

        // =================================================
        // GET CONVERSATION TYPE
        // =================================================

        var type =
            await repository.GetConversationTypeAsync(
                conversationId,
                ct);

        // =================================================
        // CHECK SENDER PERMISSION
        // =================================================

        if (type == "Group")
        {
            if (!await groups.CanSendAsync(
                    actor,
                    conversationId,
                    ct))
            {
                return false;
            }
        }
        else if (type == "Private")
        {
            if (!await privateChats.CanSendMessageAsync(
                    actor,
                    conversationId,
                    ct))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        // =================================================
        // TYPING THROTTLE
        // =================================================

        var key = (actor, conversationId);

        if (started)
        {
            var now = Now;

            var previous = LastTyping.GetOrAdd(
                key,
                DateTime.MinValue);

            // Limit repeated TypingStarted events
            // to approximately once per second.
            if (now - previous < TimeSpan.FromSeconds(1))
            {
                return true;
            }

            LastTyping[key] = now;
        }
        else
        {
            // Stop typing and clear throttle state.
            LastTyping.TryRemove(key, out _);
        }

        // =================================================
        // GET RECIPIENTS
        // =================================================

        List<Guid> recipients;

        if (type == "Group")
        {
            recipients =
                await repository.GetGroupRecipientsAsync(
                    conversationId,
                    actor,
                    ct);
        }
        else
        {
            recipients =
                await repository.GetPrivateRecipientsAsync(
                    conversationId,
                    actor,
                    ct);
        }

        // =================================================
        // BROADCAST TYPING EVENT
        // =================================================

        foreach (var recipient in recipients)
        {
            // =============================================
            // BLOCK CHECK
            // =============================================

            var blocked =
                await repository.IsBlockedBetweenAsync(
                    actor,
                    recipient,
                    ct);

            if (blocked)
                continue;

            // =============================================
            // RECIPIENT AUTHORIZATION
            // =============================================

            bool allowed;

            if (type == "Group")
            {
                allowed =
                    await groups.CanSendAsync(
                        recipient,
                        conversationId,
                        ct);
            }
            else
            {
                allowed =
                    await privateChats.CanReadAsync(
                        recipient,
                        conversationId,
                        ct);

                if (!allowed)
                {
                    allowed =
                        await privateChats.CanReadRequestAsync(
                            recipient,
                            conversationId,
                            ct);
                }
            }

            if (!allowed)
                continue;

            // =============================================
            // SIGNALR EVENT
            // =============================================

            var now = Now;

            var payload = new
            {
                eventId = Guid.NewGuid(),
                conversationId,
                actorId = actor,
                isTyping = started,
                expiresAt = started
                    ? now.AddSeconds(5)
                    : now
            };

            try
            {
                await publisher.SendToUserAsync(
                    recipient.ToString(),
                    "TypingChanged",
                    payload,
                    ct);
            }
            catch (Exception error)
            {
                logger.LogWarning(
                    error,
                    "Typing event delivery failed for {RecipientId}",
                    recipient);
            }
        }

        return true;
    }

    // =====================================================
    // BROADCAST PRESENCE
    // =====================================================

    private async Task BroadcastPresenceAsync(
        Guid actor,
        CancellationToken ct)
    {
        // =================================================
        // GET POTENTIAL VIEWERS
        // =================================================

        // Friends and users with accepted private
        // chat access may be eligible viewers.

        var viewers =
            await repository.GetPresenceViewerIdsAsync(
                actor,
                ct);

        foreach (var viewer in viewers)
        {
            // =============================================
            // PRIVACY-AWARE PERMISSION CHECK
            // =============================================

            var visible =
                await GetAsync(
                    viewer,
                    actor,
                    ct);

            // Do not expose presence if permission
            // checks fail.
            if (visible is null)
                continue;

            // Both privacy options are disabled.
            if (visible.Online is null &&
                visible.LastSeenAt is null)
            {
                continue;
            }

            // =============================================
            // SIGNALR
            // =============================================

            try
            {
                await publisher.SendToUserAsync(
                    viewer.ToString(),
                    "PresenceChanged",
                    visible,
                    ct);
            }
            catch (Exception error)
            {
                logger.LogWarning(
                    error,
                    "Presence event delivery failed for {RecipientId}",
                    viewer);
            }
        }
    }
}
