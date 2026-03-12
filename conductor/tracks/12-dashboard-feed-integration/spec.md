# Dashboard & Feed Integration — Technical Specification

## Overview

This track wires the dashboard at `/dashboard/page.tsx` to real backend data. Currently the page is entirely powered by hardcoded sample arrays (`sampleFeedItems`, `trendingProjects`, `suggestedUsers`). The `post.ts` tRPC router is the original T3 scaffold stub with only `hello` and `getSecretMessage` — it does nothing. This track replaces all of that with a real data pipeline following the established architecture: backend PostService + PostsController → API client endpoint → tRPC thin proxy → React component.

## What Is Currently Broken

| Problem | Location |
|---------|----------|
| Feed shows hardcoded fake posts | `ardanova-client/src/app/dashboard/page.tsx` lines 35–150 |
| `sampleFeedItems` array, 5 fake posts | `dashboard/page.tsx` |
| `trendingProjects` hardcoded | `dashboard/page.tsx` lines 153–175 |
| `suggestedUsers` hardcoded | `dashboard/page.tsx` lines 178–197 |
| `handlePostSubmit` adds post to local state only, never persists | `dashboard/page.tsx` lines 273–292 |
| `ComposeBox.scopes` is hardcoded to fake project/guild IDs | `dashboard/page.tsx` lines 612–615 |
| Follow buttons on "Who to Follow" do nothing | `dashboard/page.tsx` lines 416 |
| Like/bookmark/share on FeedCard are UI-only state | `feed-card.tsx` |
| `onLoadMore` is a no-op | `dashboard/page.tsx` line 621 |
| `post.ts` router has zero real procedures | `ardanova-client/src/server/api/routers/post.ts` |

## Backend Audit

### What Exists

**Domain entities (fully defined):**
- `Post` — `id, authorId, projectId?, guildId?, type (PostType enum), visibility (PostVisibility enum), title?, content, metadata (jsonb), likesCount, commentsCount, sharesCount, viewsCount, isPinned, isTrending, trendingScore, trendingRank?, trendingAt?, createdAt, updatedAt`
- `PostComment` — `id, postId, authorId, parentId?, content, likesCount, createdAt, updatedAt`
- `PostLike` — `id, postId, userId, createdAt`
- `PostBookmark` — `id, postId, userId, createdAt`
- `PostShare` — `id, postId, userId, sharedToProjectId?, sharedToGuildId?, comment?, createdAt`
- `PostMedia` — exists as a navigation collection on `Post`

**Enums (defined):**
- `PostType`: `POST, PROJECT_UPDATE, GUILD_ACTIVITY, TASK_COMPLETED, MILESTONE, PROPOSAL, SHOP_ITEM`
- `PostVisibility`: `PUBLIC, PROJECT_MEMBERS, GUILD_MEMBERS, PRIVATE`

**Adjacent services (exist, usable as reference):**
- `ActivityService` / `IActivityService` — paged queries by user and project
- `NotificationService` / `INotificationService` — paged queries by user
- `UserFollowService` / `IUserFollowService` — follow/unfollow/check/counts
- `ProjectService` — trending projects can be served from existing project queries

### What Does NOT Exist (Must Be Created)

| Item | Status |
|------|--------|
| `IPostService` interface | MISSING |
| `PostService` implementation | MISSING |
| `PostsController` | MISSING — not in `ArdaNova.API/Controllers/` |
| Post DTOs (`PostDto`, `CreatePostDto`, `PostCommentDto`, etc.) | MISSING — no `PostDtos.cs` in `ArdaNova.Application/DTOs/` |
| API client endpoint `posts.ts` | MISSING — no file in `ardanova-client/src/lib/api/ardanova/endpoints/` |

## Data Flow

```
User action (dashboard)
  → tRPC procedure (post router) [thin proxy]
    → apiClient.posts.* [HTTP wrapper]
      → POST /api/posts (PostsController)
        → IPostService (dependency injected)
          → IRepository<Post> (EF Core)
            → PostgreSQL Post table
```

For social actions (like, bookmark, share):
```
FeedCard button click
  → tRPC mutation (post.likePost / post.bookmarkPost / post.sharePost)
    → apiClient.posts.like / bookmark / share
      → POST /api/posts/{id}/likes (PostsController)
        → PostService.LikeAsync() → updates PostLike table + increments Post.likesCount
```

For trending projects (sidebar):
```
Dashboard load
  → tRPC query (project.getTrending)
    → apiClient.projects.getTrending (if missing: add to existing projects endpoint)
      → GET /api/projects?isTrending=true&limit=5 (existing ProjectsController)
```

