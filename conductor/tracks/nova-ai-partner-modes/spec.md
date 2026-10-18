---
type: spec
title: Nova AI modes beyond refinement, and a project prefill from the brief
status: proposed
tags: [nova, ogai, ai, white-label]
---
# Nova AI modes beyond refinement

## Why this is a separate track

Loop 2 was asked to back one more Nova mode (Ask, Draft, Review, Present, Rehearse) with the real
partner seam "if it maps to OGAI capability". None does today. OGAI's only partner capability is the
validate stage: a fixed six-point grilling that ends in a go/adjust/not_yet verdict and a launch brief.

- **Ask** answers a question about the page's records. OGAI never sees ArdaNova records (open
  requirement 3 of `nova-ai-ogai-partner`: a server-side read check comes first), and validate does
  not answer questions; it asks them.
- **Draft / Present / Rehearse** produce free text from a prompt. Validate's strict reply schema
  (question or verdict) cannot carry a draft, an outline or practice prompts.
- **Review** ("claims, gaps, assumptions") is the closest, but pushing a draft through validate would
  return interview questions about a business idea, not a review of the draft. Labelling that "Review"
  would be untruthful copy.

So the preview tools keep their "local preview" labels. Making any of them live needs a new OGAI
partner capability, not a relabel.

## Requirements

1. OGAI: a partner `review` route (`POST /api/partner/v1/review`) with its own prompt and strict
   schema (claims, gaps, assumptions, each pointing at the reviewed text), the same brand voice
   (`voiceFor`, letters-only host match), own quota and spend meter, nothing stored.
2. ArdaNova .NET: `IOgaiClient.ReviewAsync`, a `NovaOrchestrationService` use case with the same
   allowance, brand guard and capacity mapping as refinement, and `GET /api/nova/status` reporting
   per-mode availability.
3. Client: Review becomes live only when status says so; the other modes keep their preview labels.
4. Project prefill from a refinement brief: `/projects/create` reads a brief handed over by the Nova
   sheet (session-scoped, never a URL query carrying member text) and prefills name, one-liner and
   problem, which the member edits before creating anything. Owned by the projects lane; Nova today
   offers "Copy brief" and a link, with copy that says nothing is filled in.

## Acceptance

- A Review on a deployed stack returns findings that reference the reviewed text, and no response,
  log or UI names the provider.
- With OGAI off, Review stays labelled a local preview and never calls .NET.
- A brief from a go/adjust verdict lands in the create form as editable text; nothing is created
  until the member submits.
