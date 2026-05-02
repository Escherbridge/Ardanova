# Track 12 — Dashboard & Feed Integration

## Phase 1: Backend — PostService + PostsController

- [ ] **[P0] Create PostDtos.cs**
    - `api-server/src/ArdaNova.Application/DTOs/PostDtos.cs`
    - Records: `PostDto`, `CreatePostDto`, `UpdatePostDto`, `PostCommentDto`, `CreateCommentDto`, `PostLikeDto`, `PostBookmarkDto`, `PostShareDto`, `CreateShareDto`
    - `PostDto` must include joined author fields (`AuthorName`, `AuthorAvatar`, `AuthorRole`), joined entity fields (`ProjectName`, `ProjectSlug`, `GuildName`, `GuildSlug`), and computed booleans (`IsLikedByCurrentUser`, `IsBookmarkedByCurrentUser`)
    - `FeedQueryParams` record: `Page`, `PageSize`, `Type?` (PostType?), `TimeRange?` (string), `RequestingUserId?`

- [ ] **[P0] Create IPostService interface**
    - `api-server/src/ArdaNova.Application/Services/Interfaces/IPostService.cs`
    - Methods:
      - `GetFeedAsync(FeedQueryParams params, CancellationToken ct)` → `Result<PagedResult<PostDto>>`
      - `GetByIdAsync(string id, string? requestingUserId, CancellationToken ct)` → `Result<PostDto>`
      - `GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken ct)` → `Result<PagedResult<PostDto>>`
      - `GetTrendingAsync(int limit, CancellationToken ct)` → `Result<IReadOnlyList<PostDto>>`
      - `CreateAsync(CreatePostDto dto, CancellationToken ct)` → `Result<PostDto>`
      - `UpdateAsync(string id, UpdatePostDto dto, CancellationToken ct)` → `Result<PostDto>`
      - `DeleteAsync(string id, CancellationToken ct)` → `Result<bool>`
      - `LikeAsync(string postId, string userId, CancellationToken ct)` → `Result<bool>`
      - `UnlikeAsync(string postId, string userId, CancellationToken ct)` → `Result<bool>`
      - `IsLikedAsync(string postId, string userId, CancellationToken ct)` → `Result<bool>`
      - `BookmarkAsync(string postId, string userId, CancellationToken ct)` → `Result<bool>`
      - `RemoveBookmarkAsync(string postId, string userId, CancellationToken ct)` → `Result<bool>`
      - `IsBookmarkedAsync(string postId, string userId, CancellationToken ct)` → `Result<bool>`
      - `ShareAsync(string postId, string userId, CreateShareDto dto, CancellationToken ct)` → `Result<bool>`
      - `GetCommentsAsync(string postId, CancellationToken ct)` → `Result<IReadOnlyList<PostCommentDto>>`
      - `AddCommentAsync(CreateCommentDto dto, CancellationToken ct)` → `Result<PostCommentDto>`

- [ ] **[P0] Create PostService implementation**
    - `api-server/src/ArdaNova.Application/Services/Implementations/PostService.cs`
    - Inject `IRepository<Post>`, `IRepository<PostLike>`, `IRepository<PostBookmark>`, `IRepository<PostShare>`, `IRepository<PostComment>`, `IUnitOfWork`, `IMapper`
    - `GetFeedAsync`: query `Post` where `visibility = PUBLIC`, order by `createdAt DESC`, paginate; set `IsLikedByCurrentUser` and `IsBookmarkedByCurrentUser` by checking PostLike/PostBookmark tables for the requesting user
    - `LikeAsync`: insert `PostLike`, increment `Post.likesCount`, save
    - `UnlikeAsync`: delete `PostLike`, decrement `Post.likesCount`, save
    - `BookmarkAsync`: insert `PostBookmark`, save
    - `RemoveBookmarkAsync`: delete `PostBookmark`, save
    - `ShareAsync`: insert `PostShare`, increment `Post.sharesCount`, save
    - Author/entity fields in `PostDto`: use `Include()` on navigation properties (`Author`, `Project`, `Guild`) or manual join
    - Follow pattern from `ActivityService` and `NotificationService` for structure

- [ ] **[P0] Register PostService in DI**
    - `api-server/src/ArdaNova.Infrastructure/DependencyInjection.cs`
    - Add `services.AddScoped<IPostService, PostService>();`
    - Verify `IRepository<Post>` etc. are covered by existing generic registration (check existing pattern)

