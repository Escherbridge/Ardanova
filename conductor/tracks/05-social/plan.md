# Implementation Plan - Social & Real-time

## 1. Post Management & News Feed
- [ ] **[P0] Chat System**:
    - [ ] **Conversations**:
        - [ ] Direct (1:1) and Group (Project/Guild) support.
        - [ ] Participant management (`AddMember`, `Leave`).
    - [ ] **Messages**:
        - [ ] CRUD with `senderId`.
        - [ ] Typing indicators (`TypingIndicatorDto`).
        - [ ] Read receipts (`MarkAsRead`).
    - [ ] **Real-time**: SignalR events for `ReceiveMessage`, `UserTyping`.
- [ ] **[P0] Post CRUD**: Create/Update/Delete posts with title, content, and metadata.
- [ ] **[P0] Post Types**: Implement `POST`, `PROJECT_UPDATE`, `GUILD_ACTIVITY`, `TASK_COMPLETED`, `MILESTONE`, `PROPOSAL`.
- [ ] **[P0] Visibility Rules**: Implement `PUBLIC`, `PROJECT_MEMBERS`, `GUILD_MEMBERS`, `PRIVATE` scopes.
- [ ] **[P0] Media Attachments**: Support `IMAGE`, `VIDEO`, `AUDIO`, `DOCUMENT`, `ARCHIVE` via S3/Local.
- [ ] **[P1] Interactions**: Implement Likes, Comments (Threaded), Bookmarks, and Shares (to Project/Guild).
- [ ] **[P1] Counters**: Track `likesCount`, `commentsCount`, `sharesCount`, `viewsCount` with atomic increments.
- [ ] **[P1] Trending Logic**: Calculate `trendingScore` based on engagement; `isTrending` and `isPinned` flags.
- [ ] **[P2] Feed Generation**: Aggregated feed from followed Users, Projects, and Guilds (Chronological/Trending).

## 2. Social Graph (Follow System)
- [ ] **[P0] User Follow**: `UserFollow` entity (Follow/Unfollow), prevent self-follow, double-follow.
- [ ] **[P0] Project Follow**: `ProjectFollow` entity, with notification preferences (`notifyUpdates`, `notifyMilestones`, `notifyProposals`).
- [ ] **[P0] Guild Follow**: `GuildFollow` entity, with notification preferences (`notifyUpdates`, `notifyEvents`, `notifyProjects`).
- [ ] **[P1] Follow Counters**: Maintain `followersCount` and `followingCount` on all entities.
- [ ] **[P1] Notifications**: Trigger `USER_FOLLOWED`, `PROJECT_FOLLOWED`, `GUILD_FOLLOWED` notifications.
- [ ] **[P2] Edge Case Handling**: Cascading deletes (user/project deletion removes follows), Block logic.

## 3. Operations & Chat (Conversations)
- [ ] **[P0] Conversation CRUD**: Create `DIRECT` and `GROUP` conversations.
- [ ] **[P0] Member Management**: Add/Remove members, Roles (`OWNER`, `ADMIN`, `MEMBER`), `joinedAt` tracking.
- [ ] **[P0] Message Lifecycle**: Send (`SENT`), Deliver (`DELIVERED`), Read (`READ`), Fail (`FAILED`).
- [ ] **[P1] Message Content**: Support text, attachments, replies (`replyToId`), and edits (`editedAt`).
- [ ] **[P1] Reactions**: JSON-based `ChatReaction` storage (emoji, user).
- [ ] **[P1] Unread Management**: Track `lastReadAt` per member, calculate unread badges.
- [ ] **[P2] Real-time Features**: Typing indicators (broadcast via socket), Online presence.

## 4. Real-time Infrastructure
- [ ] **[P0] Socket/SignalR Setup**: Hubs for `FeedHub`, `ChatHub`, `NotificationHub`.
- [ ] **[P1] Event Broadcasting**: Fan-out events to room/group members (efficiently).
- [ ] **[P2] Connection Management**: Handle disconnects, reconnects, and presence state sync.
