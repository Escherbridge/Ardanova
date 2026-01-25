# ArdaNova Platform Architecture

This document provides a detailed technical architecture for the ArdaNova platform, covering the repository structure, service boundaries, data flow, and integration patterns.

---

## Recent Updates (January 2025)

### Architecture Enhancements ✅

| Component | Status | Description |
|-----------|--------|-------------|
| `ardanova-client` | ✅ Active | Main Next.js app (all features consolidated) |
| `ardanova-backend-api-mcp` | ✅ Complete | .NET 8 API + MCP Server (40+ tools) |
| `Event Bus` | ✅ Complete | In-memory event bus with domain events |
| `WebSocket/SignalR` | ✅ Complete | Real-time updates via SignalR hubs |
| `File Storage` | ✅ Complete | S3/Local storage with env var binding |
| `Dev Scripts` | ✅ Complete | Bash & PowerShell scripts (Docker/Podman) |
| `C# Generator` | ✅ Complete | Attribute-based EF Core configuration |
| `ardanova-ai-client` | 🔄 Stubbed | Python AI orchestrator (structure ready) |
| `ardanova-game-sdk` | 🔄 TODO | Unity & Godot SDKs (NuGet) |
| `contracts` | 🔄 Stubbed | Algorand smart contracts (structure ready) |

