<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from "vue";
import feedbackService from "@/services/feedbackService";
import type { FeedbackRating } from "@/services/feedbackService";

interface Props {
  functionId: string;
  questionText?: string;
  placement?: "left" | "right";
}

const props = withDefaults(defineProps<Props>(), {
  questionText: "How is your experience with this page?",
  placement: "right",
});

const FEEDBACK_STORAGE_PREFIX = "procurement.feedback.submittedAt.";
const FEEDBACK_HIDE_TTL_MS = 24 * 60 * 60 * 1000;

const isPopupOpen = ref(false);
const selectedRating = ref<FeedbackRating | null>(null);
const additionalFeedback = ref("");
const isSubmitting = ref(false);
const submitError = ref<string | null>(null);
const toastMessage = ref<string | null>(null);
const isHiddenForSession = ref(false);

let toastTimer: ReturnType<typeof setTimeout> | null = null;
let reappearTimer: ReturnType<typeof setTimeout> | null = null;

function storageKey(functionId: string) {
  return `${FEEDBACK_STORAGE_PREFIX}${functionId}`;
}

function syncHiddenState() {
  const key = storageKey(props.functionId);
  if (reappearTimer) {
    clearTimeout(reappearTimer);
    reappearTimer = null;
  }

  const raw = localStorage.getItem(key);
  if (!raw) {
    isHiddenForSession.value = false;
    return;
  }

  const submittedAt = Number(raw);
  if (!Number.isFinite(submittedAt) || submittedAt <= 0) {
    localStorage.removeItem(key);
    isHiddenForSession.value = false;
    return;
  }

  const remaining = FEEDBACK_HIDE_TTL_MS - (Date.now() - submittedAt);
  if (remaining > 0) {
    isHiddenForSession.value = true;
    reappearTimer = setTimeout(() => syncHiddenState(), remaining + 50);
  } else {
    localStorage.removeItem(key);
    isHiddenForSession.value = false;
  }
}

const canSubmit = computed(() => !!selectedRating.value && !isSubmitting.value);

const popoverAlignClass = computed(() =>
  props.placement === "left" ? "left-0" : "right-0",
);

function togglePopup() {
  if (isHiddenForSession.value) return;
  isPopupOpen.value = !isPopupOpen.value;
  submitError.value = null;
}

function closePopup() {
  isPopupOpen.value = false;
}

function selectRating(rating: FeedbackRating) {
  selectedRating.value = rating;
  submitError.value = null;
}

async function handleSubmit() {
  if (!selectedRating.value) return;

  isSubmitting.value = true;
  submitError.value = null;

  try {
    await feedbackService.submit({
      function_id: props.functionId,
      rating: selectedRating.value,
      feedback: additionalFeedback.value.trim(),
      page: window.location.href,
    });

    isPopupOpen.value = false;
    selectedRating.value = null;
    additionalFeedback.value = "";
    submitError.value = null;

    localStorage.setItem(storageKey(props.functionId), String(Date.now()));
    syncHiddenState();

    toastMessage.value =
      "Thank you! Your feedback helps us improve this application.";
    if (toastTimer) clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
      toastMessage.value = null;
      toastTimer = null;
    }, 3500);
  } catch {
    submitError.value = "Failed to submit feedback. Please try again.";
  } finally {
    isSubmitting.value = false;
  }
}

function onDocumentClick(event: MouseEvent) {
  if (!isPopupOpen.value) return;
  const root = document.getElementById("floating-feedback-root");
  if (root && !root.contains(event.target as Node)) {
    closePopup();
  }
}

function onKeyDown(event: KeyboardEvent) {
  if (event.key === "Escape") closePopup();
}

onMounted(() => {
  syncHiddenState();
  document.addEventListener("click", onDocumentClick, true);
  document.addEventListener("keydown", onKeyDown);
});

watch(
  () => props.functionId,
  () => {
    syncHiddenState();
    closePopup();
  },
);

onUnmounted(() => {
  document.removeEventListener("click", onDocumentClick, true);
  document.removeEventListener("keydown", onKeyDown);
  if (toastTimer) clearTimeout(toastTimer);
  if (reappearTimer) clearTimeout(reappearTimer);
});
</script>

