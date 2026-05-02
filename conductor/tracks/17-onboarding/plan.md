# Track 17 — Onboarding & First-Time Experience

## 1. User Type Selection
- [ ] **[P0] Create /onboarding page**
    - Three-card selection: Innovator, Contributor, Supporter
    - Calls `api.user.update({ userType })` on selection
    - Swiss brutalist design with neon accent on selected card
- [ ] **[P0] First sign-in redirect logic**
    - In auth callback or dashboard layout, check if `user.userType` is unset
    - Redirect to `/onboarding` if first-time user

## 2. Profile Setup Wizard
- [ ] **[P1] Multi-step onboarding form**
    - Step 1: User type (from above)
    - Step 2: Display name, bio, location, avatar
    - Step 3: Skills selection (from existing skill taxonomy)
    - Progress indicator
- [ ] **[P1] Wire to profile update APIs**
    - `api.profile.updateMyProfile` for bio/name
    - `api.profile.addSkill` for skills

## 3. First Action Prompt
- [ ] **[P1] Post-onboarding CTA**
    - Context-aware based on user type
    - Dismissible, shows on dashboard until first action taken

## 4. Landing Page Polish
- [ ] **[P2] Replace hardcoded stats**
    - Create public API endpoint for platform metrics (user count, project count, total funded)
    - Wire to landing page
- [ ] **[P2] Newsletter form**
    - Wire to email service or replace with "Join" CTA → sign up
