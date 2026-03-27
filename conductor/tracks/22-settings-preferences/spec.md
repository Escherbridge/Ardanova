# Settings & Preferences — Technical Specification

## Overview

Currently the settings page only redirects to KYC verification. Users need profile editing, notification preferences, privacy controls, and connected account management. Backend has user update endpoints and notification services.

## Current State

- **Backend**: UsersController has update profile endpoints
- **Backend**: NotificationServices with preference management
- **Backend**: StripeService with Connect account onboarding
- **Frontend**: `/settings` redirects to `/settings/verification` (KYC page)
- **Frontend**: No profile edit, no notification prefs, no privacy settings

## What Needs to Be Built

### Profile Settings
- Edit name, bio, headline, avatar
- Manage skills list (add/remove)
- Social links (GitHub, LinkedIn, Twitter, website)
- Uses existing `api.user.update` mutation

### Notification Preferences
- Toggle per notification type: email, in-app, push
- Categories: task updates, project activity, governance, social, financial
- Uses NotificationService preference endpoints

### Privacy Settings
- Profile visibility: public, members-only, private
- Search indexing opt-out
- Activity feed visibility

### Connected Accounts
- Stripe Connect onboarding for receiving payouts
- Wallet connection status
- OAuth connections (if applicable)

### Wallet Management
- View connected wallet address
- Wire existing Algorand wallet backend to UI
- Balance display (ARDA tokens, project tokens)
