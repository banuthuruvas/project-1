# 03 — TypeScript / Vue 3 Coding Standards

Applies to anything under `src/frontend/`.

## Naming

- Interfaces / Types: **PascalCase** (`UserProfile`, `ButtonVariant`)
- Functions / variables: **camelCase**
- Constants: **SCREAMING_SNAKE_CASE** (or camelCase for local consts)
- Component file names: **PascalCase.vue**
- Components in templates: **PascalCase** (`<NieButton />`)
- Props in templates: **kebab-case** (`<NieButton variant-mode="primary" />`)

## File layout (Vue SFC)

Order MUST be: `<script setup lang="ts">` → `<template>` → `<style>` (optional, scoped).

```vue
<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useToast, NieButton, NieCard } from '@nietemplate/ui';
import entityService, { type Entity } from '@/services/entityService';
import { EEntityStatus } from '@/types/entity';      // status ENUM, not string

const isLoading = ref(true);
const items = ref<Entity[]>([]);

const fetchData = async () => {
  isLoading.value = true;
  try {
    items.value = await entityService.getAll();
  } finally {
    isLoading.value = false;
  }
};
onMounted(fetchData);
</script>

<template>
  <NieCard title="Entities">
    <p v-if="isLoading">Loading…</p>
    <ul v-else>
      <li v-for="item in items" :key="item.id">
        {{ item.name }} —
        <span :class="badgeClass(item.status)">{{ statusLabel(item.status) }}</span>
      </li>
    </ul>
  </NieCard>
</template>
```

## Component rules (from Vue 3 Style Guide — Strongly Recommended)

- Always use `<script setup lang="ts">`.
- Multi-word component names — never one word (`<NieButton>` not `<Button>`).
- Detailed prop definitions: every prop has `type`, optional `required`, optional `default`, optional `validator`.
- Always key `v-for`.
- Never combine `v-if` and `v-for` on the same element.
- Self-close components without children (`<NieIcon name="close" />`).
- Order of element/attribute groups: definition (`is`, `v-for`), conditionals, render modifiers, then events, then content.

## Service pattern

```typescript
import api from './api';
import type { EEntityStatus } from '@/types/entity';

export interface Entity {
  id?: number;
  name: string;
  description?: string | null;
  status: EEntityStatus;            // enum mirror, never string
  createdOn?: string | null;
}

const entityService = {
  async getAll(): Promise<Entity[]> {
    return (await api.get<Entity[]>('/api/Entity/GetAll')).data;
  },
  async getById(id: number): Promise<Entity> {
    return (await api.get<Entity>(`/api/Entity/Get/${id}`)).data;
  },
  async save(entity: Entity): Promise<Entity> {
    const endpoint = entity.id ? '/api/Entity/Edit' : '/api/Entity/Save';
    return (await api.post<Entity>(endpoint, entity)).data;
  },
  async delete(id: number): Promise<void> {
    await api.post(`/api/Entity/Delete/${id}`);
  },
};
export default entityService;
```

## Enum mirroring (mandatory)

Every backend enum (`Domain.Enum.E*`) MUST have a TypeScript mirror. Place the mirror in:

- `src/frontend/main/src/types/<feature>.ts` (app-specific), or
- `src/frontend/packages/shared/src/types/` (cross-app).

```typescript
// Mirror of Domain.Enum.EPurchaseOrderStatus
export enum EPurchaseOrderStatus {
  Draft = 'Draft',
  Submitted = 'Submitted',
  PendingManagerApproval = 'PendingManagerApproval',
  PendingFinanceApproval = 'PendingFinanceApproval',
  PendingProcurementApproval = 'PendingProcurementApproval',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Cancelled = 'Cancelled',
}
```

The string values must match the C# `enum.ToString()` output. Status / state / type / category fields in component code reference these enums — never raw string literals.

## Toast and error handling

```typescript
const toast = useToast();
toast.success('Saved');
toast.error('Failed to save');
```

Never let a fetch failure go silent. Surface either an inline error or a toast.

## Don't

- Don't use `any`.
- Don't call `axios` or `fetch` directly inside a component — go through a service.
- Don't hardcode status / state / category strings — use the mirrored enum.
- Don't ship a component that lacks loading and error states.