For "Who to Follow" (sidebar):
```
Dashboard load
  → tRPC query (user.getSuggestedToFollow)
    → apiClient.users.getSuggestedToFollow (new method in users endpoint)
      → GET /api/users/{userId}/suggested-follows (new endpoint on UsersController)
        → UserFollowService — returns users not yet followed, ranked by activity
```

## API Endpoints to Create

### PostsController routes

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/posts/feed?page=1&pageSize=20&type=&timeRange=` | Paginated feed for current user (follows + public) |
| GET | `/api/posts/{id}` | Single post by ID |
| POST | `/api/posts` | Create a new post |
| PUT | `/api/posts/{id}` | Update a post (author only) |
| DELETE | `/api/posts/{id}` | Delete a post (author only) |
| POST | `/api/posts/{id}/likes` | Like a post |
| DELETE | `/api/posts/{id}/likes` | Unlike a post |
| GET | `/api/posts/{id}/likes/check?userId=` | Check if user liked |
| POST | `/api/posts/{id}/bookmarks` | Bookmark a post |
| DELETE | `/api/posts/{id}/bookmarks` | Remove bookmark |
| GET | `/api/posts/{id}/bookmarks/check?userId=` | Check if user bookmarked |
| POST | `/api/posts/{id}/shares` | Share a post |
| GET | `/api/posts/{id}/comments` | Get comments for a post |
| POST | `/api/posts/{id}/comments` | Add a comment |
| GET | `/api/posts/trending?limit=10` | Trending posts by trendingScore |
| GET | `/api/posts/user/{userId}?page=1&pageSize=20` | Posts by a specific user |

### UsersController additions (if needed for "Who to Follow")

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users/{userId}/suggested-follows?limit=5` | Users not yet followed, ranked by connections |

## Frontend Changes Required

### `post.ts` tRPC router

Replace the stub with real procedures:
- `getFeed` — protected query, paginated, calls `apiClient.posts.getFeed()`
- `create` — protected mutation, calls `apiClient.posts.create()`
- `likePost` — protected mutation
- `unlikePost` — protected mutation
- `bookmarkPost` — protected mutation
- `removeBookmark` — protected mutation
- `sharePost` — protected mutation
- `getComments` — query by postId
- `addComment` — protected mutation
- `getTrending` — public query (or reuse `project.getTrending` if added there)

### `ardanova-client/src/lib/api/ardanova/endpoints/posts.ts`

New file. Defines `PostsEndpoint` class extending `BaseApiClient` wrapping all HTTP calls. Exports types: `Post, PostComment, PostLike, PostBookmark, PostShare, CreatePostDto, CreateCommentDto, FeedQueryParams`.

### `ardanova-client/src/lib/api/ardanova/index.ts`

Add `PostsEndpoint` import, register as `posts` on `ArdaNovaApiClient`, export Post types.

### `ardanova-client/src/app/dashboard/page.tsx`

- Remove `sampleFeedItems`, `trendingProjects`, `suggestedUsers` constants
- Replace `const [feedItems, setFeedItems] = useState(sampleFeedItems)` with `api.post.getFeed.useInfiniteQuery()`
- Replace `handlePostSubmit` local-only state mutation with `api.post.create.useMutation()`
- Replace hardcoded `ComposeBox.scopes` with data from `api.project.getMyProjects` and `api.guild.getMyGuilds`
- Replace trending projects sidebar with `api.project.getTrending.useQuery()`
- Replace suggested users sidebar with `api.user.getSuggestedToFollow.useQuery()`
- Wire `onLoadMore` to `fetchNextPage` from `useInfiniteQuery`
- Wire Follow buttons to `api.user.follow.useMutation()`

### `feed-card.tsx` social actions

Currently `isLiked` and `isBookmarked` are read from props but no mutations fire. Must add:
- `onLike?: (postId: string, currentlyLiked: boolean) => void`
- `onBookmark?: (postId: string, currentlyBookmarked: boolean) => void`
- `onShare?: (postId: string) => void`

These callbacks are passed down from dashboard and backed by tRPC mutations.

## DTO Shape (Backend)

