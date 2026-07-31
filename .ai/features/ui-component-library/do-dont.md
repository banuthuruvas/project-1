# UI Component Library — Do and Don't

## DO ✅

1. **DO** import every NIE component via the package barrel: `import { NieButton, NieDataTable } from "@nietemplate/ui";`. Going through the barrel keeps imports stable when files move.
2. **DO** reuse primitives (`NieButton`, `NieInput`, `NieSelect`, ...) inside any new composite — composites should not re-style buttons or invent their own input.
3. **DO** name every component with the `Nie` prefix (`NieFoo.vue`). The prefix lets Vue users know at a glance the component is from this library and prevents collisions with project-specific components.
4. **DO** use the `cn(...)` utility for class composition — it merges Tailwind classes deterministically (`clsx` + `tailwind-merge`).
5. **DO** consume theme tokens via CSS variables (defined in `globals.css`) rather than hardcoding hex codes. The token names (e.g. `--color-primary`, `--color-surface`) are the design contract.
6. **DO** keep the package source-only (`main / module / types → ./src/index.ts`). Vite consumers compile through TS during build; no manual publish step is needed.
7. **DO** add new components in their own folder (e.g. `components/ui/avatar/NieAvatar.vue` + `index.ts` re-export) and add the export line to `src/index.ts`.
8. **DO** ensure every consumer app's `tailwind.config.js` `content` glob covers `node_modules/@nietemplate/ui/src/**/*.{vue,ts}` — without this, Tailwind JIT misses classes used inside the lib and styles disappear in production.
9. **DO** keep `vue` and `tailwindcss` as peer dependencies — bundling them inside the lib doubles the runtime size in consumers.
10. **DO** test new components in BOTH apps (main + auth). The auth app uses a smaller subset, but it's still a consumer; breaking it is easy if you ship Vue 3.5+ syntax that conflicts.

## DON'T ❌

1. **DON'T** import individual files like `import NieButton from "@nietemplate/ui/src/components/ui/button/NieButton.vue"`. The `./src/*` subpath export exists for emergency cases only — go through the barrel.
2. **DON'T** add business logic to library components. A `NieDataTable` should know about pagination and filtering, NOT about audit logs or specific entity types.
3. **DON'T** add API calls inside library components. The lib has no axios dependency and should not. Consumers pass data via props and listen to events.
4. **DON'T** import from `@/` aliases inside library files. The lib is consumed by N apps, each with their own `@/` alias — using it inside the lib breaks portability.
5. **DON'T** ship a hardcoded NIE-specific copy ("Welcome to NIE") inside a component. Use a slot or a prop so derived projects can replace strings.
6. **DON'T** introduce a new design token without updating `tailwind.config.js` AND `globals.css` (CSS variables) AND documenting it in this dossier.
7. **DON'T** publish the package to npm. It's `private: true` for a reason — versioning and security exposure are not worth it for an internal lib.
8. **DON'T** add a peer dep that isn't already in main + auth. New peers must be added to consumer apps simultaneously, otherwise pnpm errors out.
9. **DON'T** export internal helpers from the barrel. If a function is used only inside the library, keep it scoped to the package; only expose via `index.ts` what consumers should rely on.
10. **DON'T** rename a component without leaving a re-export shim. Component names appear in dozens of pages — silent rename creates a wave of build failures across derived repos. Add a `// @deprecated` comment + a re-export.