- [ ] **[P0] Create PostsController**
    - `api-server/src/ArdaNova.API/Controllers/PostsController.cs`
    - Follow `ActivitiesController` pattern (inject service, call service, `ToActionResult()`)
    - Endpoints:
      - `GET /api/posts/feed` → `_postService.GetFeedAsync()`
      - `GET /api/posts/{id}` → `_postService.GetByIdAsync()`
      - `GET /api/posts/user/{userId}` → `_postService.GetByUserIdAsync()`
      - `GET /api/posts/trending` → `_postService.GetTrendingAsync()`
      - `POST /api/posts` → `_postService.CreateAsync()`
      - `PUT /api/posts/{id}` → `_postService.UpdateAsync()`
      - `DELETE /api/posts/{id}` → `_postService.DeleteAsync()`
      - `POST /api/posts/{id}/likes` → `_postService.LikeAsync()`
      - `DELETE /api/posts/{id}/likes` → `_postService.UnlikeAsync()`
      - `GET /api/posts/{id}/likes/check` → `_postService.IsLikedAsync()`
      - `POST /api/posts/{id}/bookmarks` → `_postService.BookmarkAsync()`
      - `DELETE /api/posts/{id}/bookmarks` → `_postService.RemoveBookmarkAsync()`
      - `GET /api/posts/{id}/bookmarks/check` → `_postService.IsBookmarkedAsync()`
      - `POST /api/posts/{id}/shares` → `_postService.ShareAsync()`
      - `GET /api/posts/{id}/comments` → `_postService.GetCommentsAsync()`
      - `POST /api/posts/{id}/comments` → `_postService.AddCommentAsync()`

- [ ] **[P0] Add AutoMapper profile for Post entities**
    - Locate existing AutoMapper profile(s) in the Application project
    - Add mappings: `Post` → `PostDto`, `PostComment` → `PostCommentDto`
    - For joined fields (`AuthorName`, `ProjectName`, etc.) use `AfterMap` or `MapFrom` with `.Include()`-loaded navigation properties

- [ ] **[P0] dotnet build verification**
    - Run `dotnet build` from `ardanova-backend-api-mcp/api-server/`
    - Must compile with zero errors before proceeding to frontend

---

## Phase 2: API Client — posts.ts Endpoint Wrapper

- [ ] **[P0] Create posts.ts endpoint file**
    - `ardanova-client/src/lib/api/ardanova/endpoints/posts.ts`
    - Export TypeScript interfaces matching `PostDto` shape:
      - `Post`, `PostComment`, `PostLike`, `PostBookmark`
      - `CreatePostDto`, `CreateCommentDto`, `CreateShareDto`
      - `FeedQueryParams` (page, pageSize, type?, timeRange?)
    - Export `PostsEndpoint` class:
      ```typescript
      export class PostsEndpoint {
        constructor(private client: BaseApiClient) {}
        getFeed(params: FeedQueryParams): Promise<ApiResponse<PagedResult<Post>>>
        getById(id: string, requestingUserId?: string): Promise<ApiResponse<Post>>
        getByUserId(userId: string, page: number, pageSize: number): Promise<ApiResponse<PagedResult<Post>>>
        getTrending(limit?: number): Promise<ApiResponse<Post[]>>
        create(dto: CreatePostDto): Promise<ApiResponse<Post>>
        update(id: string, dto: Partial<CreatePostDto>): Promise<ApiResponse<Post>>
        delete(id: string): Promise<ApiResponse<void>>
        like(postId: string, userId: string): Promise<ApiResponse<boolean>>
        unlike(postId: string, userId: string): Promise<ApiResponse<void>>
        isLiked(postId: string, userId: string): Promise<ApiResponse<boolean>>
        bookmark(postId: string, userId: string): Promise<ApiResponse<boolean>>
        removeBookmark(postId: string, userId: string): Promise<ApiResponse<void>>
        isBookmarked(postId: string, userId: string): Promise<ApiResponse<boolean>>
        share(postId: string, userId: string, dto: CreateShareDto): Promise<ApiResponse<boolean>>
        getComments(postId: string): Promise<ApiResponse<PostComment[]>>
        addComment(dto: CreateCommentDto): Promise<ApiResponse<PostComment>>
      }
      ```

- [ ] **[P0] Register PostsEndpoint in ArdaNovaApiClient**
    - `ardanova-client/src/lib/api/ardanova/index.ts`
    - Import `PostsEndpoint` from `./endpoints/posts`
    - Add `posts: PostsEndpoint` property to `ArdaNovaApiClient` class
    - Initialize in constructor: `this.posts = new PostsEndpoint(this);`
    - Add re-export of Post types in the `export type { ... }` block

---

## Phase 3: tRPC Router — Replace post.ts Stub

