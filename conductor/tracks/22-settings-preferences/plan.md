# Track 22 — Settings & Preferences

## 1. Profile Settings
- [ ] **[P2] Profile edit page**
    - `/settings/profile` route
    - Form: name, bio, headline, avatar upload
    - Skills tag editor (add/remove from skill list)
    - Social links fields (GitHub, LinkedIn, Twitter, website)
    - Calls `api.user.update` mutation
- [ ] **[P2] Avatar upload**
    - Image upload via S3 (AttachmentsController exists)
    - Crop/resize before upload
    - Preview in settings form

## 2. Notification Preferences
- [ ] **[P2] Notification settings page**
    - `/settings/notifications` route
    - Toggle grid: rows = notification types, columns = channels (email, in-app)
    - Categories: tasks, projects, governance, social, financial
    - Calls notification preference API endpoints
- [ ] **[P2] Email digest frequency**
    - Immediate, daily digest, weekly digest, off
    - Per-category override option

## 3. Privacy Settings
- [ ] **[P2] Privacy settings page**
    - `/settings/privacy` route
    - Profile visibility: public / members-only / private
    - Activity feed visibility toggle
    - Search indexing opt-out toggle

## 4. Connected Accounts
- [ ] **[P2] Stripe Connect onboarding**
    - `/settings/payments` route
    - "Connect Stripe" button for payout recipients
    - Stripe Connect OAuth flow
    - Connection status display
- [ ] **[P2] Wallet management**
    - `/settings/wallet` route
    - Display connected wallet address
    - ARDA token balance
    - Project token balances list

## 5. Settings Navigation
- [ ] **[P2] Settings layout with sidebar nav**
    - Tabs: Profile, Notifications, Privacy, Payments, Wallet, Verification (existing)
    - Active tab highlighting
    - Mobile-responsive collapsible nav
