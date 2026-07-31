<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";

interface Props {
  modelValue: boolean;
  disabled?: boolean;
  label?: string;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
});

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
}>();

const toggle = () => {
  if (!props.disabled) {
    emit("update:modelValue", !props.modelValue);
  }
};

const switchClasses = computed(() =>
  cn(
    "relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2",
    props.modelValue ? "bg-primary-600" : "bg-secondary-200 dark:bg-secondary-700",
    props.disabled ? "opacity-50 cursor-not-allowed" : "",
    props.class
  )
);

const knobClasses = computed(() =>
  cn(
    "pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out",
    props.modelValue ? "translate-x-5" : "translate-x-0"
  )
);
</script>

<template>
  <label class="inline-flex items-center gap-3">
    <button
      type="button"
      role="switch"
      :aria-checked="modelValue"
      :class="switchClasses"
      :disabled="disabled"
      @click="toggle"
    >
      <span :class="knobClasses"></span>
    </button>
    <span v-if="label" class="text-sm text-secondary-700 dark:text-secondary-300">
      {{ label }}
    </span>
  </label>
</template>
