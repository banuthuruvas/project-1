# `manifest.yaml` schema for feature dossiers

Every feature directory under `.ai/features/<feature-name>/` SHOULD have a `manifest.yaml` file that gives downstream tooling a structured view of the feature. nie-ignite's `TemplateRecipeAdapter` reads these files at seed time and converts them into its `InstructionSetItem` shape; the registry / bot / audit can also use them.

`README.md`, `files.md`, `do-dont.md`, `customize.md`, `verify.md` are still authored as Markdown — `manifest.yaml` is just the *machine-readable summary* of what the README says.

## Schema (v1)

```yaml
# Required
name: ai-chatbot                 # must equal the feature directory name
title: AI Chatbot                # human-readable
category: backend                # one of: backend / frontend / fullstack / infra / security / devex
status: scaffolded               # one of: scaffolded / released / deprecated
description: |
  One paragraph (no more than 500 chars) describing what the feature
  does and when to use it. This is what shows up in the Ignite wizard
  module-selection UI.

# Optional
copierFlag: include_chat         # the boolean flag in copier.yml that toggles this feature; null if not Copier-gated
relatedTasks: ["0010"]           # IDs of tasks (.ai/tasks/<NNNN>/) that ship or modify this feature
dependsOn: ["shared-utilities"]  # feature names this one requires
removableInDerivedRepo: true     # whether a derived repo can later remove this via a cleanup task
removalTaskId: null              # if removable, the task ID that removes it (null if no such task yet)
files:
  - path: "src/backend/Libraries/Services/Services/Chat/IChatService.cs"
    role: interface
  - path: "src/backend/Libraries/Services/Services/Chat/ChatService.cs"
    role: implementation
  - path: "src/backend/API/Controllers/ChatController.cs"
    role: controller
  - path: "src/frontend/main/src/services/chatService.ts"
    role: client

# Tags for catalog filtering / search
tags: [llm, sse, pgvector]

# Owners (free-form; Backstage-compatible identifiers if you use Backstage)
owners:
  - "group:nie-platform"
```

## Field rules

- `name` MUST equal the directory name. Validation at seed time fails fast if not.
- `category` is a closed enum. Adding a new category is a template-versioning change (cut a release).
- `status: scaffolded` means the feature is in the template repo but no derived repo should auto-adopt it. `released` means it's stable and tasks targeting it are eligible for the bot to PR. `deprecated` is read by the bot to suppress new auto-PRs.
- `copierFlag`, when set, must match a question name in `copier.yml`. The Ignite wizard derives its module-selection UI from this field.
- `files` is the canonical file list. `files.md` is the human-readable mirror. Keep them in sync (a future audit check will compare them).

## Validation

Run `python tools/template-audit/audit.py --repo .` — the audit's `features` category checks for the presence of `manifest.yaml`, validates required fields, and confirms `category` is in the closed enum.
