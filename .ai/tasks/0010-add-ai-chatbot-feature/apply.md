# Task 0010 — Add AI Chatbot Feature

> **Status:** scaffolded — opt-in feature, no derived repo will adopt automatically. Maintainer must finalize LLM provider wiring (Azure OpenAI / AWS Bedrock / placeholder) before promoting to a release.

> **Why:** Multiple derived repos rebuild a chat surface from scratch. This task ships a minimal, pluggable chatbot baseline (entities + service contract + SSE controller + Vue UI) so projects that need it can adopt it via one task instead of reverse-engineering CDB / isaac-adm.

## Pre-checks

```bash
test ! -f src/backend/Libraries/Services/Services/Chat/IChatService.cs \
  || { echo "Already added; skipping."; exit 0; }
```

## 1. Files to create

The canonical file list lives in [`.ai/features/ai-chatbot/files.md`](../../features/ai-chatbot/files.md) (write this dossier file when promoting the task — it does not exist at scaffold time). Copy these from the template repo at the matching template version:

```text
src/backend/Libraries/Domain/Models/ChatEntities.cs
src/backend/Libraries/Services/Services/Chat/IChatService.cs
src/backend/Libraries/Services/Services/Chat/ChatService.cs
src/backend/API/Controllers/ChatController.cs
src/frontend/main/src/pages/chat/ChatView.vue
src/frontend/main/src/components/chat/ChatSidebar.vue
src/frontend/main/src/components/chat/ChatMessageBubble.vue
src/frontend/main/src/components/chat/ChatInputBox.vue
src/frontend/main/src/services/chatService.ts
```

## 2. Files to edit

### `src/backend/API/Program.cs`

Register the service in DI alongside the other `Libraries.Services.Services.*` registrations.

```diff
+ builder.Services.AddScoped<IChatService, ChatService>();
```

**Why:** SSE streaming endpoint resolves `IChatService` per request.

### `src/backend/Libraries/Data/Data/NieTemplateDbContext.cs`

Add three `DbSet`s and configure pgvector for `ChatEmbedding`.

```diff
+ public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
+ public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
+ public DbSet<ChatEmbedding> ChatEmbeddings => Set<ChatEmbedding>();
…
+ // OnModelCreating
+ modelBuilder.Entity<ChatEmbedding>(b =>
+ {
+     b.HasIndex(x => x.ConversationId);
+     b.Property(x => x.Vector).HasColumnType("vector(1536)");
+ });
```

**Why:** pgvector requires explicit column type. Adjust dimension to match your embedding model.

### `src/backend/Libraries/Domain/Security/AccessFunctionCatalog.cs`

Add the chat permission keys so endpoints can be guarded with `[RequireAccessFunction("chat:read")]` etc.

```diff
+ public const string ChatRead   = "chat:read";
+ public const string ChatWrite  = "chat:write";
+ public const string ChatAdmin  = "chat:admin";
```

### `src/frontend/main/src/router/index.ts`

Register the chat route guarded by `chat:read`.

```diff
+ {
+   path: '/chat',
+   name: 'chat',
+   component: () => import('@/pages/chat/ChatView.vue'),
+   meta: { requiresAuth: true, accessFunction: 'chat:read' }
+ },
```

### `src/frontend/main/src/constants/permissions.ts`

Mirror the backend access functions on the frontend.

```diff
+ CHAT_READ:  'chat:read',
+ CHAT_WRITE: 'chat:write',
+ CHAT_ADMIN: 'chat:admin',
```

## 3. Database migration

Create a new EF Core migration after the edits above:

```bash
dotnet ef migrations add AddChatEntities \
  --project src/backend/Libraries/Data \
  --startup-project src/backend/API
```

Inspect the generated migration. **Required:** the `ChatEmbeddings.Vector` column must have type `vector(1536)` (or your embedding dimension), not `bytea`. If it does not, your DbContext config is missing — fix and regenerate.

Also enable the `vector` extension in the migration `Up()`:

```csharp
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
```

## 4. Verification

```bash
dotnet build src/backend/NieTemplate.sln
pnpm --filter main type-check
grep -n "IChatService" src/backend/API/Program.cs   # expect ≥1 match
```

Live smoke (after running the migration and starting both services):

```bash
curl -sN -H "Accept: text/event-stream" \
  http://localhost:5002/api/Chat/stream?conversationId=demo \
  | head -3
# Should emit SSE frames "event: message" + JSON payload
```

## 5. Rollback

```bash
git restore --staged --worktree \
  src/backend/Libraries/Domain/Models/ChatEntities.cs \
  src/backend/Libraries/Services/Services/Chat/ \
  src/backend/API/Controllers/ChatController.cs \
  src/frontend/main/src/pages/chat/ \
  src/frontend/main/src/components/chat/ \
  src/frontend/main/src/services/chatService.ts \
  src/backend/API/Program.cs \
  src/backend/Libraries/Data/Data/NieTemplateDbContext.cs \
  src/backend/Libraries/Domain/Security/AccessFunctionCatalog.cs \
  src/frontend/main/src/router/index.ts \
  src/frontend/main/src/constants/permissions.ts

# Drop the migration if generated
dotnet ef migrations remove \
  --project src/backend/Libraries/Data \
  --startup-project src/backend/API
```

## Maintainer review checklist before promoting to a release

- [ ] LLM provider chosen and wired (Azure OpenAI, Bedrock, or local)
- [ ] Embedding model + dimension confirmed; pgvector index strategy decided (HNSW vs IVFFlat)
- [ ] Rate limiting applied to `/api/Chat/stream` (`partition: user`, conservative bucket)
- [ ] CSP allows the streaming endpoint (`connect-src` includes the API origin)
- [ ] Audit log entries emitted on conversation create / message send
- [ ] `.ai/features/ai-chatbot/{files,do-dont,customize,verify}.md` filled in to dossier standard
