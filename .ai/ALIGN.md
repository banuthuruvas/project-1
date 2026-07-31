# ALIGN — Self-Check Prompt for Derived Repositories

> **Paste the entire content below into any AI agent (Claude Code, Copilot Chat, Gemini, Kiro) running in a repository that was cloned from NIE Template. The agent will inspect the repo, compare against the latest template release, and apply missing tasks — asking you when a decision is needed.**

---

## Prompt to paste

```
You are an AI agent verifying that this repository is aligned with the NIE Template at its latest release. Operate strictly in the order below and STOP for confirmation when noted.

STEP 1 — DETECT BASELINE
1.1. Read `.nie-template-version.json` from the repo root.
     - If it does not exist: report this and STOP. Ask the user whether to bootstrap the file from the template repo. Do not proceed without confirmation.
     - If it exists: capture `templateVersion` and `appliedTasks` (list of task IDs).
1.2. Read `.ai/tasks/index.json` from the local repo (it should have been copied from the template).
     - If it does not exist: report this and STOP. Ask the user to fetch `.ai/tasks/index.json` from the template repo. Do not proceed without confirmation.

STEP 2 — IDENTIFY MISSING TASKS
2.1. Compute the set of task IDs in `.ai/tasks/index.json` that are NOT in `appliedTasks`.
2.2. For each missing task, read the matching `.ai/tasks/<taskId>-<slug>/task.json`.
2.3. Filter to tasks whose `appliesIf` matches the current repo (e.g., `anyFileExists` evaluates to true).
2.4. Produce a short report listing each applicable missing task with: ID, title, type, breaking flag, summary of files affected.
2.5. STOP. Ask the user: "Apply these tasks now?" Wait for approval. Honor any specific subset they pick.

STEP 3 — APPLY EACH APPROVED TASK (in ascending taskId order)
3.1. Open the task's `apply.md`.
3.2. Walk through every section labelled "Files to delete," "Files to edit (line by line)," "Files to create."
3.3. For each line-by-line edit, do exactly what the dossier says — DO NOT IMPROVISE. If the current file content differs from the expected pre-state, STOP and ask the user how to proceed. Common cases:
       - The file does not exist anymore → skip with a note.
       - The file exists but extra customizations were added on top → ask whether to preserve, replace, or merge.
       - Conflicting renames detected → ask the user.
3.4. After all edits in the task: run the verification commands listed in `verify.sh` (or the `verification` array in `task.json`).
3.5. If verification fails: STOP. Surface the failure exactly. Do not mark the task as applied.
3.6. If verification passes: append the taskId to `appliedTasks` in `.nie-template-version.json`. Update `templateVersion` to the task's `templateVersionAfterApply` if higher than the current version.

STEP 4 — RE-RUN ALIGNMENT CHECKS
4.1. After all approved tasks are applied, run a compliance sweep against `.ai/common/04-do-and-dont.md`:
       a. No hardcoded status / state / type / category strings (grep against current frontend + backend; flag occurrences).
       b. Every entity in `Domain.Enum.E*` has a TypeScript mirror in `src/frontend/main/src/types/` or `packages/shared/src/types/`.
       c. Every Vue page touching async I/O has loading + error state.
       d. No `any` in TypeScript outside generated declaration files.
       e. No `.Result` / `.Wait()` in C#.
       f. Every controller endpoint has `[RequireAccessFunction(...)]`.
4.2. For every violation found, propose a follow-up task description (do not invent a task ID — that's a maintainer's job). Group by file.

STEP 5 — REPORT
5.1. Produce a final report with:
       - Tasks applied this run (IDs + titles)
       - Verification commands run + their exit codes
       - Compliance violations found
       - Open questions (anything you stopped to ask the user about)
       - Recommended next actions
5.2. Commit the changes with a message: `chore(template): adopt tasks NNNN..NNNN and align with NIE Template <version>`.

CONSTRAINTS
- NEVER apply a task without user approval (Step 2.5).
- NEVER fabricate file content. If the dossier expects content that the local file lacks, ask the user.
- NEVER amend a previous commit; always create new commits.
- NEVER bypass `verify.sh` — if it fails, the task is NOT applied.
- NEVER touch `node_modules/`, `bin/`, `obj/`, `dist/`, generated migrations belonging to derived-repo features.
- ASK if something looks like in-progress local work (uncommitted, on a feature branch with the matching name) — do not overwrite it.
```

---

## Tips for users running this prompt

- **First-time use** in a derived repo: the agent may need you to copy `.ai/tasks/` from the template repo. Run `git remote add nie-template <url> && git fetch nie-template main && git checkout nie-template/main -- .ai/tasks/ docs/template-releases/` if you do not already have these.
- **Big version skips**: if you are 5+ releases behind, expect the agent to stop multiple times for confirmation. That's by design — bigger jumps are riskier.
- **Local customizations**: if you renamed `NieTemplate` to your project name, the agent will detect drift on file edits. Always answer the agent's questions instead of letting it overwrite.
- **Verification is hard-required**: if `verify.sh` fails on a task, fix the failure or roll the task back before proceeding to the next one.
