using System.Collections.Generic;

namespace ArdaNova.Application.Azoa.Quests;

/// <summary>
/// Static factory class that returns canonical AZOA quest definitions for
/// ArdaNova's scrum lifecycle (contract §5.1 + §5.2).
///
/// Each method returns an <see cref="AzoaQuestDefinition"/> that can be
/// serialised to JSON and published verbatim to the AZOA node.
/// </summary>
public static class ScrumLifecycleQuests
{
    // ────────────────────────────────────────────────────────────────────────
    //  §5.1 Quest 1 — Project Lifecycle
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the canonical "Project Lifecycle" quest definition (contract §5.1).
    ///
    /// DAG (in order):
    ///   <code>
    ///   HolonCreate(Project)
    ///     → Emit(project.created)
    ///     → GateCheck(fundingGoalMet)
    ///     → FungibleTokenCreate(ProjectShare ASA)
    ///     → GateCheck(status == "FUNDED")
    ///     → Emit(sprint.started)
    ///   </code>
    ///
    /// The quest is marked <c>IsPublic = true</c> so any avatar may instantiate it
    /// without an explicit grant.
    /// </summary>
    public static AzoaQuestDefinition CreateProjectLifecycleDefinition() => new()
    {
        Name        = "Project Lifecycle",
        Description = "End-to-end lifecycle quest for an ArdaNova project: creation → funding → sprint start.",
        IsPublic    = true,
        Nodes       = new List<AzoaQuestNode>
        {
            // ── Node 1: Create the Project Holon on-chain ──────────────────
            new()
            {
                Id   = "create-project-holon",
                Type = "HolonCreate",
                Config = new
                {
                    holonType   = "Project",
                    // Metadata fields are resolved from the run-context at execution time.
                    metaBinding = "context.projectMeta"
                },
                Next = new List<string> { "emit-project-created" }
            },

            // ── Node 2: Emit project.created event ─────────────────────────
            new()
            {
                Id   = "emit-project-created",
                Type = "Emit",
                Config = new EmitNodeConfig
                {
                    Payload = new
                    {
                        eventType = "project.created",
                        holonId   = "{{context.holonId}}"   // template expanded by AZOA runner
                    }
                },
                Next = new List<string> { "gate-funding-goal" }
            },

            // ── Node 3: Wait until funding goal is met ─────────────────────
            new()
            {
                Id   = "gate-funding-goal",
                Type = "GateCheck",
                Config = new GateCheckNodeConfig
                {
                    Predicate = "fundingGoalMet == true",
                    Reads     = new Dictionary<string, object>
                    {
                        ["fundingGoalMet"] = "context.project.fundingGoalMet"
                    }
                },
                // Only one outgoing edge: the gate blocks until the predicate is satisfied.
                Next = new List<string> { "create-project-share-asa" }
            },

            // ── Node 4: Mint the ProjectShare fungible token (ASA) ─────────
            new()
            {
                Id   = "create-project-share-asa",
                Type = "FungibleTokenCreate",
                Config = new FungibleTokenCreateNodeConfig
                {
                    ChainType = "Algorand",
                    Name      = "ArdaNova Project Share",
                    UnitName  = "ANPS",
                    Total     = 1_000_000,
                    Decimals  = 2,
                    // HolonId bound at runtime from the project holon created above.
                    HolonId   = "{{context.holonId}}"
                },
                Next = new List<string> { "gate-project-funded" }
            },

            // ── Node 5: Gate on project reaching FUNDED status ────────────
            new()
            {
                Id   = "gate-project-funded",
                Type = "GateCheck",
                Config = new GateCheckNodeConfig
                {
                    Predicate = "projectStatus == \"FUNDED\"",
                    Reads     = new Dictionary<string, object>
                    {
                        ["projectStatus"] = "context.project.status"
                    }
                },
                Next = new List<string> { "emit-sprint-started" }
            },

            // ── Node 6: Emit sprint.started event ─────────────────────────
            new()
            {
                Id   = "emit-sprint-started",
                Type = "Emit",
                Config = new EmitNodeConfig
                {
                    Payload = new
                    {
                        eventType = "sprint.started",
                        holonId   = "{{context.holonId}}",
                        sprintId  = "{{context.firstSprintId}}"
                    }
                },
                Next = new List<string>()  // terminal node
            }
        }
    };

