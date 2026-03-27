# Track 16 — Application & Hiring Flow UI

## 1. Opportunity Applications
- [ ] **[P0] Wire "Apply Now" button on opportunity detail page**
    - Connect to `api.opportunity.apply` mutation
    - Show application form modal (cover letter, relevant skills)
- [ ] **[P0] Application status indicator on opportunity cards**
    - Show "Applied" badge if user has pending application
    - Status: PENDING → ACCEPTED/REJECTED
- [ ] **[P1] Application review in opportunity detail (owner view)**
    - List pending applications with applicant profiles
    - Accept/reject actions with optional message

## 2. Project Applications
- [ ] **[P0] Wire "Join Project" button**
    - Connect to `api.project.applyToProject` mutation
    - Application form with role preference
- [ ] **[P1] Project applications tab (owner view)**
    - List pending applications in team tab
    - Accept/reject with auto-team-add
    - Option to grant membership credential on acceptance

## 3. Guild Applications
- [ ] **[P1] Wire "Join Guild" button**
    - Connect to `api.guild.apply` mutation
- [ ] **[P1] Guild application review (owner view)**
    - List pending applications in members tab
    - Accept/reject flow

## 4. Application Management (User Side)
- [ ] **[P1] My Applications page or profile section**
    - List all user's applications across opportunities/projects/guilds
    - Status tracking (pending/accepted/rejected)
    - Withdraw application option
