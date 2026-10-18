# Opportunity components

## Two ways in: apply vs. propose paid terms

A role (opportunity) is joined through an **application**: `ApplyForm` calls
`opportunity.submitApplication`, and the manager's Accept in `ApplicationsTab` calls
`opportunity.updateApplicationStatus`, which in the API seats the applicant as a project member in the same
save (`OpportunityService.SeatAcceptedApplicantAsync`). That is the only hiring path that ends with a
person who can be assigned other tasks.

A **bid** (`BidForm`'s paid section, `OpportunityBidsPanel`'s "Paid proposals") is a paid-work offer that
task commerce settles. Commerce refuses a bid without a positive award, so the form requires an award and
the panel does not offer Accept on an unpaid bid; it points the owner at applications instead. Bid
acceptance does not seat a membership (commerce is on hold; see conductor track
`projects-hiring-followups_20261018`).

## Role status

New roles default to OPEN (`opportunity.create` sends `isOpenForApplications ?? true`). Managers open,
pause (DRAFT) or close a role from `OpportunityStatusControl`; DRAFT roles are visible only to the team, so
the detail and edit pages read as the signed-in viewer (`runWithActorAssertion`).

## Where the rules live

Who may manage a role, read bids, or decide applications is decided by the .NET API, not the tRPC routers
or these components. `isOwner` here only chooses which panel to render.
