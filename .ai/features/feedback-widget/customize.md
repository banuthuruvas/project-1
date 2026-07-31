# Feedback Widget — Customize

## 1. Replace the procurement namespace with your project namespace (task 0003)

Pick a short slug (e.g. `i3g`, `safeguard`, `myportal`) — this is the central-aggregator key.

1. Edit `src/frontend/main/src/staff/layouts/StaffLayout.vue:67-69`:
   ```ts
   const feedbackFunctionId = computed(
     () => `i3g.${String(route.name ?? "page")}`,   // was: `procurement.${...}`
   );
   ```
2. Edit `src/frontend/main/src/components/feedback/FloatingFeedbackButton.vue:17`:
   ```ts
   const FEEDBACK_STORAGE_PREFIX = "i3g.feedback.submittedAt.";   // was: "procurement.feedback.submittedAt."
   ```
3. Both edits MUST happen together — the layout sets the function_id, the button uses it as the storage key. Mismatched namespaces silently break the 24-hour hide.
4. Existing entries in `localStorage` under the old prefix become stale. Either accept that the widget will re-appear on every page once after the rename, or add a one-time migration in `FloatingFeedbackButton.vue.onMounted` that copies old keys to new keys.

## 2. Move the widget to the bottom-LEFT

1. Edit `StaffLayout.vue` near the `<FloatingFeedbackButton ... />` line. Pass `placement="left"`:
   ```html
   <FloatingFeedbackButton :function-id="feedbackFunctionId" placement="left" />
   ```
2. The component already supports this via `withDefaults(defineProps<Props>(), { placement: "right" })` and the `popoverAlignClass` computed property.

## 3. Disable the widget for a specific environment

1. Add an env flag in your `.env.production`:
   ```
   VITE_ENABLE_FEEDBACK=false
   ```
2. Edit `StaffLayout.vue` — wrap the mount:
   ```html
   <FloatingFeedbackButton
     v-if="import.meta.env.VITE_ENABLE_FEEDBACK !== 'false'"
     :function-id="feedbackFunctionId"
   />
   ```
3. Default behavior stays "enabled" because the flag must be EXPLICITLY `"false"` to hide the widget — keeps the org-wide default safe.

## 4. Replace the question text per page

The component accepts a `questionText` prop:

```html
<FloatingFeedbackButton
  :function-id="feedbackFunctionId"
  question-text="How was the new approval flow?"
/>
```

Pass a different string when mounted on a specific layout (e.g. one for the reports area). Default is "How is your experience with this page?".

## 5. Lengthen / shorten the 24-hour hide

1. Edit `FloatingFeedbackButton.vue:18`:
   ```ts
   const FEEDBACK_HIDE_TTL_MS = 7 * 24 * 60 * 60 * 1000; // 7 days
   ```
2. Restart the dev server. Existing localStorage entries continue to use the OLD TTL until they expire (they were stamped with `Date.now()` on submit; only the comparison TTL changed).

## 6. Add a backend proxy for `/api/Feedback/Submit`

The widget posts to `/api/Feedback/Submit` (relative). If your project does not yet have a controller for this:

1. Create `src/backend/API/Controllers/FeedbackController.cs` with a single `Submit` action that receives `{ function_id, rating, feedback, page }`, attaches the `IUserContextService.UserId`, and forwards to the central NIE feedback collector via `IHttpClientFactory`.
2. Decide auth: typically `[AllowAnonymous]` is wrong (we want to attribute), so leave it under the default session-validation gate.
3. Do NOT add a `[RequireAccessFunction]` for it — every authenticated staff member should be able to submit.
4. Audit log it via `IAuditLogger.LogAsync(EAuditAction.SystemEvent, EAuditCategory.System, "Feedback", function_id)` if compliance asks.

## 7. Embed the inline `NieAppFeedbackHub` on a specific page

```vue
<script setup lang="ts">
import { NieAppFeedbackHub } from "@nietemplate/ui";
import feedbackService from "@/services/feedbackService";

async function onSubmit(payload: { rating: "1" | "5"; feedback: string }) {
  await feedbackService.submit({
    function_id: "i3g.report-x.inline",
    rating: payload.rating,
    feedback: payload.feedback,
    page: window.location.href,
  });
}
</script>

<template>
  <NieAppFeedbackHub @submit="onSubmit" />
</template>
```

The inline hub does NOT carry the 24-hour hide logic — that lives in `FloatingFeedbackButton`. Add your own local state if you want a "thanks" message after submit.
