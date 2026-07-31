<script setup lang="ts">
interface Props {
  show?: boolean;
  message?: string;
  fullscreen?: boolean;
}

withDefaults(defineProps<Props>(), {
  show: true,
  message: "Loading...",
  fullscreen: false,
});
</script>

<template>
  <Transition name="fade">
    <div
      v-if="show"
      :class="[
        'flex items-center justify-center bg-white/80 dark:bg-secondary-900/80 backdrop-blur-sm',
        fullscreen ? 'fixed inset-0 z-50' : 'absolute inset-0 rounded-lg',
      ]"
    >
      <div class="flex flex-col items-center gap-3">
        <svg
          class="h-10 w-10 animate-spin text-primary-600"
          fill="none"
          viewBox="0 0 24 24"
        >
          <circle
            class="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            stroke-width="4"
          ></circle>
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          ></path>
        </svg>
        <p v-if="message" class="text-sm text-secondary-600 dark:text-secondary-400">
          {{ message }}
        </p>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
