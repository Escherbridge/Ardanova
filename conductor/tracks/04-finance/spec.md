# Financial & Tokenomics (Legacy) — Retroactive Specification

> This document retroactively captures early financial modeling work that was later superseded by Track 09 (Tokenomics & Project Equity).

## Status: SUPERSEDED BY TRACK 09

The initial financial concepts were implemented as part of the broader tokenomics system in Track 09. All equity, token, treasury, payout, and Stripe functionality now lives in Track 09's scope.

## What Was Originally Planned

Early iterations included:
- Basic project funding model
- Equity share tracking
- Simple payout calculations

## Where It Lives Now

All financial functionality has been fully implemented in Track 09:
- **Project Tokens**: `ProjectTokensController`, `ProjectTokenService` — token configuration, allocation, distribution
- **Token Balances**: `TokenBalanceController`, `TokenBalanceService` — balance tracking, crediting, debiting
- **Payouts**: `PayoutsController`, `PayoutService` — payout requests, processing, Stripe disbursement
- **Treasury**: `TreasuryController`, `TreasuryService` — three-bucket model, rebalancing, audit log
- **Exchange**: `ExchangeService` — project token to ARDA conversion
- **Stripe**: `StripeService`, `StripeWebhookController` — checkout, Connect accounts, webhooks
- **Project Gates**: `ProjectGateService` — funding/liquidity gate transitions

See [Track 09 spec](../09-tokenomics/spec.md) for full details.
