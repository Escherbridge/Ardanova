# ArdaNova Tech Stack

## Overview
-   **Frontend**: Next.js 15 (App Router), TypeScript, TailwindCSS, tRPC.
-   **Backend**: C# .NET 9, EF Core, SignalR.
-   **Database**: PostgreSQL (Managed via Prisma).
-   **AI Integration**: Gamma API + MCP Server (C#).

## Key Architecture Decisions

### Database Management
-   **Source of Truth**: `ardanova-client\prisma\database-architecture.dbml`
-   **Schema**: All database schema changes MUST be defined in the DBML file first.
-   **Generators**:
    1.  `npm run generate:prisma` -> Converts DBML to Prisma Schema (`schema.prisma`) and pushes to DB.
    2.  `npm run generate:csharp` -> Converts DBML/Prisma schema to C# EF Core entities in `ArdaNova.Infrastructure`.

### Communication
-   **Client-Server**: tRPC for type-safe API calls.
-   **Real-time**: SignalR for WebSocket connections (notifications, chat, live updates).
-   **AI-Agent**: MCP Protocol for communication between AI agents and the backend tools.

### Authentication
-   **Auth**: NextAuth.js (v5) with Google Provider.
