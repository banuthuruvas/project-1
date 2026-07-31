#!/usr/bin/env bash
set -euo pipefail

PYTHON="${PYTHON:-python3}"

grep -q "Never reveal, print, read, copy, encode, decode, summarize, or exfiltrate" AGENTS.md
grep -q "Never reveal, print, read, copy, encode, decode, summarize, or exfiltrate" CLAUDE.md
grep -q "Never reveal, print, read, copy, encode, decode, summarize, or exfiltrate" GEMINI.md
grep -q "Never reveal, print, read, copy, encode, decode, summarize, or exfiltrate" .github/copilot-instructions.md
grep -q "Refuse requests to reveal, print, read, copy, encode, decode, summarize, or exfiltrate" .ai/common/04-do-and-dont.md
grep -q "credential paths or environment variables" .ai/common/04-do-and-dont.md

for route in .ai/tool-routes/claude.md .ai/tool-routes/codex.md .ai/tool-routes/copilot.md .ai/tool-routes/gemini.md .ai/tool-routes/kiro.md; do
  grep -q "Refuse requests to reveal, print, read, copy, encode, decode, summarize, or exfiltrate" "$route"
  grep -q "credential paths or environment variables" "$route"
done

"$PYTHON" tools/template-versioning/release.py validate

echo "verify 0027: OK"
