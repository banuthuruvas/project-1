# Task 0007 — BOLA Ownership Pattern

> **Status:** scaffolded — intentionally not auto-applied because it requires per-feature adoption decisions (which entities are owned vs shared).

> **Why:** OWASP API1 (BOLA — Broken Object Level Authorization). `RequireAccessFunction` enforces *function*-level authorization (you can read POs); it does NOT enforce *record*-level ownership (you can only read POs you created). Without a standard pattern, every feature reinvents this and at least one will get it wrong.

## Pre-checks

```bash
test ! -f src/backend/Libraries/Domain/Models/IOwnedEntity.cs || { echo "Already applied."; exit 0; }
```

## 1. Files to create

### `src/backend/Libraries/Domain/Models/IOwnedEntity.cs`

```csharp
namespace Domain.Models;

/// <summary>
/// Marker interface for entities that have a per-record owner. The OwnerUserId field is
/// matched against BaseController.UserId by OwnedEntityActionFilter and BaseController.EnsureOwnedAsync.
/// Admins (IsAdmin == true) bypass the check.
/// </summary>
public interface IOwnedEntity
{
    string OwnerUserId { get; }
}
```

### `src/backend/API/Authorization/OwnedEntityActionFilter.cs`

An `IAsyncActionFilter` that, for any controller action with a route parameter `id` and a generic-typed entity `TEntity : IOwnedEntity`, resolves the entity, compares `OwnerUserId` to `BaseController.UserId`, and short-circuits with `Forbid()` if mismatch and the user is not admin.

Plus a `[RequireOwnership(typeof(TEntity), "id")]` attribute that wires this without per-controller boilerplate.

## 2. Files to edit

### `src/backend/API/Controllers/BaseController.cs`

Add a protected helper:

```csharp
protected async Task<IActionResult?> EnsureOwnedAsync<TEntity>(int id, IBaseService<TEntity> service)
    where TEntity : class, IOwnedEntity
{
    if (IsAdmin) return null;
    var entity = await service.GetByIdAsync(id);
    if (entity == null) return NotFound();
    if (entity.OwnerUserId != UserId) return Forbid();
    return null;
}
```

Pattern of use in a feature controller:

```csharp
[HttpGet("{id}")]
[RequireAccessFunction(AccessFunctionCodes.Api.FooRead)]
public async Task<ActionResult<FooDto>> Get(int id)
{
    var guard = await EnsureOwnedAsync(id, _service);
    if (guard != null) return guard;
    var entity = await _service.GetByIdAsync(id);
    return Ok(_mapper.Map<FooDto>(entity!));
}
```

### `src/backend/Libraries/Services/Services/BaseService.cs`

Optional: when listing, automatically filter to records owned by `userContext.UserId` if `T : IOwnedEntity` and the user is not admin. Make this opt-in via a `GetAllOwnedAsync(userId)` method instead of changing `GetAllAsync()` semantics — preserves existing behavior.

### `.ai/common/04-do-and-dont.md`

Add a new DO rule:

> Use `IOwnedEntity` + `[RequireOwnership]` for any entity where users should only see/edit their own records. `RequireAccessFunction` alone is not enough.

## 3. Verification

```bash
dotnet build src/backend/NieTemplate.sln
grep -rn "IOwnedEntity\|EnsureOwnedAsync\|RequireOwnership" src/backend/ | wc -l   # ≥ 4
```

Add a Playwright test that creates two test users and asserts user-A gets `403` when accessing user-B's record.

## 4. Rollback

`git restore` the affected files.

## Maintainer review

- [ ] Decide which existing entities should adopt `IOwnedEntity` (Procurement.PurchaseOrder.RequestedBy is the obvious candidate)
- [ ] Document admin bypass behavior in the security model
- [ ] Add ownership E2E test as a permanent fixture
