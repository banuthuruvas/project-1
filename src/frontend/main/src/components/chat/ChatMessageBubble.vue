<script setup lang="ts">
/**
 * ChatMessageBubble — single chat message with role-based styling.
 */
import { computed } from "vue";
import type { ChatMessage } from "@/services/chatService";

const props = defineProps<{ message: ChatMessage }>();

const isUser = computed(() => props.message.role === "user");
const isAssistant = computed(() => props.message.role === "assistant");

const formatTime = (dateStr: string) => {
  const d = new Date(dateStr);
  return d.toLocaleTimeString("en-SG", { hour: "2-digit", minute: "2-digit" });
};
</script>

<template>
  <div class="message-bubble" :class="{ user: isUser, assistant: isAssistant }">
    <div class="bubble-avatar">{{ isUser ? "👤" : "🤖" }}</div>
    <div class="bubble-content">
      <div class="bubble-text" v-text="message.content"></div>
      <div class="bubble-time">{{ formatTime(message.createdAt) }}</div>
    </div>
  </div>
</template>

<style scoped>
.message-bubble {
  display: flex;
  gap: 10px;
  padding: 8px 0;
}
.message-bubble.user {
  flex-direction: row-reverse;
}
.message-bubble.assistant {
  flex-direction: row;
}
.bubble-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
  background: #f3f4f6;
}
.message-bubble.user .bubble-avatar {
  background: #dbeafe;
}
.bubble-content {
  max-width: 75%;
}
.message-bubble.user .bubble-content {
  text-align: right;
}
.bubble-text {
  padding: 10px 14px;
  border-radius: 12px;
  font-size: 14px;
  line-height: 1.5;
  white-space: pre-wrap;
}
.message-bubble.assistant .bubble-text {
  background: #f3f4f6;
  color: #1f2937;
  border-bottom-left-radius: 4px;
}
.message-bubble.user .bubble-text {
  background: #3b82f6;
  color: white;
  border-bottom-right-radius: 4px;
}
.bubble-time {
  font-size: 10px;
  color: #9ca3af;
  margin-top: 4px;
  padding: 0 4px;
}
</style>