```csharp
// PostDtos.cs
public record PostDto {
    public string Id { get; init; }
    public string AuthorId { get; init; }
    public string AuthorName { get; init; }      // joined from User
    public string? AuthorAvatar { get; init; }   // joined from User
    public string AuthorRole { get; init; }      // joined from User (for badge)
    public string? ProjectId { get; init; }
    public string? ProjectName { get; init; }    // joined from Project
    public string? ProjectSlug { get; init; }    // joined from Project
    public string? GuildId { get; init; }
    public string? GuildName { get; init; }      // joined from Guild
    public string? GuildSlug { get; init; }      // joined from Guild
    public PostType Type { get; init; }
    public PostVisibility Visibility { get; init; }
    public string? Title { get; init; }
    public string Content { get; init; }
    public string? Metadata { get; init; }       // JSON string
    public int LikesCount { get; init; }
    public int CommentsCount { get; init; }
    public int SharesCount { get; init; }
    public int ViewsCount { get; init; }
    public bool IsPinned { get; init; }
    public bool IsTrending { get; init; }
    public bool IsLikedByCurrentUser { get; init; }    // computed at query time
    public bool IsBookmarkedByCurrentUser { get; init; } // computed at query time
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreatePostDto {
    public required string AuthorId { get; init; }
    public string? ProjectId { get; init; }
    public string? GuildId { get; init; }
    public required PostType Type { get; init; }
    public required PostVisibility Visibility { get; init; }
    public string? Title { get; init; }
    public required string Content { get; init; }
    public string? Metadata { get; init; }
}

public record PostCommentDto {
    public string Id { get; init; }
    public string PostId { get; init; }
    public string AuthorId { get; init; }
    public string AuthorName { get; init; }
    public string? AuthorAvatar { get; init; }
    public string? ParentId { get; init; }
    public string Content { get; init; }
    public int LikesCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateCommentDto {
    public required string PostId { get; init; }
    public required string AuthorId { get; init; }
    public string? ParentId { get; init; }
    public required string Content { get; init; }
}
```

## Mapping from `PostDto` → `FeedCardProps`

The frontend `FeedCardProps` type (in `feed-card.tsx`) uses a different shape than the raw `PostDto`. The tRPC router or a mapper utility must translate:

```
PostDto.Type (PostType enum string)  → FeedCardProps.type (FeedItemType lowercase)
PostDto.AuthorId/Name/Avatar/Role    → FeedCardProps.author
PostDto.ProjectId/Name/Slug          → FeedCardProps.entity (type: "project")
PostDto.GuildId/Name/Slug            → FeedCardProps.entity (type: "guild")
PostDto.Title + Content              → FeedCardProps.content.title + .text
PostDto.LikesCount/CommentsCount/... → FeedCardProps.engagement
PostDto.IsLikedByCurrentUser         → FeedCardProps.isLiked
PostDto.IsBookmarkedByCurrentUser    → FeedCardProps.isBookmarked
PostDto.CreatedAt                    → FeedCardProps.timestamp (Date)
```

The PostType-to-FeedItemType mapping (enum case to lowercase slug):
- `POST` → `"post"`
- `PROJECT_UPDATE` → `"project_update"`
- `GUILD_ACTIVITY` → `"guild_activity"`
- `TASK_COMPLETED` → `"task_completed"`
- `MILESTONE` → `"milestone"`
- `PROPOSAL` → `"proposal"`

## Feed Query Logic (Backend)

The `GetFeedAsync` service method should return posts visible to the requesting user, ordered by `createdAt DESC`, with cursor-based pagination. Feed composition:
1. Posts by users the current user follows (where `Post.visibility = PUBLIC` or shared to a mutual project/guild)
2. Posts in projects the user is a member of
3. Posts in guilds the user belongs to
4. Trending public posts (fallback fill if feed is sparse)

For MVP simplicity: return all `PUBLIC` posts ordered by `createdAt DESC`, paged. Personal feed filtering can be a follow-up.

## Infrastructure Registration

After creating `IPostService` / `PostService`:
- Register in `ArdaNova.Infrastructure/DependencyInjection.cs` following the existing pattern:
  ```csharp
  services.AddScoped<IPostService, PostService>();
  ```
- Register repositories for `Post`, `PostComment`, `PostLike`, `PostBookmark`, `PostShare` if not already in the generic repository registration.

## Out of Scope for This Track

- Real-time feed updates via SignalR (deferred to Track 14 or later)
- Post media/attachment upload (deferred; `PostMedia` table exists but upload flow is separate)
- Full-text search across posts (deferred to Track 18 Search & Discovery)
- Notification generation on like/comment (NotificationService exists but wiring it from PostService is deferred)
- Feed algorithm / personalization beyond basic chronological + trending fallback
