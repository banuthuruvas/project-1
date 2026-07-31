# AI Chatbot (pgvector + Streaming)

> **Status:** `optional` | **Source:** CDB chatbot layout + isaac-adm pgvector

## Overview

AI-powered chat interface with SSE streaming, conversation management, and pgvector-based semantic search. Supports optional LLM backends (Azure OpenAI, AWS Bedrock).

## Key Files

- `Domain/Models/ChatEntities.cs` — ChatConversation, ChatMessage, ChatEmbedding (pgvector)
- `Services/Chat/IChatService.cs` — Conversations, messages, streaming
- `Services/Chat/ChatService.cs` — Implementation with placeholder LLM
- `API/Controllers/ChatController.cs` — REST + SSE streaming endpoint
- `pages/chat/ChatView.vue` — Full chat page with sidebar
- `components/chat/ChatSidebar.vue` — Conversation list
- `components/chat/ChatMessageBubble.vue` — Message bubble
- `components/chat/ChatInputBox.vue` — Input with send/stop
- `services/chatService.ts` — API client with SSE streaming
- `build/appsettings.api.json` — AI config section

## pgvector Setup

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```
