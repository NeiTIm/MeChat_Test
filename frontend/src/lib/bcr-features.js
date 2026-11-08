// Build-time UI switches. Enable only alongside the separately approved backend rollout.
// They are not an authorization boundary; backend options remain authoritative.
export const bcrFeatures = Object.freeze({
  notifications: import.meta.env.VITE_BCR_NOTIFICATION_LIFECYCLE_ENABLED === 'true',
  friendSeen: import.meta.env.VITE_BCR_FRIEND_REQUEST_SEEN_ENABLED === 'true',
})
