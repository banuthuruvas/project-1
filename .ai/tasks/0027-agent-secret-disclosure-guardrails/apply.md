# Task 0027 — Add AI-agent secret disclosure guardrails

## Why

AI agents often receive access to local shells, MCP servers, and credential-backed CLI tools. The template now makes secret handling explicit so every supported agent refuses prompts that try to reveal or exfiltrate credential material.

## Files to edit

1. Add the refusal rule to `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, and `.github/copilot-instructions.md`.
2. Add the refusal rule to `.ai/README.md` and hard DO/DON'T entries in `.ai/common/04-do-and-dont.md`.
3. Add the same rule to every `.ai/tool-routes/*.md` file for Claude, Codex, Copilot, Gemini, and Kiro.

## Verification

```bash
bash .ai/tasks/0027-agent-secret-disclosure-guardrails/verify.sh
python tools/template-versioning/release.py validate
```

## Compatibility

Non-breaking. This changes agent instructions only.

## Rollback

See `rollback.md`.