- [ ] **[P0] Rewrite post.ts router**
    - `ardanova-client/src/server/api/routers/post.ts`
    - Delete `hello` and `getSecretMessage` stubs
    - Add procedures (follow the `user.ts` / `project.ts` pattern):
      - `getFeed` — `protectedProcedure`, input: `{ page, pageSize, type?, timeRange? }`, calls `apiClient.posts.getFeed()`; returns paginated result with `nextCursor`
      - `getById` — `publicProcedure`, input: `{ id }`, optionally passes `ctx.session?.user.id` as `requestingUserId`
      - `getTrending` — `publicProcedure`, input: `{ limit? }`, calls `apiClient.posts.getTrending()`
      - `create` — `protectedProcedure`, input: zod schema for `CreatePostDto`, injects `authorId` from `ctx.session.user.id`, calls `apiClient.posts.create()`
      - `likePost` — `protectedProcedure`, input: `{ postId }`, calls `apiClient.posts.like()`
      - `unlikePost` — `protectedProcedure`, input: `{ postId }`, calls `apiClient.posts.unlike()`
      - `bookmarkPost` — `protectedProcedure`, input: `{ postId }`, calls `apiClient.posts.bookmark()`
      - `removeBookmark` — `protectedProcedure`, input: `{ postId }`, calls `apiClient.posts.removeBookmark()`
      - `sharePost` — `protectedProcedure`, input: `{ postId, comment?, sharedToProjectId?, sharedToGuildId? }`, calls `apiClient.posts.share()`
      - `getComments` — `publicProcedure`, input: `{ postId }`, calls `apiClient.posts.getComments()`
      - `addComment` — `protectedProcedure`, input: `{ postId, content, parentId? }`, injects `authorId` from session, calls `apiClient.posts.addComment()`
    - Note: `post` is already registered on `appRouter` in `root.ts` — no change needed there

---

## Phase 4: Dashboard — Wire Feed to Real Data

- [ ] **[P0] Replace feed data source with tRPC infinite query**
    - `ardanova-client/src/app/dashboard/page.tsx`
    - Remove `sampleFeedItems` constant (lines 35–150)
    - Add: `const { data, fetchNextPage, hasNextPage, isFetchingNextPage } = api.post.getFeed.useInfiniteQuery({ pageSize: 20 }, { getNextPageParam: (last) => last.nextCursor })`
    - Flatten pages to a single `feedItems` array: `const feedItems = data?.pages.flatMap(p => p.items) ?? []`
    - Apply local filter logic (search, type, timeRange) to `feedItems` — keep existing filter UI as-is, filtering client-side on already-fetched data (MVP approach; server-side filtering is a follow-up)
    - Wire `hasMore` prop on `<Feed>` to `!!hasNextPage`
    - Wire `onLoadMore` to `() => void fetchNextPage()`

- [ ] **[P0] Add mapper from PostDto to FeedCardProps**
    - Add a utility function `mapPostToFeedCard(post: Post): FeedCardProps` in the dashboard file or a shared utility
    - Maps PostType enum string (uppercase) to `FeedItemType` (lowercase slugs)
    - Maps author, entity, content, engagement, timestamps, isLiked, isBookmarked
    - Apply in the `feedItems` flatten: `data?.pages.flatMap(p => p.items.map(mapPostToFeedCard)) ?? []`

- [ ] **[P0] Wire ComposeBox to post creation mutation**
    - Add: `const createPost = api.post.create.useMutation({ onSuccess: () => void refetch() })`
    - Replace `handlePostSubmit` implementation: call `createPost.mutate({ content: post.text, type: "POST", visibility: "PUBLIC", projectId: post.scope.id, ... })`
    - Remove local state optimistic update (replace with `refetch` or optimistic update via `onMutate`)

- [ ] **[P1] Replace hardcoded ComposeBox scopes with real data**
    - Add: `const { data: myProjects } = api.project.getMyProjects.useQuery()` (add this procedure if missing)
    - Add: `const { data: myGuilds } = api.guild.getMyGuilds.useQuery()` (add this procedure if missing)
    - Replace hardcoded `scopes` prop array with mapped real data

---

## Phase 5: ComposeBox — Wire Social Actions on FeedCard

- [ ] **[P0] Add mutation callbacks to FeedCard props**
    - `ardanova-client/src/components/feed/feed-card.tsx`
    - Add optional props: `onLike?`, `onUnlike?`, `onBookmark?`, `onRemoveBookmark?`, `onShare?`
    - Wire Heart button click: if `isLiked` call `onUnlike(id)`, else call `onLike(id)`
    - Wire Bookmark button click: if `isBookmarked` call `onRemoveBookmark(id)`, else call `onBookmark(id)`
    - Wire Share button click: call `onShare(id)`
    - Add local optimistic toggle state for instant UI feedback (flip `isLiked`/`isBookmarked` immediately, revert on error)

