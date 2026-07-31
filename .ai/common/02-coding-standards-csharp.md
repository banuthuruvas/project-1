# 02 — C# / .NET Coding Standards

Applies to anything under `src/backend/`.

## Naming

- Classes / methods / public properties: **PascalCase**
- Private fields: **`_camelCase`** (underscore prefix mandatory)
- Local variables / parameters: **camelCase**
- Constants: **PascalCase**
- Async methods: suffix with **`Async`**
- Interface: prefix with **`I`** (`IUserService`)
- Enums: prefix with **`E`** (`ERole`, `ECodeType`)

## Common imports (top of file, in this order)

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MapsterMapper;
using Domain.Models;
using Domain.Dto;
using Domain.Enum;
using Domain.Security;
using Data.Data;
```

## Entity pattern

```csharp
public class YourEntity : TimestampedEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public EYourStatus Status { get; set; }              // ENUM, not string
    public int RelatedId { get; set; }
    public virtual RelatedEntity Related { get; set; } = default!;
    public virtual ICollection<Child> Children { get; set; } = new List<Child>();
}
```

`= default!` on required reference types; nullable `?` on optional fields. **Status fields are always enums.**

## DTO pattern

```csharp
public class YourEntityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public EYourStatus Status { get; set; }              // mirror the enum
    public string? RelatedName { get; set; }             // flatten nav for display
}
```

DTOs never expose navigation properties. They flatten what the UI needs.

## Service pattern

```csharp
public interface IYourEntityService : IBaseService<YourEntity>
{
    Task<IList<YourEntity>> GetActiveAsync();
}

public class YourEntityService : BaseService<YourEntity>, IYourEntityService
{
    public YourEntityService(MainDbContext context) : base(context) { }

    public Task<IList<YourEntity>> GetActiveAsync() =>
        Records.Where(x => x.Status == EYourStatus.Active).ToListAsync();
}
```

Use `Records` (the protected `DbSet<T>` from `BaseService`). Use `Include()` for eager loading.

## Controller pattern

```csharp
public class YourEntityController : BaseController
{
    private readonly IYourEntityService _service;
    private readonly IMapper _mapper;
    private readonly ILogger<YourEntityController> _logger;

    public YourEntityController(IYourEntityService service, IMapper mapper, ILogger<YourEntityController> logger)
    {
        _service = service;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.YourEntityRead)]
    public async Task<ActionResult<IEnumerable<YourEntityDto>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(_mapper.Map<List<YourEntityDto>>(items));
    }
}
```

## Async rules

- All I/O is async.
- Never call `.Result` or `.Wait()`.
- Stream large results with `IAsyncEnumerable<T>` when paging is not enough.

## Error handling

- Don't catch `Exception` to swallow. Throw, the global exception middleware turns it into a typed response.
- Validate user input at controller boundary (return `BadRequest`).
- Trust internal callers — no defensive null checks on DI-injected services.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/backend/Libraries/Data --startup-project src/backend/API
dotnet ef database update --project src/backend/Libraries/Data --startup-project src/backend/API
dotnet ef migrations remove --project src/backend/Libraries/Data --startup-project src/backend/API
```

Every schema change ships with a migration in the same PR.
