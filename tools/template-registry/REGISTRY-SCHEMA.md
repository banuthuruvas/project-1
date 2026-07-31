# Fleet Registry — Telemetry Schema

This describes the JSON payload `audit.yml` POSTs to the registry endpoint when `telemetry_endpoint` is configured. The reference implementation is [`receiver.py`](./receiver.py); production systems can replace it with anything that accepts this shape.

## Endpoint

```
POST <telemetry_endpoint>     # e.g. https://nie-registry.example.com/v1/audit
Content-Type: application/json
Authorization: Bearer <secrets.telemetry_token>     # optional
```

Telemetry is **best-effort**. The CI job logs a warning on non-2xx response but never fails the build.

## Payload

```jsonc
{
  // From GitHub Actions context
  "repo":      "Nie/ems-portal",       // GITHUB_REPOSITORY
  "ref":       "refs/heads/main",      // GITHUB_REF
  "sha":       "a1b2c3...",             // GITHUB_SHA
  "runId":     "1234567890",            // GITHUB_RUN_ID
  "timestamp": "2026-05-03T12:00:00Z",

  // From .nie-template-version.json in the derived repo
  "templateVersion": "2026.04.28.2",
  "appliedTasks":    ["0001", "0004", "0005", "0006", "0007", "0008", "0009"],

  // From .copier-answers.yml (if scaffolded via Copier)
  "answers": {
    "project_name":  "ems-portal",
    "organization":  "Nie",
    "stack":         "full",
    "include_chat":  false,
    "include_workflow": true,
    "include_pdf":   false,
    "include_storage": true,
    "include_myinfo": false
  },

  // Summary from audit.py --json
  "audit": {
    "passed":      62,
    "total":       68,
    "hasCritical": false,
    "failures":    [
      {"name": "feature 'pdf-generation' has files.md", "category": "features",
       "passed": false, "critical": false, "remediation": ["..."]}
    ]
  }
}
```

## Hard limits

| Field | Limit |
|---|---|
| Total payload size | 1 MB (the reference receiver rejects > 1MB with HTTP 413) |
| `audit.failures` length | unbounded (capped indirectly by audit.py rule count) |

## What the registry should expose

At minimum, two read endpoints:

```
GET /v1/audit                # latest snapshot per repo (one row per derived repo)
GET /v1/audit/<repo-slug>    # full history for one repo
```

Useful aggregates a dashboard can compute from these:

- Repos behind the latest `templateVersion`
- Repos with `hasCritical: true`
- Adoption % of any specific task across the fleet (e.g. `0005-add-security-headers-middleware`)
- Drift heatmap: which feature dossiers are missing where

## Authentication

Bearer-token via `Authorization: Bearer <token>`. Configured via:

- **CI side** (template caller): `secrets.telemetry_token` passed to the reusable workflow
- **Receiver side** (reference): `REGISTRY_TOKEN` env var; if unset, auth is off

Production should use a real auth provider (OIDC, mTLS, signed payloads). The reference single-token check is for piloting only.

## Privacy notes

`.copier-answers.yml` may contain free-form fields if a derived repo customised `copier.yml`. Make sure no secrets land there before enabling telemetry. The audit JSON includes file paths, never file contents.

## Compatibility

Schema changes that ADD fields are backwards-compatible. Removing or renaming a field is a breaking change — bump the URL version (`/v1/audit` → `/v2/audit`) and run both for a deprecation window.