- [ ] **[P0] Pass mutation handlers from dashboard to Feed/FeedCard**
    - In `dashboard/page.tsx`, define mutations:
      - `const likePost = api.post.likePost.useMutation()`
      - `const unlikePost = api.post.unlikePost.useMutation()`
      - `const bookmarkPost = api.post.bookmarkPost.useMutation()`
      - `const removeBookmark = api.post.removeBookmark.useMutation()`
      - `const sharePost = api.post.sharePost.useMutation()`
    - Pass handler functions to `<Feed>` and thread down to `<FeedCard>`
    - `<Feed>` component may need to accept and forward these handlers — check `feed.tsx` and update props if needed

---

## Phase 6: Trending Projects Sidebar — Wire to Real Data

- [ ] **[P0] Replace hardcoded trendingProjects with real query**
    - `ardanova-client/src/app/dashboard/page.tsx`
    - Remove `trendingProjects` constant (lines 153–175)
    - Add: `const { data: trendingProjectsData } = api.post.getTrending.useQuery({ limit: 3 })`
      - OR: check if `api.project.getAll` already supports `isTrending` filter; use `api.project.getAll.useQuery({ limit: 3 })` sorted by `supportersCount` as a proxy for trending if a dedicated trending endpoint is not worth adding separately
    - Map result to the sidebar display format: `name`, `category` (from tags or categories), `coOwners` (from `supportersCount`), `progress` (derived from `currentFunding / fundingGoal * 100`)

---

## Phase 7: "Who to Follow" Sidebar — Wire to Real Data

- [ ] **[P1] Add getSuggestedToFollow on UsersController (backend)**
    - `api-server/src/ArdaNova.API/Controllers/UsersController.cs`
    - New endpoint: `GET /api/users/{userId}/suggested-follows?limit=5`
    - Implementation in `UserService` or `UserFollowService`:
      - Return users where there is no existing follow relationship with `userId`
      - Order by `totalXP DESC` or `createdAt DESC` as a simple ranking signal
      - Limit to `limit` results

- [ ] **[P1] Add getSuggestedToFollow to users.ts endpoint**
    - `ardanova-client/src/lib/api/ardanova/endpoints/users.ts`
    - Add method: `getSuggestedToFollow(userId: string, limit?: number): Promise<ApiResponse<User[]>>`

- [ ] **[P1] Add getSuggestedToFollow tRPC procedure**
    - `ardanova-client/src/server/api/routers/user.ts`
    - Add `getSuggestedToFollow` protected procedure, calls `apiClient.users.getSuggestedToFollow(ctx.session.user.id, input.limit)`

- [ ] **[P1] Wire "Who to Follow" sidebar to real data**
    - `ardanova-client/src/app/dashboard/page.tsx`
    - Remove `suggestedUsers` constant (lines 178–197)
    - Add: `const { data: suggestedUsersData } = api.user.getSuggestedToFollow.useQuery({ limit: 3 })`
    - Map result to sidebar display format: `name`, `avatar` (from `image`), `bio`

- [ ] **[P1] Wire Follow buttons to follow mutation**
    - In dashboard sidebar "Who to Follow" section
    - Track which users have been followed in local state: `const [followedIds, setFollowedIds] = useState<Set<string>>(new Set())`
    - Wire each Follow button: `api.user.follow.useMutation()`, toggle button to "Following" on success

---

## Phase 8: Verification

- [ ] **[P0] dotnet build — backend compiles clean**
    - Run: `dotnet build` from `ardanova-backend-api-mcp/api-server/`
    - Expected: 0 errors

- [ ] **[P0] npm run build — Next.js compiles clean**
    - Run: `npm run build` from `ardanova-client/`
    - Expected: 0 TypeScript errors, 0 build failures

- [ ] **[P0] Manual smoke test: feed loads**
    - Navigate to `/dashboard` as logged-in user
    - Feed must show real posts from backend (or empty state — not hardcoded fake data)
    - Pagination "load more" must trigger and append new posts

- [ ] **[P0] Manual smoke test: post creation**
    - Use ComposeBox, submit text post
    - Post appears at top of feed after submit
    - Refresh confirms post persisted in backend

- [ ] **[P0] Manual smoke test: social actions**
    - Click Like on a post — count increments, heart fills
    - Click Like again — count decrements, heart unfills
    - Click Bookmark — bookmark icon fills
    - Verify mutations reach backend (check network tab or DB directly)

- [ ] **[P1] Manual smoke test: sidebar data**
    - Trending Projects sidebar shows real project names from DB
    - "Who to Follow" shows real users not yet followed
    - Clicking Follow changes button state
