# Contributing & Best Practices

Guidelines for working with the NIE Template project.

---

## Code Style

### Backend (.NET)

- Use `async/await` for all I/O operations
- Use meaningful names for variables, methods, and classes
- Follow C# naming conventions (PascalCase for public, camelCase for private)
- Keep methods small and focused
- Add XML documentation for public APIs

### Frontend (Vue/TypeScript)

- Use Composition API with `<script setup>`
- Use TypeScript for all files
- Follow Vue 3 naming conventions
- Keep components small and reusable
- Use composables for shared logic

---

## Do's and Don'ts

### ✅ DO's

#### Architecture & Design

1. **Use the established patterns** - Follow existing code structure and conventions
2. **Extend base classes** - Use `BaseEntity`, `TimestampedEntity`, `BaseService`, `BaseController`
3. **Use dependency injection** - Register services in `Program.cs` and inject them
4. **Separate concerns** - Controllers → Services → Data Access
5. **Use DTOs** - Never expose entities directly to the API

#### Backend Development

6. **Use `TimestampedEntity`** for all entities needing audit tracking
7. **Register mappings** in `MappingProfile.cs` for all DTOs
8. **Handle errors gracefully** - Use try/catch and return appropriate HTTP status codes
9. **Log important operations** - Use `ILogger` for debugging and monitoring
10. **Validate input** - Check required fields and business rules in services

#### Frontend Development

11. **Use shared UI components** from `@nietemplate/ui`
12. **Use TypeScript interfaces** for all data types
13. **Handle loading states** - Show spinners during API calls
14. **Handle errors** - Display user-friendly messages with toast notifications
15. **Use Vue Router** for navigation, not direct window.location changes

#### Database & Migrations

16. **Create migrations for every schema change**
17. **Use meaningful migration names** (e.g., `AddUserProfile`, not `Migration1`)
18. **Test migrations locally** before committing
19. **Include rollback strategy** - Test that migrations can be reverted

#### Security

20. **Use session-based auth** - Never bypass the auth middleware
21. **Check permissions** - Use `[RequirePermission]` or role checks
22. **Don't trust client data** - Always validate on the server
23. **Use environment variables** for secrets - Never hardcode credentials

#### Code Quality

24. **Write self-documenting code** - Clear names over comments
25. **Keep methods under 50 lines** - Extract to helper methods if longer
26. **Remove dead code** - Don't comment out, delete it (git has history)
27. **Format code** - Use Prettier (frontend) and IDE formatting (backend)

---

### ❌ DON'Ts

#### Critical - Never Do These

1. **Don't modify `BaseService.cs`** - Extend it with your own base class if needed
2. **Don't modify `BaseController.cs`** - Extend it instead
3. **Don't modify `BaseEntity.cs` or `TimestampedEntity.cs`** - They're core infrastructure
4. **Don't disable the session validation middleware** - Security critical
5. **Don't commit credentials** - No passwords, API keys, or secrets in code

#### Backend Anti-Patterns

6. **Don't put business logic in controllers** - Use services
7. **Don't bypass the DbContext** - Don't write raw SQL unless absolutely necessary
8. **Don't create circular dependencies** - Services shouldn't depend on each other circularly
9. **Don't modify the audit logging system** without team approval
10. **Don't skip migrations** - Never modify the database manually

#### Frontend Anti-Patterns

11. **Don't call API directly** - Use service classes
12. **Don't ignore errors** - Always handle catch blocks
13. **Don't use `any` type** - Define proper TypeScript interfaces
14. **Don't modify shared packages** for app-specific features - Use composition
15. **Don't hardcode URLs** - Use environment variables

#### Performance

16. **Don't fetch all data** - Use pagination for large datasets
17. **Don't make unnecessary API calls** - Cache when appropriate
18. **Don't load unused data** - Only include related entities when needed
19. **Don't block the UI** - Use async operations with loading states

#### General

20. **Don't commit `node_modules`** - It's in `.gitignore`
21. **Don't commit `bin/obj`** - They're in `.gitignore`
22. **Don't modify `.gitignore` to include build artifacts**
23. **Don't push directly to main** - Use feature branches and PRs
24. **Don't leave console.log statements** - Remove before committing

---

## Common Mistakes to Avoid

### 1. Forgetting to Register Services

```csharp
// ❌ Wrong - Service not registered, will throw at runtime
public class MyController : BaseController
{
    private readonly IMyService _myService; // Runtime exception!
}

// ✅ Correct - Register in Program.cs
builder.Services.AddScoped<IMyService, MyService>();
```

### 2. Not Mapping DTOs

```csharp
// ❌ Wrong - No mapping configured
return Ok(_mapper.Map<MyDto>(entity)); // Empty/wrong data

// ✅ Correct - Add to MappingProfile.cs
TypeAdapterConfig<MyEntity, MyDto>.NewConfig();
```

### 3. Not Handling Null

```csharp
// ❌ Wrong - Will throw NullReferenceException
var user = await _userService.GetByIdAsync(id);
return user.Name; // Null reference if not found

// ✅ Correct
var user = await _userService.GetByIdAsync(id);
if (user == null)
    return NotFound("User not found");
return user.Name;
```

### 4. Not Using Loading States

```vue
<!-- ❌ Wrong - No loading indication -->
<template>
  <div v-for="item in items" :key="item.id">
    {{ item.name }}
  </div>
</template>

<!-- ✅ Correct - Show loading state -->
<template>
  <NieSpinner v-if="isLoading" />
  <div v-else v-for="item in items" :key="item.id">
    {{ item.name }}
  </div>
</template>
```

### 5. Not Catching Errors

```typescript
// ❌ Wrong - Uncaught error crashes the app
const data = await api.get("/api/items");

// ✅ Correct - Handle errors gracefully
try {
  const data = await api.get("/api/items");
} catch (error) {
  toast.error("Failed to load items");
}
```

---

## Git Workflow

### Branch Naming

- `feature/add-user-profile` - New features
- `bugfix/fix-login-error` - Bug fixes
- `hotfix/security-patch` - Urgent production fixes
- `refactor/cleanup-services` - Code improvements

### Commit Messages

Use clear, descriptive commit messages:

```
✅ Good:
- Add user profile page with avatar upload
- Fix session timeout not redirecting to login
- Update Product entity with category relationship

❌ Bad:
- Fixed stuff
- Updates
- WIP
```

### Before Committing

1. Run the build: `dotnet build` and `pnpm build`
2. Run type checking: `pnpm type-check`
3. Test your changes locally
4. Remove debug statements and console.logs
5. Review your own changes before pushing

---

## Pull Request Guidelines

### Before Creating a PR

- [ ] Code builds without errors
- [ ] All existing tests pass
- [ ] New features have appropriate error handling
- [ ] No hardcoded values that should be configurable
- [ ] Database migrations are included if needed
- [ ] Documentation updated if needed

### PR Description Template

```markdown
## Summary

Brief description of changes

## Changes

- Added X feature
- Fixed Y bug
- Updated Z configuration

## Testing

How to test these changes

## Screenshots (if UI changes)

[Include screenshots]

## Migration Required?

- [ ] Yes - Run migrations after deployment
- [ ] No
```

---

## Testing

### Backend

Test API endpoints using:

- Swagger UI (`/swagger`)
- Postman or similar tools
- Playwright API tests

### Frontend

Test UI by:

- Manual testing in development mode
- Cross-browser testing before release
- Testing different screen sizes

### Integration

Before deployment:

1. Start all services locally
2. Test complete workflows end-to-end
3. Verify database migrations work
4. Test authentication flow