    // ────────────────────────────────────────────────────────────────────────
    //  §5.1 Quest 2 — Task Bounty
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the canonical "Task Bounty" quest definition (contract §5.1).
    ///
    /// DAG (happy path → reject branch):
    ///   <code>
    ///   GateCheck(submissionAccepted)
    ///     [accepted]  → Transfer/Grant(reward) → Emit(task.completed)
    ///     [rejected]  → Refund(escrowed token)  → Emit(task.rejected)
    ///   </code>
    ///
    /// The quest is marked <c>IsPublic = true</c>.
    /// </summary>
    public static AzoaQuestDefinition CreateTaskBountyDefinition() => new()
    {
        Name        = "Task Bounty",
        Description = "Evaluates a task submission and either rewards the contributor or refunds the escrowed bounty.",
        IsPublic    = true,
        Nodes       = new List<AzoaQuestNode>
        {
            // ── Node 1: Gate on reviewer acceptance ────────────────────────
            new()
            {
                Id   = "gate-submission-accepted",
                Type = "GateCheck",
                Config = new GateCheckNodeConfig
                {
                    Predicate = "submissionAccepted == true",
                    Reads     = new Dictionary<string, object>
                    {
                        ["submissionAccepted"] = "context.task.submissionAccepted"
                    }
                },
                // Two outgoing edges: accepted branch and rejected branch.
                Next = new List<string> { "transfer-bounty-reward", "refund-escrowed-bounty" }
            },

            // ── Happy path: transfer / grant reward ────────────────────────
            // Using Transfer for an existing reward NFT held in escrow.
            new()
            {
                Id   = "transfer-bounty-reward",
                Type = "Transfer",
                Config = new TransferNodeConfig
                {
                    // The specific NFT id is resolved from run context at execution time.
                    NftId   = "{{context.bountyNftId}}",
                    Request = new
                    {
                        // Recipient is the actor avatar from run context — not hardcoded.
                        recipientBinding = "context.submitterAvatar",
                        note             = "Task bounty reward transfer"
                    }
                },
                Next = new List<string> { "emit-task-completed" }
            },

            new()
            {
                Id   = "emit-task-completed",
                Type = "Emit",
                Config = new EmitNodeConfig
                {
                    Payload = new
                    {
                        eventType = "task.completed",
                        taskId    = "{{context.taskId}}",
                        holonId   = "{{context.holonId}}"
                    }
                },
                Next = new List<string>()   // terminal (happy)
            },

            // ── Reject branch: refund escrowed token ───────────────────────
            new()
            {
                Id   = "refund-escrowed-bounty",
                Type = "Refund",
                Config = new RefundNodeConfig
                {
                    NftId   = "{{context.bountyNftId}}",
                    Request = new
                    {
                        note = "Task submission rejected — bounty refunded to project treasury"
                    }
                },
                Next = new List<string> { "emit-task-rejected" }
            },

            new()
            {
                Id   = "emit-task-rejected",
                Type = "Emit",
                Config = new EmitNodeConfig
                {
                    Payload = new
                    {
                        eventType = "task.rejected",
                        taskId    = "{{context.taskId}}",
                        holonId   = "{{context.holonId}}"
                    }
                },
                Next = new List<string>()   // terminal (reject)
            }
        }
    };

    // ────────────────────────────────────────────────────────────────────────
    //  §5.1 Quest 3 — Membership Credential
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the canonical "Membership Credential" quest definition (contract §5.1).
    ///
    /// DAG:
    ///   <code>
    ///   Grant(soulbound credential NFT: total=1, decimals=0, frozen=true)
    ///     → HolonCreate(Credential Holon anchored to the minted NFT)
    ///   </code>
    ///
    /// The credential is soulbound: <c>total = 1</c>, <c>decimals = 0</c>,
    /// and the ASA is configured as frozen so it cannot be transferred after minting.
    ///
    /// This quest is <c>IsPublic = false</c>; only authorized issuers may grant
    /// membership credentials.
    /// </summary>
    public static AzoaQuestDefinition CreateMembershipCredentialDefinition() => new()
    {
        Name        = "Membership Credential",
        Description = "Issues a soulbound on-chain membership credential NFT and creates an associated Credential Holon.",
        IsPublic    = false,   // Restricted — explicit issuer grant required.
        Nodes       = new List<AzoaQuestNode>
        {
            // ── Node 1: Mint soulbound credential NFT ──────────────────────
            new()
            {
                Id   = "grant-membership-nft",
                Type = "Grant",
                Config = new GrantNodeConfig
                {
                    Request = new
                    {
                        // Soulbound properties: indivisible, supply of exactly 1, frozen.
                        total         = 1UL,
                        decimals      = 0,
                        defaultFrozen = true,
                        name          = "ArdaNova Membership Credential",
                        unitName      = "ANMC",
                        // Recipient avatar is sourced from run context, not config body.
                        recipientBinding = "context.memberAvatar"
                    },
                    // HolonId set after the Credential Holon is created (back-linked at runtime).
                    HolonId = null
                },
                Next = new List<string> { "create-credential-holon" }
            },

            // ── Node 2: Create the on-chain Credential Holon ──────────────
            new()
            {
                Id   = "create-credential-holon",
                Type = "HolonCreate",
                Config = new
                {
                    holonType  = "Credential",
                    // Links back to the NFT minted in the previous node.
                    nftBinding = "context.mintedNftId",
                    metaBinding = "context.credentialMeta"
                },
                Next = new List<string>()   // terminal
            }
        }
    };
}