<template>
  <div
    v-if="!isHiddenForSession || toastMessage || isPopupOpen"
    id="floating-feedback-root"
    class="fixed bottom-6 z-50 md:bottom-8"
    :class="placement === 'left' ? 'left-6 md:left-8' : 'right-6 md:right-8'"
  >
    <div class="relative">
      <!-- Trigger button -->
      <button
        v-if="!isHiddenForSession"
        type="button"
        class="flex h-11 w-11 items-center justify-center rounded-full border border-gray-300/50 bg-white/95 shadow-lg transition-all hover:scale-105 hover:bg-gray-50 dark:border-gray-600/50 dark:bg-gray-700/95 dark:hover:bg-gray-600/95 md:h-12 md:w-12"
        aria-label="Open feedback"
        :aria-expanded="isPopupOpen"
        @click.stop="togglePopup"
      >
        <span
          class="material-symbols-outlined text-[22px] text-gray-700 dark:text-gray-200"
        >
          thumb_up
        </span>
      </button>

      <!-- Success toast -->
      <div
        v-if="toastMessage"
        class="absolute bottom-full mb-3 w-[22rem] max-w-[calc(100vw-3rem)] rounded-xl border border-gray-200 bg-white px-4 py-3 shadow-2xl ring-2 ring-blue-600/20 dark:border-gray-700 dark:bg-gray-800"
        :class="popoverAlignClass"
        role="status"
        aria-live="polite"
      >
        <p class="text-sm font-semibold text-gray-900 dark:text-white">
          {{ toastMessage }}
        </p>
      </div>

      <!-- Feedback popup -->
      <div
        v-if="isPopupOpen"
        class="absolute bottom-full mb-3 w-[22rem] max-w-[calc(100vw-3rem)] rounded-2xl border border-gray-200 bg-white p-5 shadow-2xl dark:border-gray-700 dark:bg-gray-800"
        :class="popoverAlignClass"
        @click.stop
      >
        <!-- Header -->
        <div class="flex items-start justify-between gap-3">
          <div class="flex flex-col gap-0.5">
            <p
              class="text-xs font-semibold uppercase tracking-wide text-gray-600 dark:text-gray-300"
            >
              Feedback
            </p>
            <p class="text-sm font-semibold text-gray-900 dark:text-white">
              {{ questionText }}
            </p>
          </div>
          <button
            type="button"
            class="text-gray-700 hover:text-gray-900 dark:text-gray-300 dark:hover:text-white"
            aria-label="Close feedback"
            @click="closePopup"
          >
            <span class="material-symbols-outlined text-[20px]">close</span>
          </button>
        </div>

        <!-- Rating -->
        <div class="mt-4">
          <p
            class="mb-2 text-xs font-semibold text-gray-700 dark:text-gray-300"
          >
            Rating
          </p>
          <p class="mb-3 text-xs text-gray-600 dark:text-gray-400">
            Tap a rating to continue.
          </p>
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="flex h-10 w-10 items-center justify-center rounded-xl border text-xl transition-colors"
              :class="
                selectedRating === '5'
                  ? 'border-indigo-400 bg-indigo-50 dark:border-indigo-500 dark:bg-indigo-900/30'
                  : 'border-gray-200 hover:bg-gray-50 dark:border-gray-700 dark:hover:bg-gray-900'
              "
              :disabled="isSubmitting"
              aria-label="Thumbs up"
              @click="selectRating('5')"
            >
              👍
            </button>
            <button
              type="button"
              class="flex h-10 w-10 items-center justify-center rounded-xl border text-xl transition-colors"
              :class="
                selectedRating === '1'
                  ? 'border-red-400 bg-red-50 dark:border-red-500 dark:bg-red-900/30'
                  : 'border-gray-200 hover:bg-gray-50 dark:border-gray-700 dark:hover:bg-gray-900'
              "
              :disabled="isSubmitting"
              aria-label="Thumbs down"
              @click="selectRating('1')"
            >
              👎
            </button>
          </div>
        </div>

        <!-- Additional Feedback -->
        <div class="mt-4 border-t border-gray-100 pt-4 dark:border-gray-700">
          <p
            class="mb-2 text-xs font-semibold text-gray-700 dark:text-gray-300"
          >
            Additional Feedback
          </p>
          <textarea
            v-model="additionalFeedback"
            rows="3"
            class="w-full rounded-xl border border-gray-200 bg-white p-3 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-600/30 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-100"
            placeholder="Tell us what you think..."
            :disabled="isSubmitting"
          />
          <p class="mt-2 text-xs text-gray-600 dark:text-gray-400">
            ⚠️ Please refrain from entering any sensitive or personal
            information.
          </p>
        </div>

        <!-- Submit -->
        <div
          class="mt-4 flex items-start gap-3 border-t border-gray-100 pt-4 dark:border-gray-700"
        >
          <button
            type="button"
            class="rounded-xl bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
            :disabled="!canSubmit"
            @click="handleSubmit"
          >
            {{ isSubmitting ? "Submitting..." : "Submit" }}
          </button>
          <p v-if="submitError" class="flex-1 text-sm text-red-600">
            {{ submitError }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
