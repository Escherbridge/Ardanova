# ArdaNova Workflows

## 1. Project Creation
-   **Entry Point**: `/dashboard/create` or `/projects/create`
-   **Steps**:
    1.  **Basic Info**: Title, Description, Category, Visibility.
    2.  **Resources**: Define required resources (Budget, skilled roles).
    3.  **Roles**: Define team structure (Founder, Leader, Contributor).
    4.  **Milestones**: Set initial milestones/timeline.
    5.  **Review**: Confirm details and submit.
-   **Outcome**: Project created in `DRAFT` status (or `PUBLISHED` if selected).

## 2. Member Addition & Credentialing
-   **Concept**: Membership is managed via `MembershipCredential`s, not just simple roles.
-   **Types**:
    -   **Founder**: Granted automatically to creator.
    -   **DAO Vote**: Granted via community proposal and voting.
    -   **Contribution**: Earned by completing tasks/bounties.
-   **Flow**:
    1.  User requests membership or is invited (`ProjectInvitation`).
    2.  If invitation accepted -> `MembershipCredential` issued.
    3.  If request approved (by Owner or DAO) -> `MembershipCredential` issued.

## 3. Guild Management
-   **Entry Point**: `/guilds`
-   **Creation**:
    -   Define Guild Name, Category (e.g., "Developers", "Designers").
    -   Set admission criteria (Open, Application, Invite Only).
-   **Joining**:
    -   User applies -> Guild Admin reviews application (`GuildApplication`).
    -   Approved -> User becomes Member.

## 4. Task Workflow
-   **Entry Point**: Project "Tasks" tab or "Opportunities" board.
-   **Actors**: `ProjectOwner`, `Contributor`, `Reviewer`.
-   **Lifecycle**:
    1.  **Creation** (`TODO`): 
        -   Project Owner creates Task.
        -   Sets `Priority`, `Skills`, and optional `TaskCompensation` (Bounty/Shares).
    2.  **Assignment** (`IN_PROGRESS`):
        -   Direct assignment to a Member OR Project Member claims an open task.
        -   External users apply via **Opportunity** -> Application accepted -> Assigned.
    3.  **Execution**:
        -   Assignee works on task.
        -   (Optional) Update progress/comments.
    4.  **Submission** (`REVIEW`):
        -   Assignee submits `TaskSubmission` (Link, PR, Doc).
        -   Status changes to `REVIEW`.
    5.  **Review** (`COMPLETED` / `REVISION_REQUESTED`):
        -   Reviewer approves: 
            -   Status -> `COMPLETED`.
            -   **XP Awarded**: +50 XP (Gamification).
            -   **Payment Released**: Escrow funds released (if applicable).
        -   Reviewer rejects:
            -   Status -> `IN_PROGRESS` (with feedback).
    6.  **Escrow (If applicable)**:
        -   `TaskEscrow` released upon completion.

## 5. Governance Proposal
-   **Entry Point**: `/governance` or Project "Governance" tab.
-   **Flow**:
    1.  Create Proposal (Type: Treasury, Strategic, etc.).
    2.  Voting Period Active.
    3.  Vote tally -> Pass/Fail based on Quorum.
    4.  Execution (if passed).
