# Dual-Path Auth With Portal SSO

## Metadata

- **Date:** 2026-04-13
- **Status:** Accepted
- **Deciders:** NIE Template maintainers
- **AI Model Used:** GPT-5 Codex

## Context

The template already supported direct username/password login through the Auth API and then relied on Valkey-backed sessions plus `UserId`-based authorization in the Main API. The new requirement was to add a second, permanent SSO entry path from a portal application without changing the downstream session or authorization model.

## Options Considered

### Option A: Replace manual login with full in-app federation

**Description:** Move the template to a single SSO-only model and remove the direct credential path.

- **Pros:** Simplifies the long-term auth surface.
- **Cons:** Does not match the requirement to keep individual-system login permanently available.

### Option B: Keep manual login and add a parallel portal SSO callback flow

**Description:** Preserve `Auth/Login` and add `Auth/SsoStart`, `Auth/SsoCallback`, and `Auth/SsoFinalize`, then reuse the same session issuance logic after either path authenticates.

- **Pros:** Matches the business requirement, keeps the Main API unchanged, and limits the blast radius to the Auth service and auth frontend.
- **Cons:** Adds more configuration and a second entry path to maintain.

### Option C: Let the frontend relay unsigned SSO payloads

**Description:** Accept SSO payloads through the browser and trust frontend-posted identity data.

- **Pros:** Faster to wire initially.
- **Cons:** Weak trust boundary and higher tampering risk.

## Decision

Choose **Option B**. The template now supports direct username/password login and portal SSO side by side. The portal path validates an encrypted/signed callback payload, exchanges it for the same `LoginResponse` contract used by the legacy path, and then reuses the same session creation logic.

## Consequences

- **Positive:** Manual login remains intact, portal one-click login is added, and downstream authorization remains unchanged.
- **Negative:** The Auth service now carries more configuration and callback-state handling.
- **Risks:** Misconfigured keys, issuer/audience values, or exchange API settings will make SSO unavailable until corrected.

## AI Reasoning Chain

> The implementation keeps `LoginResponse.userId` as the canonical identity and treats the SSO exchange API as the authoritative source of that value. This avoids breaking role lookup, access-function resolution, and any downstream logic already keyed to `UserId`.
