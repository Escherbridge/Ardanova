# Track 21 — Reviews & Trust Enhancement

## 1. Guild Reviews UI
- [ ] **[P3] Guild review form**
    - Star rating (1-5) + text review on guild detail page
    - Only members who have been in guild can review
    - Calls existing guild review API endpoint
- [ ] **[P3] Guild review listing**
    - Paginated reviews on guild detail page
    - Average rating display on guild cards in listings
    - Sort by: newest, highest rated, lowest rated

## 2. Project Reviews
- [ ] **[P3] Project review form**
    - Rate project on: communication, fairness, organization (1-5 each)
    - Text review with optional categories
    - Available to team members after task completion
- [ ] **[P3] Contributor review form**
    - Rate contributor on: quality, timeliness, collaboration (1-5 each)
    - Available to project owners after task completion
    - Backend: may need new ReviewService if not yet implemented

## 3. Trust Score Display
- [ ] **[P3] Trust score component**
    - Aggregates: XP level, credential count, review average, completion rate, streak
    - Visual badge: Bronze (0-25), Silver (25-50), Gold (50-75), Platinum (75-100)
    - Display on user profile and people listing cards
- [ ] **[P3] Trust breakdown tooltip**
    - Hover to see component scores
    - Link to full profile for details

## 4. Review Prompts
- [ ] **[P3] Post-completion review prompt**
    - After task marked complete, notification to review the other party
    - Non-blocking dismissible prompt
- [ ] **[P3] Pending reviews section**
    - "You have N pending reviews" on dashboard
    - Quick link to review form
