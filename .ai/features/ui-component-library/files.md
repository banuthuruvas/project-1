# UI Component Library — File Map

## Owned files

### Package metadata

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/frontend/packages/ui/package.json` | Manifest | Private workspace package, source-only export, scripts (`dev`, `build`, `type-check`, `clean`) |
| `src/frontend/packages/ui/tsconfig.json` | TS config | Source typings for consumers |
| `src/frontend/packages/ui/vite.config.ts` | Bundler | Vite library mode for the optional `dist/` artifact (consumers default to source) |
| `src/frontend/packages/ui/tailwind.config.js` | Tailwind | Color tokens, font stack, spacing scales — the design DNA |
| `src/frontend/packages/ui/postcss.config.js` | PostCSS | Tailwind + autoprefixer pipeline |
| `src/frontend/packages/ui/README.md` | Docs | Per-package readme |

### Barrel + supporting

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/frontend/packages/ui/src/index.ts` | Barrel | Re-exports every component + composable + utility. Adding a new component means adding an `export * from "./components/.../Xxx"` here |
| `src/frontend/packages/ui/src/lib/utils.ts` | Utility | `cn`, `formatDate`, `formatDateTime`, `truncate`, `capitalize`, `generateId`, `sleep`, `debounce` |
| `src/frontend/packages/ui/src/styles/globals.css` | Styles | Tailwind layer + theme tokens (CSS variables) |
| `src/frontend/packages/ui/src/theme/` | Tokens | Theme presets (palettes), theme-switching composable |
| `src/frontend/packages/ui/src/composables/` | Composables | Shared hooks; re-exported from the barrel |

### Primitive components (`./components/ui/`)

| Path | Component | Purpose |
| --- | --- | --- |
| `components/ui/alert/NieAlert.vue` | `NieAlert` | Inline alert with `variant: "info" \| "success" \| "warning" \| "error"` |
| `components/ui/badge/NieBadge.vue` | `NieBadge` | Status pill with size + variant |
| `components/ui/button/NieButton.vue` | `NieButton` | Button with `variant`, `size`, `loading`, `disabled`, `icon` slots |
| `components/ui/card/NieCard.vue` | `NieCard` | Section/content card |
| `components/ui/input/NieInput.vue` | `NieInput` | Form input wrapper with label, hint, error states |
| `components/ui/modal/NieModal.vue` | `NieModal` | Overlay + dialog with named slots for header / body / footer |
| `components/ui/select/NieSelect.vue` | `NieSelect` | Single/multi-select; integrates with code-table options |
| `components/ui/switch/NieSwitch.vue` | `NieSwitch` | Boolean toggle |
| `components/ui/table/NieTable.vue` | `NieTable` | Plain table primitive (composite `NieDataTable` wraps this) |

### Composite components (`./components/composite/`)

| Path | Component | Purpose |
| --- | --- | --- |
| `components/composite/app-feedback/NieAppFeedbackHub.vue` | `NieAppFeedbackHub` | The thumbs-up/down + textarea visual used by `feedback-widget` |
| `components/composite/confirm/NieConfirmDialog.vue` | `NieConfirmDialog` | Yes/No confirmation modal |
| `components/composite/data-table/NieDataTable.vue` | `NieDataTable` | Sortable, paginated, filterable data grid |
| `components/composite/data-table/NieColumnFilterMenu.vue` | `NieColumnFilterMenu` | The per-column filter popover used by `NieDataTable` |
| `components/composite/file-upload/NieFileUploadField.vue` | `NieFileUploadField` | Drag-drop + browse upload field |
| `components/composite/filter-bar/NieFilterBar.vue` | `NieFilterBar` | Multi-filter chip bar (used on dashboards / list pages) |
| `components/composite/list-controls/NieListControls.vue` | `NieListControls` | Search box + view-toggle + count strip for list pages |
| `components/composite/loading/NieLoadingOverlay.vue` | `NieLoadingOverlay` | Page-level spinner with optional message |
| `components/composite/page-header/NiePageHeader.vue` | `NiePageHeader` | Title + breadcrumb + actions slot |
| `components/composite/pagination/NiePagination.vue` | `NiePagination` | Page-number + page-size controls |
| `components/composite/profile-menu/NieLaunchpadProfileMenu.vue` | `NieLaunchpadProfileMenu` | The user avatar dropdown used in `StaffLayout.vue` |
| `components/composite/smart-filter-dropdown/NieSmartFilterDropdown.vue` | `NieSmartFilterDropdown` | Type-ahead dropdown with checkbox multi-select |
| `components/composite/state-panel/NieStatePanel.vue` | `NieStatePanel` | Empty / error / not-found / loading states with illustrations |
| `components/composite/toast/NieToastContainer.vue` | `NieToastContainer` | Toast stack consumer used by `useToast` |

### Theme components (`./components/theme/`)

| Path | Component | Purpose |
| --- | --- | --- |
| `components/theme/NieThemeAuthPanel.vue` | `NieThemeAuthPanel` | Demo auth-panel surface |
| `components/theme/NieThemeReportCard.vue` | `NieThemeReportCard` | Demo report card |
| `components/theme/NieThemeShell.vue` | `NieThemeShell` | Demo shell layout |
| `components/theme/NieThemeStatCard.vue` | `NieThemeStatCard` | Demo KPI card |
| `components/theme/NieThemeWizardStepper.vue` | `NieThemeWizardStepper` | Demo wizard stepper |

## Touched files

| Path | What it contains | Why must be touched |
| --- | --- | --- |
| `src/frontend/main/package.json` | `"@nietemplate/ui": "workspace:*"` dep | Required for the main app to import the package |
| `src/frontend/auth/package.json` | `"@nietemplate/ui": "workspace:*"` dep | Required for the auth app to import the package |
| `src/frontend/main/src/main.ts` | `import "@nietemplate/ui/styles"` | Loads the global Tailwind layer + tokens |
| `src/frontend/auth/src/main.ts` | `import "@nietemplate/ui/styles"` | Same for auth |
| `src/frontend/main/tailwind.config.js` (and auth's) | `content` glob extended to include `node_modules/@nietemplate/ui/src/**/*.{vue,ts}` | Required for Tailwind JIT to scan UI lib classes |
| `src/frontend/main/vite.config.ts` (and auth's) | Vite `resolve.dedupe` for `vue` to avoid double-bundling | The UI lib has `vue` as a peer dep |
| `src/frontend/pnpm-workspace.yaml` | Includes `packages/ui` in the workspace | Required for `workspace:*` resolution |

## Migrations

None — no DB layer.

## External dependencies

| Package | Purpose |
| --- | --- |
| `vue` (peer) | Component framework |
| `@heroicons/vue` | Icon set used by primitives + composites |
| `@vueuse/core` | Reactive utilities (e.g. `useElementVisibility`, `onClickOutside`) |
| `clsx` + `tailwind-merge` | Behind `cn()` utility |
| `tailwindcss` (peer) | Atomic styling — but each app is responsible for its own Tailwind build |
