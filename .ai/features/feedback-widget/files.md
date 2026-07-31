# Feedback Widget — File Map

## Owned files

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/frontend/main/src/components/feedback/FloatingFeedbackButton.vue` | Page widget | The mounted floating button. Owns popover open/close, rating selection (`FeedbackRating` from the service), submit lifecycle (`isSubmitting`, `submitError`), success toast, and the 24-hour `localStorage`-based hide window |
| `src/frontend/main/src/services/feedbackService.ts` | Service | Typed axios client. Exports `FeedbackRating = "1" \| "5"`, `FeedbackSubmitRequest`, `FeedbackSubmitResponse`, and a single `submit` function calling `POST /api/Feedback/Submit` |
| `src/frontend/packages/ui/src/components/composite/app-feedback/NieAppFeedbackHub.vue` | UI primitive | The reusable visual: rating + textarea + submit button. Used by `FloatingFeedbackButton.vue` so the same look-and-feel can be embedded inline elsewhere if needed |
| `src/frontend/packages/ui/src/components/composite/app-feedback/index.ts` | Barrel | Re-exports `NieAppFeedbackHub` from the `@nietemplate/ui` package so consumers can `import { NieAppFeedbackHub } from "@nietemplate/ui"` |

## Touched files

| Path | What it contains | Why must be touched |
| --- | --- | --- |
| `src/frontend/main/src/staff/layouts/StaffLayout.vue` | Imports `FloatingFeedbackButton`, defines `const feedbackFunctionId = computed(() => \`procurement.${String(route.name ?? "page")}\`)` (line 67-69), renders `<FloatingFeedbackButton :function-id="feedbackFunctionId" />` near line 1034 | Adding a new layout (e.g. a dedicated reports layout) means duplicating this mount; or factor it into a shared layout slot. Removing the widget means deleting these three locations |
| `src/frontend/packages/ui/src/index.ts` | Exports `./components/composite/app-feedback` (line 13) | Required to expose `NieAppFeedbackHub` from the UI library barrel |
| (External) Backend endpoint `/api/Feedback/Submit` | Owned by NIE central feedback service or a thin proxy controller in this Main API | The widget assumes a working endpoint at this path; if the project rolls its own controller, place it in `src/backend/API/Controllers/FeedbackController.cs` and gate it via `[AllowAnonymous]` or a simple `screen.feedback.submit` access function |

## Known issue (task 0003)

The `function_id` namespace is hardcoded as `procurement.<routeName>` in `StaffLayout.vue:67-69` and the storage prefix is `procurement.feedback.submittedAt.` in `FloatingFeedbackButton.vue:17`. This is a leftover from the procurement reference sample. Task `.ai/tasks/0003-remove-procurement-samples` (or its equivalent in derived repos) replaces both with the project's namespace.

## Migrations

None — the FE widget does not own any backend table.

## External dependencies

| Package | Where | Purpose |
| --- | --- | --- |
| `vue` | FE | Reactive primitives, lifecycle |
| `@nietemplate/ui` | FE | Source of `NieAppFeedbackHub` (peer dep) |
| `axios` (via `@/services/api`) | FE | HTTP client |
| `localStorage` (browser-native) | FE | 24-hour hide window |
