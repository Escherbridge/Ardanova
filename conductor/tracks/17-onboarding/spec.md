# Onboarding & First-Time Experience — Technical Specification

## Overview

New users land on a static landing page with hardcoded $0 stats and no guided experience after sign-in. This track creates a first-time user flow and polishes the landing page with real data.

## Current State

- Landing page (`/page.tsx`): Static marketing content, hardcoded stats, non-functional newsletter
- Auth flow: Google OAuth → dashboard redirect. No user type selection, no profile setup
- Dashboard: Shows hardcoded sample data (addressed in Track 12)
- User entity has `userType` (INNOVATOR, FREELANCER, etc.) but it's never set by the user

## What Needs to Be Built

### User Type Selection (First Sign-In)
- After first OAuth sign-in, redirect to `/onboarding` instead of `/dashboard`
- Step 1: "What brings you to ArdaNova?" — Innovator / Contributor / Supporter
- Sets `user.userType` via `api.user.update`
- Only shown once (check if userType is set)

### Profile Setup Wizard
- Step 2: Basic info (display name, bio, location)
- Step 3: Skills & interests (select from predefined + custom)
- Progress bar showing 3 steps

### First Action Prompt
- After onboarding, show contextual CTA based on user type:
  - Innovator: "Create your first project"
  - Contributor: "Browse opportunities"
  - Supporter: "Explore projects to fund"

### Landing Page Polish
- Replace hardcoded $0 stats with real metrics from platform API
- Wire newsletter subscription to email service or remove the form
