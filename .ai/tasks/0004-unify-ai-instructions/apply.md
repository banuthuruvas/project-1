# Task 0004 — Adopt Unified `.ai/` Instruction Folder

> **Why:** before this task, AI instructions lived in 6 places (root markdowns, `.github/instructions/`, `.kiro/steering/`, `agents/*.instructions.md`, `docs/COMMON-INSTRUCTIONS.md`, `docs/AGENT-PLAYBOOK.md`). After this task, they live in `.ai/` only. Tool-specific entry points become 10-line redirect stubs.

## Pre-checks

```bash
[ ! -d .ai ] || { echo "Already adopted; skipping."; exit 0; }
```

## 1. Files to copy from the template repo

Copy the entire `.ai/` folder from the template at the version stated in `task.json:templateVersionAfterApply`:

```text
.ai/README.md
.ai/ALIGN.md
.ai/common/                 # full folder (00 through 09)
.ai/tool-routes/            # full folder (claude, copilot, kiro, gemini)
.ai/tasks/index.json
.ai/tasks/_TEMPLATE/        # full folder
.ai/features/_TEMPLATE/     # full folder
.ai/adrs/TEMPLATE.md
.ai/adrs/001-portal-sso-dual-path-auth.md
.ai/adrs/002-template-release-versioning.md
```

## 2. Files to edit

### `CLAUDE.md` (root)

Replace the entire file with this 10-line redirect:

```markdown
# Claude Code — NIE Template

Instructions live in [`.ai/`](.ai/). Start at [`.ai/tool-routes/claude.md`](.ai/tool-routes/claude.md).

For any code change, follow the read order in [`.ai/README.md`](.ai/README.md).

For derived-repo alignment, paste [`.ai/ALIGN.md`](.ai/ALIGN.md) into a Claude Code session.

For template release changes or upgrading a derived repo, read [`.ai/common/09-template-versioning.md`](.ai/common/09-template-versioning.md).
```

### `AGENTS.md` (root)

```markdown
# AI Agents — NIE Template

All AI agent instructions live in [`.ai/`](.ai/). Start at [`.ai/README.md`](.ai/README.md).

| Tool | Routing file |
| --- | --- |
| Claude Code | [`.ai/tool-routes/claude.md`](.ai/tool-routes/claude.md) |
| GitHub Copilot | [`.ai/tool-routes/copilot.md`](.ai/tool-routes/copilot.md) |
| Gemini | [`.ai/tool-routes/gemini.md`](.ai/tool-routes/gemini.md) |
| Kiro | [`.ai/tool-routes/kiro.md`](.ai/tool-routes/kiro.md) |

To upgrade a derived repository to the latest template release, paste [`.ai/ALIGN.md`](.ai/ALIGN.md) into your AI agent.
```

### `GEMINI.md` (root)

```markdown
# Gemini — NIE Template

Instructions live in [`.ai/`](.ai/). Start at [`.ai/tool-routes/gemini.md`](.ai/tool-routes/gemini.md).

For any code change, follow the read order in [`.ai/README.md`](.ai/README.md).
```

### `.github/copilot-instructions.md`

```markdown
# GitHub Copilot — NIE Template

Instructions live in [`.ai/`](../.ai/). Start at [`.ai/tool-routes/copilot.md`](../.ai/tool-routes/copilot.md).

For any code change, follow the read order in [`.ai/README.md`](../.ai/README.md).
```

### `.kiro/steering/_redirect.md` (create)

```markdown
# Kiro Steering — NIE Template

Steering content lives in [`.ai/`](../../.ai/) (the unified instruction folder). Start at [`.ai/tool-routes/kiro.md`](../../.ai/tool-routes/kiro.md).
```

## 3. Files to delete

```text
agents/backend.instructions.md
agents/csharp.instructions.md
agents/access-control.instructions.md
agents/audit-log.instructions.md
agents/frontend.instructions.md
agents/typescript-vue.instructions.md
.github/instructions/                     # entire folder
.kiro/steering/backend.md
.kiro/steering/csharp.md
.kiro/steering/frontend.md
.kiro/steering/typescript-vue.md
.kiro/steering/aidlc.md
.kiro/steering/nietemplate.md
.kiro/steering/copilot-instructions.md
```

Move existing ADRs:

```bash
git mv agents/001-portal-sso-dual-path-auth.md .ai/adrs/001-portal-sso-dual-path-auth.md
git mv agents/002-template-release-versioning.md .ai/adrs/002-template-release-versioning.md
git mv agents/TEMPLATE.md .ai/adrs/TEMPLATE.md
```

After the moves, the `agents/` folder should be empty. Either delete it (`rmdir agents`) or leave a single redirect README pointing into `.ai/adrs/`.

## 4. Verification

```bash
test -f .ai/README.md
test -f .ai/ALIGN.md
test -f .ai/tasks/index.json
grep -q "Instructions live in" CLAUDE.md
grep -q "Instructions live in" AGENTS.md
grep -q "Instructions live in" GEMINI.md
grep -q "Instructions live in" .github/copilot-instructions.md
test ! -d .github/instructions
ls agents/ 2>/dev/null | grep -E "\.instructions\.md$" | wc -l    # 0
```

## 5. Rollback

`git restore` the affected files. The previous structure is reachable via git history.
