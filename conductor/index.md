---
type: index
---

# ArdaNova Conductor Context

## Index
- [Product Definition](product.md) - High-level overview and goals.
- [Tech Stack](tech-stack.md) - Technology, database, and architecture.
- [Workflows](workflow.md) - Key user flows and processes.
- [Product Guidelines](product-guidelines.md) - QA checklist and best practices.
- [Tracks Registry](tracks.md) - Every track, its status, and the cross-cutting status notes.
- [AZOA Integration Contract](ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) - The AZOA node boundary. **Byte-identical with AZOA's copy — change both in the same commit window.**
- [Platform Architecture](../documentation/ARCHITECTURE.md) - Detailed technical architecture: repository structure, service boundaries, data flow, and integration patterns.
- [Runbook](RUNBOOK.md) - Current environment state, open blockers, and what is uncommitted right now.

## Usage
Use these files to understand the project context when generating code or answering questions.

> **Terminology.** AZOA's domain nouns are **user** (was avatar) and **action**
> (was quest). ArdaNova cut over in `f2e4bc5`; routes, wire fields and SDK
> exports use the new names, while ArdaNova's own C# types, DB columns and config
> keys deliberately still say `Avatar`/`Quest`. See [`tracks.md`](tracks.md)
> § "Cross-cutting status".
