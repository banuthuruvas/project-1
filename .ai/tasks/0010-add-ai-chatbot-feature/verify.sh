#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

# Structural checks (always run)
test -f src/backend/Libraries/Services/Services/Chat/IChatService.cs \
  || { echo "FAIL: IChatService.cs missing"; exit 1; }
test -f src/backend/Libraries/Services/Services/Chat/ChatService.cs \
  || { echo "FAIL: ChatService.cs missing"; exit 1; }
test -f src/backend/API/Controllers/ChatController.cs \
  || { echo "FAIL: ChatController.cs missing"; exit 1; }
test -f src/frontend/main/src/services/chatService.ts \
  || { echo "FAIL: frontend chatService.ts missing"; exit 1; }

grep -q "IChatService" src/backend/API/Program.cs \
  || { echo "FAIL: IChatService not registered in Program.cs"; exit 1; }

# Build checks
dotnet build src/backend/NieTemplate.sln
pnpm --filter main type-check

echo "verify.sh: OK (ai-chatbot feature wired)"
