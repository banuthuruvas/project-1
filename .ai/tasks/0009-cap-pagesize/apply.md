# Task 0009 — Enforce Max PageSize=100

> **Status:** scaffolded.
> **Why:** OWASP API4 (Unrestricted Resource Consumption) and rule N-17. `PurchaseOrderSearchDto` accepts `PageSize` from the client without bounds, so a caller can request millions of rows. Each new search DTO would need to remember the cap independently. The fix: a base DTO that clamps once.

## Pre-checks

```bash
test ! -f src/backend/Libraries/Domain/Dto/PagedSearchDto.cs || { echo "Already applied."; exit 0; }
```

## 1. Files to create

### `src/backend/Libraries/Domain/Dto/PagedSearchDto.cs`

```csharp
namespace Domain.Dto;

/// <summary>
/// Base for any search DTO that supports pagination. PageSize is clamped to [1, 100] on set
/// so a malicious caller cannot request unbounded result sets. Every search DTO must extend
/// this — see template rule N-17 / OWASP API4.
/// </summary>
public abstract class PagedSearchDto
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 25;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
```

## 2. Files to edit

### `src/backend/Libraries/Domain/Dto/PurchaseOrderDto.cs`

```diff
-public class PurchaseOrderSearchDto
+public class PurchaseOrderSearchDto : PagedSearchDto
 {
-    public int Page { get; set; } = 1;
-    public int PageSize { get; set; } = 25;
     public string? Status { get; set; }
     // …other filters…
 }
```

### `src/backend/Libraries/Services/Services/PurchaseOrder/PurchaseOrderService.cs`

`SearchAsync` continues to use `filter.Page` and `filter.PageSize` — they're now clamped, no further changes needed. Confirm the existing query uses `Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)`.

### Any other search DTO in the project

Sweep `Libraries/Domain/Dto/` for classes ending in `SearchDto` or any DTO with `int Page`/`int PageSize` properties. Migrate each to extend `PagedSearchDto`.

## 3. Verification

```bash
dotnet build src/backend/NieTemplate.sln
grep -rn "PagedSearchDto" src/backend/Libraries/Domain/Dto/ src/backend/Libraries/Services/Services/ | wc -l   # ≥ 2
# Live smoke
curl -X POST http://localhost:5002/api/PurchaseOrder/Search \
     -H "X-Session-Id: <SESSION>" -H "Content-Type: application/json" \
     -d '{"PageSize":99999}' | jq '.PageSize'   # expect 100
```

## 4. Rollback

```bash
git restore --staged --worktree src/backend/Libraries/Domain/Dto/ src/backend/Libraries/Services/Services/
```

## Maintainer review

- [ ] Choose `MaxPageSize` per project — 100 is a reasonable default but data-export endpoints may want a higher cap (then they should explicitly allow it via a separate DTO, not by raising the global cap)
- [ ] Document the cap in `.ai/common/04-do-and-dont.md`
