<script setup lang="ts">
/**
 * ChatView — main AI chat page with sidebar layout (from CDB chatbot pattern).
 */
import { computed, ref, onMounted, nextTick, watch } from "vue";
import { useRoute } from "vue-router";
import { useToast } from "@nietemplate/ui";
import ChatSidebar from "@/components/chat/ChatSidebar.vue";
import ChatMessageBubble from "@/components/chat/ChatMessageBubble.vue";
import ChatInputBox from "@/components/chat/ChatInputBox.vue";
import chatService, {
  type Conversation,
  type ChatMessage,
  type StreamEvent,
} from "@/services/chatService";

const route = useRoute();
const toast = useToast();

const conversations = ref<Conversation[]>([]);
const messages = ref<ChatMessage[]>([]);
const currentConversation = ref<Conversation | null>(null);
const loadingConv = ref(false);
const loadingMsgs = ref(false);
const streaming = ref(false);
const streamingContent = ref("");
const messagesContainer = ref<HTMLElement>();

const source = computed(() => (route.params.source as string) || "procurement");

const loadConversations = async () => {
  loadingConv.value = true;
  try {
    conversations.value = await chatService.getConversations(source.value);
  } catch (e) {
    toast.error("Failed to load conversations");
  } finally {
    loadingConv.value = false;
  }
};

const selectConversation = async (conv: Conversation) => {
  currentConversation.value = conv;
  loadingMsgs.value = true;
  try {
    messages.value = await chatService.getMessages(conv.id);
    await nextTick();
    scrollToBottom();
  } catch (e) {
    toast.error("Failed to load messages");
  } finally {
    loadingMsgs.value = false;
  }
};

const newConversation = async () => {
  try {
    const conv = await chatService.createConversation(source.value, "New Chat");
    conversations.value.unshift(conv);
    await selectConversation(conv);
  } catch (e) {
    toast.error("Failed to create conversation");
  }
};

const deleteConversation = async (conv: Conversation) => {
  try {
    await chatService.deleteConversation(conv.id);
    conversations.value = conversations.value.filter((c) => c.id !== conv.id);
    if (currentConversation.value?.id === conv.id) {
      currentConversation.value = null;
      messages.value = [];
    }
  } catch (e) {
    toast.error("Failed to delete conversation");
  }
};

const renameConversation = async (conv: Conversation, newTitle: string) => {
  try {
    await chatService.renameConversation(conv.id, newTitle);
    conv.title = newTitle;
  } catch (e) {
    toast.error("Failed to rename");
  }
};

const sendMessage = async (content: string) => {
  if (!currentConversation.value) await newConversation();
  if (!currentConversation.value) return;

  messages.value.push({
    id: Date.now(),
    role: "user",
    content,
    createdAt: new Date().toISOString(),
    conversationId: currentConversation.value.id,
  });
  streaming.value = true;
  streamingContent.value = "";

  try {
    const response = await chatService.sendMessageStream(
      currentConversation.value.id,
      content,
    );
    const reader = response.body!.getReader();
    const decoder = new TextDecoder();

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      const text = decoder.decode(value);
      const lines = text.split("\n");
      for (const line of lines) {
        if (line.startsWith("data: ")) {
          const data = line.slice(6);
          if (data === "[DONE]") break;
          try {
            const parsed = JSON.parse(data);
            streamingContent.value += parsed.content;
          } catch {
            streamingContent.value += data;
          }
        }
      }
    }

    messages.value.push({
      id: Date.now() + 1,
      role: "assistant",
      content: streamingContent.value,
      createdAt: new Date().toISOString(),
      conversationId: currentConversation.value.id,
    });
  } catch (e) {
    toast.error("Failed to send message");
  } finally {
    streaming.value = false;
    streamingContent.value = "";
    await nextTick();
    scrollToBottom();
    loadConversations();
  }
};

const scrollToBottom = () => {
  const el = messagesContainer.value;
  if (el) el.scrollTop = el.scrollHeight;
};

onMounted(() => loadConversations());
</script>

<template>
  <div class="chat-view">
    <div class="main-area">
      <!-- Welcome / empty state -->
      <div v-if="!currentConversation" class="welcome">
        <h1>💬 AI Assistant</h1>
        <p>
          Ask questions about procurement, vendors, orders, or any system data.
        </p>
        <div class="suggestions">
          <button
            class="suggestion-chip"
            @click="
              newConversation();
              sendMessage('What are my pending purchase orders?');
            "
          >
            📋 Pending Orders
          </button>
          <button
            class="suggestion-chip"
            @click="
              newConversation();
              sendMessage('Show me vendor performance summary');
            "
          >
            🏢 Vendor Summary
          </button>
          <button
            class="suggestion-chip"
            @click="
              newConversation();
              sendMessage('What is the total spend this month?');
            "
          >
            💰 Monthly Spend
          </button>
          <button
            class="suggestion-chip"
            @click="
              newConversation();
              sendMessage('Explain the approval workflow');
            "
          >
            🔄 Approval Workflow
          </button>
        </div>
        <ChatInputBox
          :disabled="streaming"
          @send="
            newConversation();
            sendMessage($event);
          "
        />
      </div>

      <!-- Active conversation -->
      <template v-else>
        <div class="conversation-header">
          <h2>{{ currentConversation.title }}</h2>
          <span class="source-badge">{{ currentConversation.source }}</span>
        </div>
        <div ref="messagesContainer" class="messages-container">
          <div v-if="loadingMsgs" class="loading">Loading messages...</div>
          <ChatMessageBubble
            v-for="msg in messages"
            :key="msg.id"
            :message="msg"
          />
          <div v-if="streaming" class="streaming-message">
            <ChatMessageBubble
              :message="{
                id: 0,
                role: 'assistant',
                content: streamingContent,
                createdAt: new Date().toISOString(),
                conversationId: 0,
              }"
            />
          </div>
        </div>
        <ChatInputBox :disabled="streaming" @send="sendMessage" />
      </template>
    </div>
  </div>
</template>

<style scoped>
.chat-view {
  display: flex;
  height: calc(100vh - 60px);
}
.main-area {
  flex: 1;
  display: flex;
  flex-direction: column;
  max-width: 800px;
  margin: 0 auto;
  width: 100%;
}
.welcome {
  text-align: center;
  padding: 60px 20px;
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
}
.welcome h1 {
  font-size: 28px;
  margin-bottom: 8px;
}
.welcome p {
  color: #6b7280;
  margin-bottom: 24px;
}
.suggestions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  justify-content: center;
  margin-bottom: 24px;
}
.suggestion-chip {
  padding: 8px 16px;
  border-radius: 20px;
  border: 1px solid #d1d5db;
  background: white;
  cursor: pointer;
  font-size: 13px;
  transition: all 0.15s;
}
.suggestion-chip:hover {
  border-color: #3b82f6;
  background: #eff6ff;
}
.conversation-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  border-bottom: 1px solid #e5e7eb;
}
.conversation-header h2 {
  font-size: 16px;
  margin: 0;
  flex: 1;
}
.source-badge {
  padding: 2px 10px;
  border-radius: 12px;
  background: #eff6ff;
  color: #3b82f6;
  font-size: 11px;
  font-weight: 600;
}
.messages-container {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
}
.loading {
  text-align: center;
  padding: 40px;
  color: #9ca3af;
}
</style>
