# Feedback Widget — Do and Don't

## DO ✅

1. **DO** keep the widget mounted exactly once, inside `StaffLayout.vue`. Adding a second mount on a child page double-renders the floating button.
2. **DO** namespace the `function_id` to `<project-slug>.<routeName>`. The current `procurement.${routeName}` is a known issue that derived projects MUST replace as part of task 0003. The namespace is what the central feedback service uses to aggregate, so a clean prefix matters.
3. **DO** use `route.name` (string identifier) — NOT `route.path` — for the `<routeName>` segment. Path can change between deployments; the route name is stable in `src/frontend/main/src/router/index.ts`.
4. **DO** keep the rating values as the literal strings `"1"` and `"5"`. The receiver expects exactly these two values; introducing `"3"` (a "neutral") changes the contract.
5. **DO** reuse `NieAppFeedbackHub` from `@nietemplate/ui` for any inline (non-floating) feedback surface — for example a "rate this report" panel inside a report page. The visual remains consistent.
6. **DO** keep `FEEDBACK_HIDE_TTL_MS = 24 * 60 * 60 * 1000` (24 hours) — short enough to recapture sentiment after meaningful page changes, long enough to avoid annoying users. Coordinate with product before changing.
7. **DO** call `feedbackService.submit({ function_id, rating, feedback, page })` with `page = window.location.href`. The full URL gives the receiver the query string for context (e.g. which entity ID a comment was about).
8. **DO** show a brief toast and close the popover on success. Stay open and surface `submitError` on failure so the user can retry.
9. **DO** clear `reappearTimer` on `onUnmounted` to prevent setTimeout leaks. The component already does this; preserve it on edits.
10. **DO** test the widget on every route. The route-name-derived `function_id` should produce a stable string per page; deep-linking back to a page after submit should still hide the widget per the localStorage entry.

## DON'T ❌

1. **DON'T** make the widget removable. It is part of the org-wide tech roadmap; every NIE staff app surfaces it. If product wants it disabled per environment, gate it on a config flag (e.g. `VITE_ENABLE_FEEDBACK = "true"`) but keep the code in place.
2. **DON'T** put PII or sensitive identifiers in `function_id`. The function_id is logged on the receiver side and may flow into dashboards. Stick to route-name + project slug.
3. **DON'T** submit on every keystroke or rating click. Only the explicit "Submit" button triggers `feedbackService.submit`. The popover should otherwise be lossless.
4. **DON'T** push feedback into the shared session — sessions are for auth state only. The 24-hour hide is per-browser, per-device via `localStorage`. That's intentional.
5. **DON'T** wrap `feedbackService.submit` in a global retry loop. A failed submission shows an error and the user can decide to retry — silent retries can spam the receiver if the user navigates away mid-failure.
6. **DON'T** auto-open the popover on page load. The widget is opt-in (user clicks the button); auto-popups are user-hostile.
7. **DON'T** bind the widget to a Pinia store. The state is local and short-lived; a store adds complexity without benefit.
8. **DON'T** swallow exceptions from `feedbackService.submit` — the popover renders `submitError`; surfacing the error is the contract. Wrapping in try/catch + ignoring is wrong.
9. **DON'T** style the widget with arbitrary Tailwind classes that compete with `NieAppFeedbackHub` — the hub is the design source. Override colors only via the theme tokens defined in `@nietemplate/ui`.
10. **DON'T** mount the widget inside the auth layout (`src/frontend/auth/`) — feedback is for authenticated staff pages. Pre-login, it has no `userId` to attribute.
