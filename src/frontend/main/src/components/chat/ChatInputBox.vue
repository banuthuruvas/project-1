<script setup lang="ts">
/**
 * ChatInputBox — message input with send button and stop button for streaming.
 */
import { ref } from "vue";

defineProps<{ disabled?: boolean }>();

const emit = defineEmits<{
  (e: "send", content: string): void;
  (e: "stop"): void;
}>();

const input = ref("");

const handleSend = () => {
  const content = input.value.trim();
  if (!content) return;
  emit("send", content);
  input.value = "";
};

const handleKeydown = (e: KeyboardEvent) => {
  if (e.key === "Enter" && !e.shiftKey) {
    e.preventDefault();
    handleSend();
  }
};
</script>

<template>
  <div class="chat-input-box">
    <textarea
      v-model="input"
      placeholder="Type a message..."
      :disabled="disabled"
      @keydown="handleKeydown"
      rows="1"
    />
    <button
      v-if="!disabled"
      class="send-btn"
      @click="handleSend"
      :disabled="!input.trim()"
    >
      ➤
    </button>
    <button v-else class="stop-btn" @click="emit('stop')">⏹ Stop</button>
  </div>
</template>

<style scoped>
.chat-input-box {
  display: flex;
  gap: 8px;
  padding: 16px;
  border-top: 1px solid #e5e7eb;
  align-items: flex-end;
}
textarea {
  flex: 1;
  padding: 10px 14px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 14px;
  resize: none;
  font-family: inherit;
  max-height: 120px;
}
textarea:focus {
  outline: none;
  border-color: #3b82f6;
}
.send-btn {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border: none;
  background: #3b82f6;
  color: white;
  cursor: pointer;
  font-size: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.send-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.stop-btn {
  padding: 8px 16px;
  border-radius: 6px;
  border: 1px solid #ef4444;
  background: white;
  color: #ef4444;
  cursor: pointer;
  font-size: 13px;
}
</style>