**Key Changes:**
- DAO, Studio, Exchange, Explorer, and Agent UI consolidated into main platform
- Event Bus implemented for domain event publishing
- SignalR WebSocket hub for real-time client updates
- S3-compatible file storage service with presigned URLs
- Development scripts for Docker/Podman (hot-reload support)
- AI client structure with MCP client stubs
- Smart contract directory structure with PyTeal stubs
- EF Core relationship fixes for multi-FK entities
- **C# Generator enhanced with attribute-based EF Core configuration**
- DbContext simplified from 682 lines to ~120 lines (OnModelCreating: 570+ → 9 lines)

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Repository Structure](#repository-structure)
3. [Development Scripts](#development-scripts)
4. [Application Architecture](#application-architecture)
5. [Event-Driven Architecture](#event-driven-architecture)
6. [Real-Time Communication](#real-time-communication)
7. [File Storage](#file-storage)
8. [Data Flow](#data-flow)
9. [Database Design](#database-design)
10. [Code Generation & EF Core](#code-generation--ef-core)
11. [API Layer](#api-layer)
12. [Authentication & Authorization](#authentication--authorization)
13. [AI & MCP Integration](#ai--mcp-integration)
14. [Blockchain Integration](#blockchain-integration)
15. [Infrastructure](#infrastructure)
16. [Security](#security)

---

## System Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT LAYER                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                     ARDANOVA-CLIENT (Next.js 15)                       │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │ │
│  │  │ Projects │ │   DAO    │ │  Studio  │ │ Exchange │ │  Agent   │    │ │
│  │  │Dashboard │ │Governance│ │  (Gamma) │ │  Tokens  │ │   Chat   │    │ │
│  │  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘    │ │
│  │       └────────────┴────────────┴─────┬──────┴────────────┘          │ │
│  │                                       │                               │ │
│  │       ┌───────────────────────────────┼───────────────────────────┐  │ │
│  │       │                               │                           │  │ │
│  │       ▼                               ▼                           ▼  │ │
│  │  ┌─────────┐                   ┌─────────────┐              ┌─────────┐│ │
│  │  │  tRPC   │                   │  SignalR    │              │ Storage ││ │
│  │  │ Client  │                   │  WebSocket  │              │  Client ││ │
│  │  └────┬────┘                   └──────┬──────┘              └────┬────┘│ │
│  └───────┼───────────────────────────────┼───────────────────────────┼────┘ │
│          │                               │                           │       │
└──────────┼───────────────────────────────┼───────────────────────────┼───────┘
           │                               │                           │
           ▼                               ▼                           ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ARDANOVA-BACKEND-API-MCP                          │
│                            (.NET 8 + MCP Server)                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │ Controllers  │  │  Event Bus   │  │  SignalR Hub │  │   Storage    │    │
│  │  (14 APIs)   │  │ (In-Memory)  │  │ (WebSocket)  │  │   Service    │    │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘    │
│         │                 │                 │                 │             │
│         └─────────────────┴─────────────────┴─────────────────┘             │
│                                    │                                         │
│                         ┌──────────▼──────────┐                             │
│                         │    Application      │                             │
│                         │     Services        │                             │
│                         │   (28+ Services)    │                             │
│                         └──────────┬──────────┘                             │
│                                    │                                         │
│                         ┌──────────▼──────────┐                             │
│                         │   Infrastructure    │                             │
│                         │ Repository Pattern  │                             │
│                         └──────────┬──────────┘                             │
│                                    │                                         │
└────────────────────────────────────┼─────────────────────────────────────────┘
                                     │
           ┌─────────────────────────┼─────────────────────────┐
           │                         │                         │
           ▼                         ▼                         ▼
    ┌──────────────┐         ┌──────────────┐         ┌──────────────┐
    │  PostgreSQL  │         │     S3       │         │   Algorand   │
    │  (Database)  │         │   Storage    │         │  Blockchain  │
    └──────────────┘         └──────────────┘         └──────────────┘
```

### Core Principles

1. **Simplicity** - Single platform with all features consolidated
2. **Type Safety** - End-to-end TypeScript with tRPC
3. **Clean Architecture** - Separation of concerns in backend
4. **Event-Driven** - Domain events for loose coupling
5. **Real-Time** - WebSocket for instant updates
6. **AI-First** - Native AI integration via MCP protocol
7. **Decentralization** - Progressive decentralization via Algorand DAOs

---

## Repository Structure

```
ardanova/
│
├── ardanova-client/                    # Main Next.js application (all features)
│   ├── src/
│   │   ├── app/                        # Next.js 15 App Router
│   │   │   ├── auth/                   # Authentication pages
│   │   │   ├── dashboard/              # User dashboard
│   │   │   ├── projects/               # Project listing & details
│   │   │   ├── marketplace/            # Task marketplace (TODO)
│   │   │   ├── dao/                    # Governance UI (TODO)
│   │   │   ├── studio/                 # Pitch builder (TODO)
│   │   │   ├── exchange/               # Token exchange (TODO)
│   │   │   ├── explorer/               # Blockchain explorer (TODO)
│   │   │   ├── agent/                  # AI agent chat (TODO)
│   │   │   └── api/                    # Next.js API routes
│   │   │
│   │   ├── components/                 # Shared UI components
│   │   ├── lib/                        # Utilities and helpers
│   │   │   ├── api/                    # API client for .NET backend
│   │   │   ├── blockchain/             # Algorand integration (TODO)
│   │   │   └── hooks/                  # Real-time WebSocket hooks
│   │   │
│   │   ├── server/                     # Server-side code
│   │   │   ├── api/routers/            # tRPC routers
│   │   │   ├── auth/                   # NextAuth configuration
│   │   │   └── db.ts                   # Prisma client
│   │   │
│   │   └── trpc/                       # tRPC client
│   │
│   ├── prisma/                         # Database schema & migrations
│   └── public/                         # Static assets
│
├── ardanova-backend-api-mcp/           # .NET 8 Backend + MCP Server
│   ├── ardanova.sln                    # Solution file (6 projects)
│   └── api-server/
│       └── src/
│           ├── ArdaNova.Domain/        # Entities, Enums (60+ entities)
│           ├── ArdaNova.Application/   # Services, DTOs (28+ services)
│           │   ├── Common/             # Result<T>, PagedResult<T>
│           │   ├── DTOs/               # 15+ DTO files
│           │   ├── Mappings/           # AutoMapper profiles
│           │   └── Services/           # Business logic
│           ├── ArdaNova.Infrastructure/# DbContext, Repositories
│           │   ├── Data/               # EF Core DbContext
│           │   ├── Repositories/       # Generic Repository
│           │   └── Storage/            # S3/Local storage ✅
│           ├── ArdaNova.API/           # Controllers, Middleware
│           │   ├── Controllers/        # 14 REST controllers ✅
│           │   ├── EventBus/           # Domain event bus ✅
│           │   ├── WebSocket/          # SignalR hub ✅
│           │   └── Middleware/         # API key, exception handling
│           └── ArdaNova.MCP/           # 40+ MCP Tools
│
├── ardanova-ai-client/                 # Python AI Orchestrator ✅ STUBBED
│   ├── src/
│   │   ├── api/                        # FastAPI endpoints
│   │   ├── orchestrator/               # Agent orchestration
│   │   ├── agents/                     # Agent definitions
│   │   ├── mcp/                        # MCP client
│   │   └── config/                     # Settings
│   ├── requirements.txt
│   └── Dockerfile
│
├── ardanova-game-sdk/                  # Game SDK (TODO)
│   ├── game-sdk-unity/                 # C# SDK for Unity
│   └── game-sdk-godot/                 # C# SDK for Godot
│
├── contracts/                          # Algorand smart contracts ✅ STUBBED
│   ├── governance/                     # DAO governance
│   ├── tokens/                         # ASA token factories
│   ├── ico/                            # ICO lifecycle
│   ├── exchange/                       # DEX/swap contracts
│   └── escrow/                         # Task payment escrow
│
├── documentation/                      # All documentation
│   ├── ARCHITECTURE.md                 # This file
│   └── ROADMAP.md                      # Development roadmap
│
├── scripts/                            # Development scripts ✅ NEW
│   ├── dev-up.sh                       # Bash: Start dev environment
│   ├── dev-down.sh                     # Bash: Tear down environment
│   ├── dev-up.ps1                      # PowerShell: Start dev environment
│   └── dev-down.ps1                    # PowerShell: Tear down environment
│
├── uploads/                            # Local file storage (dev only)
│
├── docker-compose.yml                  # Production compose
├── docker-compose.dev.yml              # Development compose (hot-reload) ✅ NEW
├── railway.toml                        # Railway deployment
├── .env.example                        # Environment template
└── README.md                           # Project overview
```

---

## Development Scripts

### Overview

Development scripts support both Docker and Podman, with automatic runtime detection.

```
scripts/
├── dev-up.sh       # Bash - Start development environment
├── dev-down.sh     # Bash - Tear down environment
├── dev-up.ps1      # PowerShell - Start development environment
└── dev-down.ps1    # PowerShell - Tear down environment
```

### Usage

**Start Development Environment (Hot-Reload):**
```bash
# Bash (Linux/Mac/Git Bash)
./scripts/dev-up.sh

# PowerShell (Windows)
.\scripts\dev-up.ps1
```

**Tear Down Environment:**
```bash
# Bash - Basic tear down
./scripts/dev-down.sh

# Bash - Remove volumes, images, and prune
./scripts/dev-down.sh -a

# PowerShell
.\scripts\dev-down.ps1 -All
```

### Script Options

| Script | Flag | Description |
|--------|------|-------------|
| `dev-up` | `-p, --prod` | Use production compose |
| `dev-up` | `-b, --build` | Force rebuild images |
| `dev-up` | `-f, --foreground` | Run in foreground |
| `dev-up` | `-s, --service` | Start specific service |
| `dev-down` | `-v, --volumes` | Remove volumes |
| `dev-down` | `-i, --images` | Remove images |
| `dev-down` | `-p, --prune` | Prune unused resources |
| `dev-down` | `-a, --all` | Remove everything |

### Docker Compose Files

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Production deployment |
| `docker-compose.dev.yml` | Development with hot-reload |

### Hot-Reload Development

The development compose file (`docker-compose.dev.yml`) enables hot-reload:

- **.NET Backend**: Uses `dotnet watch` - code changes auto-reload
- **Next.js Client**: Uses `next dev` - changes auto-reload
- **Python AI Client**: Uses `uvicorn --reload`

**No rebuild needed for code changes in development mode.**

### Container Runtime Detection

Scripts automatically detect available runtime:
1. Check for `podman` first
2. Fall back to `docker`
3. Detect compose variant (`podman-compose`, `podman compose`, `docker-compose`, `docker compose`)

---

## Event-Driven Architecture

### Event Bus Implementation

The backend uses an in-memory event bus for domain event publishing and handling.

```
┌─────────────────────────────────────────────────────────────────┐
│                      EVENT BUS ARCHITECTURE                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────┐        ┌─────────────┐        ┌─────────────┐  │
│  │  Service    │ ─────> │  Event Bus  │ ─────> │   Handler   │  │
│  │ (Publisher) │        │ (In-Memory) │        │ (Subscriber)│  │
│  └─────────────┘        └─────────────┘        └─────────────┘  │
│                                │                                 │
│                                ▼                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                     EVENT TYPES                           │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │                                                           │   │
│  │  PROJECT EVENTS:                                          │   │
│  │  • ProjectCreatedEvent                                    │   │
│  │  • ProjectUpdatedEvent                                    │   │
│  │  • ProjectStatusChangedEvent                              │   │
│  │  • ProjectDeletedEvent                                    │   │
│  │  • ProjectTaskCompletedEvent                              │   │
│  │  • ProjectMemberAddedEvent                                │   │
│  │  • ProjectMemberRemovedEvent                              │   │
│  │                                                           │   │
│  │  USER EVENTS:                                             │   │
│  │  • UserCreatedEvent                                       │   │
│  │  • UserUpdatedEvent                                       │   │
│  │  • UserVerifiedEvent                                      │   │
│  │  • UserDeletedEvent                                       │   │
│  │                                                           │   │
│  │  NOTIFICATION EVENTS:                                     │   │
│  │  • NotificationCreatedEvent                               │   │
│  │  • NotificationReadEvent                                  │   │
│  │  • NotificationsMarkedAllReadEvent                        │   │
│  │                                                           │   │
│  │  ACTIVITY EVENTS:                                         │   │
│  │  • ActivityLoggedEvent                                    │   │
│  │                                                           │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Event Bus Interfaces

```csharp
// IDomainEvent - Base interface for all events
public interface IDomainEvent
{
    Guid EventId { get; }
    string EventType { get; }
    DateTime OccurredAt { get; }
}

// IEventBus - Event pub/sub interface
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent;

    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : IDomainEvent;
}

// IEventHandler<T> - Handler interface
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
```

---

## Real-Time Communication

### SignalR WebSocket Hub

The platform uses SignalR for real-time updates with group-based message routing.

```
┌─────────────────────────────────────────────────────────────────┐
│                      SIGNALR ARCHITECTURE                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    ARDANOVA HUB                          │    │
│  │                  /hubs/ardanova                          │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │                                                          │    │
│  │  GROUP SUBSCRIPTIONS:                                    │    │
│  │  ├── user:{userId}      User-specific events            │    │
│  │  ├── project:{projectId} Project-specific events        │    │
│  │  ├── agency:{agencyId}   Agency-specific events         │    │
│  │  └── all                 Broadcast to all clients       │    │
│  │                                                          │    │
│  │  HUB METHODS (Client → Server):                          │    │
│  │  ├── SubscribeToProject(projectId)                      │    │
│  │  ├── UnsubscribeFromProject(projectId)                  │    │
│  │  ├── SubscribeToAgency(agencyId)                        │    │
│  │  ├── UnsubscribeFromAgency(agencyId)                    │    │
│  │  ├── SubscribeToUser(userId) // Own user only           │    │
│  │  └── SubscribeToAll()                                   │    │
│  │                                                          │    │
│  │  CLIENT METHODS (Server → Client):                       │    │
│  │  ├── ReceiveEvent(eventType, payload)                   │    │
│  │  ├── UserCreated(user), UserUpdated(user)               │    │
│  │  ├── ProjectCreated(project), ProjectUpdated(project)   │    │
│  │  ├── ProjectStatusChanged(data)                         │    │
│  │  ├── TaskCompleted(task)                                │    │
│  │  ├── NotificationReceived(notification)                 │    │
│  │  └── ActivityLogged(activity)                           │    │
│  │                                                          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    HUB HANDLERS                          │    │
│  │             (Event Bus → SignalR Bridge)                 │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │  ├── ProjectEventHubHandler (7 event handlers)          │    │
│  │  ├── UserEventHubHandler (4 event handlers)             │    │
│  │  ├── NotificationHubHandler (3 event handlers)          │    │
│  │  └── ActivityHubHandler (1 event handler)               │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Client-Side WebSocket Hooks

```typescript
// use-realtime.ts - Real-time subscription hook
export function useRealtime<T>(eventType: string, handler: (data: T) => void) {
    useEffect(() => {
        const connection = getSignalRConnection();
        connection.on(eventType, handler);
        return () => connection.off(eventType, handler);
    }, [eventType, handler]);
}

// use-event-subscription.ts - Event subscription hook
export function useEventSubscription(projectId: string) {
    useEffect(() => {
        const connection = getSignalRConnection();
        connection.invoke('SubscribeToProject', projectId);
        return () => connection.invoke('UnsubscribeFromProject', projectId);
    }, [projectId]);
}
```

---

## File Storage

### Storage Architecture

The platform uses S3-compatible storage for file uploads with database metadata tracking.

```
┌─────────────────────────────────────────────────────────────────┐
│                      STORAGE ARCHITECTURE                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   IStorageService                        │    │
│  │              (Cloud Storage Abstraction)                 │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │  • GetPresignedUploadUrlAsync(request, userId)          │    │
│  │  • GetPresignedDownloadUrlAsync(bucketPath)             │    │
│  │  • UploadAsync(stream, fileName, contentType)           │    │
│  │  • DeleteAsync(bucketPath)                              │    │
│  │  • ExistsAsync(bucketPath)                              │    │
│  │  • GetPublicUrl(bucketPath)                             │    │
│  │  • GetMimeType(contentType)                             │    │
│  │  • CopyAsync(source, destination)                       │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│              ┌───────────────┴───────────────┐                  │
│              ▼                               ▼                   │
│  ┌───────────────────────┐      ┌───────────────────────┐       │
│  │   S3StorageService    │      │  LocalStorageService  │       │
│  │  (Production/Cloud)   │      │   (Development)       │       │
│  ├───────────────────────┤      ├───────────────────────┤       │
│  │  • AWS S3             │      │  • Local filesystem   │       │
│  │  • MinIO              │      │  • ./uploads folder   │       │
│  │  • Cloudflare R2      │      │  • Direct URLs        │       │
│  │  • DigitalOcean Spaces│      │                       │       │
│  └───────────────────────┘      └───────────────────────┘       │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  IAttachmentService                      │    │
│  │              (Database Metadata CRUD)                    │    │
│  ├─────────────────────────────────────────────────────────┤    │
│  │  • GetByIdAsync(id)                                     │    │
│  │  • GetByUserIdAsync(userId)                             │    │
│  │  • CreateAsync(dto)                                     │    │
│  │  • UpdateLastUsedAsync(id)                              │    │
│  │  • DeleteAsync(id)                                      │    │
│  │  • GetByTypeAsync(mimeType)                             │    │
│  │  • GetByBucketPathAsync(path)                           │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Storage Configuration

The storage service supports both `appsettings.json` format and flat environment variables.

**Environment Variables (Recommended for Docker/Production):**

| Environment Variable | Description | Default |
|---------------------|-------------|---------|
| `STORAGE_PROVIDER` | Storage provider (S3, Local) | `Local` |
| `S3_ACCESS_KEY` | AWS Access Key ID | - |
| `S3_SECRET_KEY` | AWS Secret Access Key | - |
| `S3_BUCKET_NAME` | S3 bucket name | - |
| `S3_REGION` | AWS region | `us-east-1` |
| `S3_SERVICE_URL` | Custom S3 endpoint (MinIO, R2) | - |
| `S3_USE_PATH_STYLE` | Path-style addressing | `false` |
| `S3_PUBLIC_BASE_URL` | CDN/custom public URL | - |
| `LOCAL_STORAGE_PATH` | Local storage path | `./uploads` |
| `LOCAL_STORAGE_BASE_URL` | Local public URL | `http://localhost:8080/files` |

**Configuration Binding:**

The `StorageServiceExtensions` uses a custom binding approach to support both formats:

```csharp
// Reads from environment variable first, falls back to appsettings path
private static string? GetConfigValue(IConfiguration configuration, string envVarName, string settingsPath)
{
    var envValue = Environment.GetEnvironmentVariable(envVarName);
    if (!string.IsNullOrEmpty(envValue))
        return envValue;
    return configuration[settingsPath];
}
```

**appsettings.json Format:**

```json
{
  "Storage": {
    "Provider": "S3",
    "DefaultExpirationMinutes": 60,
    "MaxFileSizeBytes": 104857600,
    "AllowedExtensions": [],
    "S3": {
      "Region": "us-east-1",
      "BucketName": "ardanova-uploads",
      "AccessKeyId": "",
      "SecretAccessKey": "",
      "ServiceUrl": "",
      "ForcePathStyle": false,
      "PublicBaseUrl": ""
    }
  }
}
```

### Upload Flow

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Client  │    │   API    │    │  Storage │    │    S3    │
│  (Web)   │    │Controller│    │  Service │    │  Bucket  │
└────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘
     │               │               │               │
     │ POST /upload-url              │               │
     │──────────────>│               │               │
     │               │ GetPresignedUploadUrl         │
     │               │──────────────>│               │
     │               │               │ Generate URL  │
     │               │               │──────────────>│
     │               │               │<──────────────│
     │ { uploadUrl, bucketPath }     │               │
     │<──────────────│               │               │
     │               │               │               │
     │ PUT to presigned URL          │               │
     │───────────────────────────────────────────────>
     │               │               │               │
     │ POST /attachments (register)  │               │
     │──────────────>│               │               │
     │               │ CreateAsync   │               │
     │               │──────────────>│               │
     │ { attachment record }         │               │
     │<──────────────│               │               │
```

---

## Application Architecture

### Client Application (ardanova-client)

The main platform is a Next.js 15 application using the App Router pattern.

```
┌─────────────────────────────────────────────────────────────────┐
│                    ARDANOVA-CLIENT ARCHITECTURE                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                     APP ROUTES                           │    │
│  │  /auth, /dashboard, /projects, /dao, /studio,           │    │
│  │  /exchange, /explorer, /agent, /marketplace             │    │
│  └────────────────────────┬────────────────────────────────┘    │
│                           │                                      │
│          ┌────────────────┼────────────────┐                    │
│          ▼                ▼                ▼                    │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐           │
│  │ tRPC Client │   │  SignalR    │   │   Storage   │           │
│  │(React Query)│   │  Hooks      │   │   Client    │           │
│  └──────┬──────┘   └──────┬──────┘   └──────┬──────┘           │
│         │                 │                 │                    │
│         └─────────────────┴─────────────────┘                    │
│                           │                                      │
│                           ▼                                      │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    API CLIENT                            │    │
│  │  lib/api/ardanova/                                       │    │
│  │  • Base HTTP client with API key auth                   │    │
│  │  • Typed endpoints for all resources                    │    │
│  │  • Error handling & retries                             │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Backend Architecture (.NET 8)

```
┌─────────────────────────────────────────────────────────────────┐
│                    .NET BACKEND ARCHITECTURE                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    ArdaNova.API                          │    │
│  │  • 14 REST Controllers                                  │    │
│  │  • API Key Middleware                                   │    │
│  │  • Exception Handling Middleware                        │    │
│  │  • SignalR Hub (ArdaNovaHub)                           │    │
│  │  • Event Bus (InMemoryEventBus)                        │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│  ┌───────────────────────────┼───────────────────────────┐      │
│  │                           │                           │      │
│  ▼                           ▼                           ▼      │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐        │
│  │ArdaNova.MCP  │   │ArdaNova.     │   │ArdaNova.     │        │
│  │(40+ Tools)   │   │Application   │   │Infrastructure│        │
│  └──────────────┘   └──────────────┘   └──────────────┘        │
│                              │                  │               │
│                     ┌────────┴────────┐   ┌────┴────────┐      │
│                     │ 28+ Services    │   │  DbContext  │      │
│                     │ Result<T>       │   │  Repository │      │
│                     │ AutoMapper      │   │  S3 Storage │      │
│                     └─────────────────┘   └─────────────┘      │
│                                                  │               │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    ArdaNova.Domain                       │    │
│  │  60+ Entities, Enums, Value Objects                     │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Flow

### Request Flow: Client → tRPC → API → Database

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  React   │───>│  tRPC    │───>│  tRPC    │───>│   API    │───>│  .NET    │
│Component │    │  Client  │    │  Server  │    │  Client  │    │  Backend │
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └────┬─────┘
                                                                      │
                                                        ┌─────────────┤
                                                        ▼             ▼
                                                 ┌──────────┐  ┌──────────┐
                                                 │PostgreSQL│  │    S3    │
                                                 └──────────┘  └──────────┘
```

### Real-Time Event Flow

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Service │───>│ Event    │───>│   Hub    │───>│ SignalR  │───>│  Client  │
│  Action  │    │   Bus    │    │ Handler  │    │WebSocket │    │  (React) │
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
     │                                                                │
     │  ProjectService.CreateAsync()                                  │
     │       │                                                        │
     │       ├── Publish(ProjectCreatedEvent)                        │
     │       │         │                                              │
     │       │         └── ProjectEventHubHandler.Handle()           │
     │       │                  │                                     │
     │       │                  └── Hub.Clients.Group().ProjectCreated()
     │       │                                   │                    │
     │       │                                   └────────────────────┘
     │       │                                      ReceiveEvent()
```

---

## Database Design

### Key Schema Relationships

| Parent | Child | Relationship |
|--------|-------|--------------|
| Project | Roadmap | 1:1 |
| Roadmap | RoadmapPhase | 1:N |
| RoadmapPhase | Epic | 1:N |
| Epic | PBI | 1:N |
| PBI | BacklogItem | 1:N |
| BacklogItem | ProjectTask | 1:N |
| Sprint | ProjectTask | N:M (via SprintItem) |
| Project | ProjectMember | 1:N |
| Project | Proposal | 1:N |
| Project | ProjectToken | 1:1 |
| ProjectToken | ICO | 1:1 |
| User | XPEvent | 1:N |
| User | UserAchievement | 1:N |
| User | Attachment | 1:N |

---

## Code Generation & EF Core

### Prisma to C# Entity Generation

The database schema is defined in DBML (`ardanova-client/prisma/database-archietecture.dbml`) which generates Prisma schema, and C# entities are generated from DBML.

**Source of Truth:** `prisma/database-archietecture.dbml` → `prisma/schema.prisma` → C# Entities

**Generator:** `ardanova-client/scripts/generate-csharp-models.ts`

### Attribute-Based EF Core Configuration

The generator uses EF Core data annotations (attributes) for all model configuration, eliminating manual Fluent API in DbContext.

**Generated Attributes:**

| Attribute | Source | Purpose |
|-----------|--------|---------|
| `[Table("Name")]` | DBML table name | Table mapping |
| `[Key]` | DBML `pk` | Primary key |
| `[Required]` | DBML `not null` | Required field |
| `[Column(TypeName = "text")]` | DBML `text` type | Text column type |
| `[Precision(18, 8)]` | DBML `decimal(x,y)` or default | Decimal precision |
| `[Index(nameof(field), IsUnique = true)]` | DBML `unique` | Unique index |
| `[ForeignKey("fieldId")]` | DBML references | Foreign key |
| `[InverseProperty("Collection")]` | Multi-FK detection | Relationship disambiguation |

**Example Generated Entity:**

```csharp
[Index(nameof(email), IsUnique = true)]
[Table("User")]
public class User
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string email { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? bio { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal trustScore { get; set; }

    // Multi-FK collections with descriptive names
    public virtual ICollection<Referral> ReferralsAsReferrer { get; set; } = new List<Referral>();
    public virtual ICollection<Referral> ReferralsAsReferred { get; set; } = new List<Referral>();
}
```

### Convention-Based DbContext

The DbContext uses two conventions instead of manual Fluent API configuration:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // All enums stored as strings in the database
    modelBuilder.ApplyEnumStringConvention();

    // Composite indexes and other generated configurations
    modelBuilder.ApplyGeneratedConfigurations();
}
```

**Key Files:**

| File | Purpose |
|------|---------|
| `ArdaNova.Infrastructure/Conventions/EnumStringConvention.cs` | Converts all enum properties to strings |
| `ArdaNova.Infrastructure/Data/GeneratedModelConfigurations.cs` | Generated composite index configurations |
| `ArdaNova.Domain/ArdaNova.Domain.csproj` | References `Microsoft.EntityFrameworkCore.Abstractions` |

### Multi-FK Handling with `[InverseProperty]` Attributes

The generator automatically handles entities with multiple foreign keys to the same type using EF Core's `[InverseProperty]` attribute. This eliminates the need for manual Fluent API configuration.

**How it works:**

1. **Detection**: The generator detects when multiple FKs from one entity point to the same target entity
2. **Descriptive Collections**: Generates named collections on the target entity (e.g., `ReferralsAsReferrer`, `ReferralsAsReferred`)
3. **InverseProperty**: Adds `[InverseProperty]` attribute on the many-side to link navigation properties with their corresponding collections

**Example Generated Code:**

```csharp
// User.cs (the "one" side - target entity)
public virtual ICollection<Referral> ReferralsAsReferrer { get; set; } = new List<Referral>();
public virtual ICollection<Referral> ReferralsAsReferred { get; set; } = new List<Referral>();

// Referral.cs (the "many" side - source entity)
[ForeignKey("referrerId")]
[InverseProperty("ReferralsAsReferrer")]
public virtual User? Referrer { get; set; }

[ForeignKey("referredId")]
[InverseProperty("ReferralsAsReferred")]
public virtual User? Referred { get; set; }
```

**Entities with Multi-FK Patterns:**
- `Referral` → User (referrerId, referredId)
- `DelegatedVote` → User (delegatorId, delegateeId)
- `ChatMessage` → User (userFromId, userToId)
- `Invoice` → User (userId, buyerId)
- `Sale` → User (userId, buyerId)
- `TaskSubmission` → User (submittedById, reviewedById)
- `TokenSwap` → ProjectToken (fromTokenId, toTokenId)
- `LiquidityPool` → ProjectToken (token1Id, token2Id)
- `ProjectTaskDependency` → ProjectTask (taskId, dependsOnId)

**Delete Behavior:** Uses EF Core defaults (Cascade for required FKs, ClientSetNull for optional). Override at database level if Restrict behavior is needed

### Running the Generator

```bash
cd ardanova-client
node --import tsx scripts/generate-csharp-models.ts

# Options:
# --dry-run    Preview changes without writing files
# --no-remove  Don't remove stale DbSets from DbContext
```

The generator:
1. Reads `prisma/database-archietecture.dbml`
2. Generates C# entities in `ArdaNova.Domain/Models/Entities/`
3. Generates C# enums in `ArdaNova.Domain/Models/Enums/`
4. Generates `GeneratedModelConfigurations.cs` for composite indexes
5. Updates `ArdaNovaDbContext.cs` with new/removed DbSets

---

## API Layer

### REST Controllers (14 Total)

| Controller | Endpoints | Description |
|------------|-----------|-------------|
| `UsersController` | 8 | User CRUD, verification |
| `ProjectsController` | 13 | Project CRUD, publishing, featured |
| `ActivitiesController` | 4 | Activity logging |
| `AgenciesController` | 7 | Agency management |
| `BusinessesController` | 6 | Business operations |
| `DelegatedVotesController` | 5 | Governance voting |
| `ExchangeController` | 6 | Token swap/liquidity |
| `NotificationsController` | 5 | Notification management |
| `ReferralsController` | 5 | Referral system |
| `TaskEscrowsController` | 5 | Escrow services |
| `UserStreaksController` | 4 | Gamification streaks |
| `WalletsController` | 5 | Wallet operations |
| `AttachmentsController` | 9 | File storage ✅ NEW |

### Service Layer (28+ Services)

| Category | Services |
|----------|----------|
| **User** | UserService, AccountService, SessionService, UserSkillService, UserExperienceService |
| **Project** | ProjectService, ProjectTaskService, ProjectResourceService, ProjectMilestoneService, ProjectSupportService, ProjectApplicationService, ProjectCommentService, ProjectUpdateService, ProjectEquityService |
| **Agency** | AgencyService, AgencyMemberService, ProjectBidService, AgencyReviewService |
| **Business** | BusinessService, CustomerService, ProductService, InvoiceService, SaleService, InventoryItemService, MarketingCampaignService |
| **Financial** | WalletService, TaskEscrowService, TokenSwapService, LiquidityPoolService |
| **Gamification** | UserStreakService, ReferralService |
| **Communication** | NotificationService, ActivityService |
| **Governance** | DelegatedVoteService |
| **Storage** | AttachmentService ✅ NEW |

---

## Authentication & Authorization

### Authentication Flow

```
┌──────────┐     ┌──────────┐     ┌──────────┐
│  Client  │────>│  OAuth   │────>│  Google  │
│  (Web)   │     │ Redirect │     │ Provider │
└──────────┘     └──────────┘     └────┬─────┘
                                       │
                                       ▼
                                ┌──────────┐
                                │ Callback │
                                │ NextAuth │
                                └────┬─────┘
                                     │
                        ┌────────────┼────────────┐
                        ▼            ▼            ▼
                  ┌──────────┐ ┌──────────┐ ┌──────────┐
                  │  Create  │ │  Create  │ │  Issue   │
                  │   User   │ │  Session │ │   JWT    │
                  └──────────┘ └──────────┘ └──────────┘
```

### Authorization (RBAC)

| Role | Level | Permissions |
|------|-------|-------------|
| Founder | 100 | All permissions |
| Leader | 80 | Roadmap, treasury propose, member management |
| Core Contributor | 60 | Epic CRUD, task assign/review, proposals |
| Contributor | 40 | Task claim/submit, vote, agent use |
| Observer | 20 | Read-only access |

---

## AI & MCP Integration

### Python AI Orchestrator (Stubbed)

```
ardanova-ai-client/
├── src/
│   ├── api/
│   │   └── main.py           # FastAPI application
│   ├── orchestrator/         # Agent routing & context
│   ├── agents/
│   │   ├── base.py           # Base agent class
│   │   ├── project_manager.py
│   │   ├── pitch_generator.py
│   │   ├── governance.py
│   │   └── token_analyst.py
│   ├── mcp/
│   │   └── client.py         # MCP protocol client
│   └── config/
│       └── settings.py       # App settings
├── requirements.txt
└── Dockerfile
```

### MCP Tools (40+ Implemented)

| Category | Tools |
|----------|-------|
| **User** | user_get_by_id, user_get_by_email, user_create, user_update, user_verify |
| **Project** | project_get_by_id, project_get_by_slug, project_create, project_update, project_publish, project_delete |
| **Agency** | agency_get_by_id, agency_create, agency_verify, agency_delete |
| **Business** | business_get_by_id, business_create, business_upgrade_plan |
| **Task** | task_assign, task_complete, task_review |
| **Governance** | proposal_create, proposal_vote |
| **Analytics** | analytics_*, report_* |

---

## Blockchain Integration

### Smart Contracts (PyTeal - Stubbed)

```
contracts/
├── governance/     # DAO governance (proposals, voting)
│   └── __init__.py
├── tokens/         # ASA token factories
│   └── __init__.py
├── ico/            # ICO lifecycle contracts
│   └── __init__.py
├── exchange/       # DEX/swap contracts
│   └── __init__.py
└── escrow/         # Task payment escrow
    └── __init__.py
```

### Client Integration (TODO)

```
lib/blockchain/
├── sdk/            # Core Algorand SDK wrapper
├── tokens/         # ASA token management
├── ico/            # ICO lifecycle management
├── exchange/       # Token exchange logic
├── wallet/         # Wallet connection (Pera, Defly)
├── indexer/        # Algorand indexer client
└── contracts/      # Smart contract ABIs & interactions
```

---

## Infrastructure

### Deployment Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEPLOYMENT ARCHITECTURE                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  CDN (Vercel Edge)                      │    │
│  │  • Static assets, Edge functions, Image optimization    │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  APPLICATION TIER                        │    │
│  │  ┌────────────────────────────────────────────────────┐ │    │
│  │  │         ardanova-client (Railway/Vercel)           │ │    │
│  │  │         www.ardanova.com                           │ │    │
│  │  └────────────────────────────────────────────────────┘ │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    SERVICE TIER                          │    │
│  │  ┌──────────────┐  ┌──────────────┐                     │    │
│  │  │ .NET Backend │  │ Python AI    │                     │    │
│  │  │ api.ardanova │  │ ai.ardanova  │                     │    │
│  │  │ + SignalR    │  │ (Railway)    │                     │    │
│  │  │ + Storage    │  │              │                     │    │
│  │  │ (Railway)    │  │              │                     │    │
│  │  └──────────────┘  └──────────────┘                     │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                      DATA TIER                           │    │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐               │    │
│  │  │PostgreSQL│  │  Redis   │  │S3/R2/MinIO              │    │
│  │  │(Railway) │  │ (Upstash)│  │(Cloudflare)│             │    │
│  │  └──────────┘  └──────────┘  └──────────┘               │    │
│  └─────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                   BLOCKCHAIN TIER                        │    │
│  │  Algorand (TestNet → MainNet)                           │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Subdomains

| Subdomain | Service | Description |
|-----------|---------|-------------|
| www.ardanova.com | ardanova-client | Main platform (all features) |
| api.ardanova.com | ardanova-backend-api-mcp | .NET 8 API + MCP + SignalR |
| ai.ardanova.com | ardanova-ai-client | Python AI orchestrator |

---

## Security

### Security Measures

1. **Authentication**
   - OAuth 2.0 with Google (NextAuth)
   - API Key authentication for backend
   - JWT tokens for API calls

2. **Authorization**
   - Role-based access control (RBAC)
   - Permission checks in tRPC procedures
   - Token-weighted permissions for DAO

3. **Data Protection**
   - Encryption at rest (database)
   - Encryption in transit (TLS 1.3)
   - Sensitive data hashing
   - Presigned URLs for file access

4. **API Security**
   - Input validation (Zod schemas)
   - SQL injection prevention (EF Core)
   - Rate limiting
   - CORS configuration

5. **Smart Contract Security**
   - PyTeal best practices
   - Multi-sig for admin functions
   - Timelocks for governance
   - Audits before mainnet

---

## Implementation Status

### Completed ✅

| Feature | Status | Location |
|---------|--------|----------|
| .NET Backend API | ✅ Complete | `ardanova-backend-api-mcp/` |
| 14 REST Controllers | ✅ Complete | `ArdaNova.API/Controllers/` |
| 28+ Application Services | ✅ Complete | `ArdaNova.Application/Services/` |
| 40+ MCP Tools | ✅ Complete | `ArdaNova.MCP/` |
| Event Bus | ✅ Complete | `ArdaNova.API/EventBus/` |
| SignalR WebSocket | ✅ Complete | `ArdaNova.API/WebSocket/` |
| S3 File Storage | ✅ Complete | `ArdaNova.Infrastructure/Storage/` |
| Env Var Config Binding | ✅ Complete | `StorageServiceExtensions.cs` |
| Attachment Controller | ✅ Complete | `ArdaNova.API/Controllers/AttachmentsController.cs` |
| C# Model Generator | ✅ Complete | `ardanova-client/scripts/generate-csharp-models.ts` |
| EF Core Conventions | ✅ Complete | `ArdaNova.Infrastructure/Conventions/` |
| Dev Scripts (Bash/PS) | ✅ Complete | `scripts/` |
| Docker Compose (Dev) | ✅ Complete | `docker-compose.dev.yml` |
| AI Client Stub | ✅ Stubbed | `ardanova-ai-client/` |
| Smart Contracts Stub | ✅ Stubbed | `contracts/` |

### In Progress 🔄

| Feature | Status | Notes |
|---------|--------|-------|
| DAO UI | 🔄 TODO | Governance proposals, voting |
| Studio UI | 🔄 TODO | Gamma API integration |
| Exchange UI | 🔄 TODO | Token swap, liquidity |
| Explorer UI | 🔄 TODO | Blockchain/token explorer |
| Agent UI | 🔄 TODO | AI agent chat interface |
| Game SDK | 🔄 TODO | Unity & Godot SDKs |

### Known Issues

| Issue | Workaround | Status |
|-------|------------|--------|
| Multi-FK EF Core relationships | `[InverseProperty]` attributes | ✅ Fixed |
| Next.js Turbopack/Alpine WASM | Use standard Node image | 🔄 Open |
| Port 8080 conflicts (local) | Stop conflicting services | 🔄 Open |

---

## Next Steps

1. ✅ **Phase 1**: .NET Backend API with Clean Architecture - **COMPLETE**
2. ✅ **Phase 2**: MCP Tools Integration (40+ tools) - **COMPLETE**
3. ✅ **Phase 3**: Event Bus & WebSocket - **COMPLETE**
4. ✅ **Phase 4**: File Storage Service - **COMPLETE**
5. ✅ **Phase 5**: AI Client & Contracts Stubs - **COMPLETE**
6. ✅ **Phase 5.1**: Dev Scripts & Docker Hot-Reload - **COMPLETE**
7. 🔄 **Phase 6**: Consolidate DAO, Studio, Exchange, Explorer into client
8. **Phase 7**: Blockchain integration (Algorand)
9. **Phase 8**: Smart contract development (PyTeal)
10. **Phase 9**: Game SDK (Unity, Godot)

See [ROADMAP.md](./ROADMAP.md) for detailed timelines.

---

**Last Updated**: January 2025
**Version**: 4.2.0 (Attribute-Based EF Core, Convention DbContext, Enhanced Generator)
