<script setup lang="ts">
import { ref } from "vue";
import type { Conversation } from "../../services/chatService";

const props = defineProps<{
  conversations: Conversation[];
  currentConversationId?: number;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  (e: "select", conversation: Conversation): void;
  (e: "new"): void;
  (e: "delete", conversation: Conversation): void;
  (e: "rename", conversation: Conversation, newTitle: string): void;
}>();

const editingId = ref<number | null>(null);
const editTitle = ref("");
const menuOpenId = ref<number | null>(null);

const startEdit = (conv: Conversation) => {
  editingId.value = conv.id;
  editTitle.value = conv.title || "";
  menuOpenId.value = null;
};

const saveEdit = (conv: Conversation) => {
  if (editTitle.value.trim()) emit("rename", conv, editTitle.value.trim());
  editingId.value = null;
};

const formatDate = (dateStr: string) => {
  const d = new Date(dateStr);
  const now = new Date();
  const diff = Math.floor(
    (now.getTime() - d.getTime()) / (1000 * 60 * 60 * 24),
  );
  if (diff <= 0) return "Today";
  if (diff === 1) return "Yesterday";
  if (diff < 7) return `${diff}d ago`;
  return d.toLocaleDateString();
};
</script>

<template>
  <div class="chat-sidebar">
    <button class="new-chat-btn" @click="emit('new')">+ New Chat</button>
    <div class="conversations-list">
      <div v-if="isLoading" class="loading">Loading...</div>
      <div v-else-if="conversations.length === 0" class="empty">
        No conversations yet
      </div>
      <div
        v-for="conv in conversations"
        :key="conv.id"
        class="conv-item"
        :class="{ active: conv.id === currentConversationId }"
        @click="emit('select', conv)"
      >
        <template v-if="editingId === conv.id">
          <input
            v-model="editTitle"
            class="edit-input"
            @click.stop
            @keyup.enter="saveEdit(conv)"
            @keyup.escape="editingId = null"
            @blur="saveEdit(conv)"
            autofocus
          />
        </template>
        <template v-else>
          <div class="conv-title">{{ conv.title }}</div>
          <div class="conv-meta">
            {{ formatDate(conv.lastMessageAt) }} · {{ conv.messageCount }} msgs
          </div>
          <button
            class="conv-menu-btn"
            @click.stop="menuOpenId = menuOpenId === conv.id ? null : conv.id"
          >
            ⋯
          </button>
          <div v-if="menuOpenId === conv.id" class="conv-menu">
            <button @click.stop="startEdit(conv)">✏️ Rename</button>
            <button @click.stop="emit('delete', conv)">🗑️ Delete</button>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.chat-sidebar {
  width: 260px;
  border-right: 1px solid #e5e7eb;
  display: flex;
  flex-direction: column;
  background: #fafafa;
  flex-shrink: 0;
}
.new-chat-btn {
  margin: 12px;
  padding: 10px;
  border-radius: 8px;
  border: 1px solid #d1d5db;
  background: white;
  cursor: pointer;
  font-size: 13px;
  font-weight: 600;
}
.new-chat-btn:hover {
  border-color: #3b82f6;
}
.conversations-list {
  flex: 1;
  overflow-y: auto;
  padding: 0 8px;
}
.conv-item {
  padding: 10px 12px;
  border-radius: 8px;
  cursor: pointer;
  position: relative;
  margin-bottom: 2px;
}
.conv-item:hover,
.conv-item.active {
  background: #f3f4f6;
}
.conv-title {
  font-size: 13px;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  padding-right: 24px;
}
.conv-meta {
  font-size: 11px;
  color: #9ca3af;
  margin-top: 2px;
}
.conv-menu-btn {
  position: absolute;
  right: 8px;
  top: 8px;
  background: none;
  border: none;
  cursor: pointer;
  font-size: 14px;
  color: #9ca3af;
  padding: 2px 6px;
}
.conv-menu {
  position: absolute;
  right: 8px;
  top: 32px;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  z-index: 10;
  overflow: hidden;
}
.conv-menu button {
  display: block;
  width: 100%;
  padding: 8px 16px;
  border: none;
  background: none;
  cursor: pointer;
  font-size: 12px;
  text-align: left;
}
.conv-menu button:hover {
  background: #f3f4f6;
}
.edit-input {
  width: 100%;
  padding: 4px 8px;
  border: 1px solid #3b82f6;
  border-radius: 4px;
  font-size: 13px;
}
</style>
