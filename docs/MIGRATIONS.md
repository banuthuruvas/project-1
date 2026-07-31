# Database Migration Guide

## Migration Commands

All commands should be run from the `src` directory.

### Add a New Migration

```bash
dotnet ef migrations add <MigrationName> --project backend/Libraries/Data --startup-project backend/API
```

**Example:**
```bash
dotnet ef migrations add AddUserProfile --project backend/Libraries/Data --startup-project backend/API
```

### Apply Migrations to Database

```bash
dotnet ef database update --project backend/Libraries/Data --startup-project backend/API
```

### Remove Last Migration (if not applied)

```bash
dotnet ef migrations remove --project backend/Libraries/Data --startup-project backend/API
```

### Generate SQL Script

```bash
dotnet ef migrations script --project backend/Libraries/Data --startup-project backend/API -o migration.sql
```

### Roll Back to Specific Migration

```bash
dotnet ef database update <MigrationName> --project backend/Libraries/Data --startup-project backend/API
```

---

## For New Projects

When starting a new project from this template:

1. **Delete all existing migrations:**
   ```bash
   rm -rf backend/Libraries/Data/Migrations/*
   ```

2. **Update the DbContext:**
   - Remove sample entities (SampleModel, SampleChildModel)
   - Add your own entities

3. **Create initial migration:**
   ```bash
   dotnet ef migrations add InitialCreate --project backend/Libraries/Data --startup-project backend/API
   ```

4. **Apply the migration:**
   ```bash
   dotnet ef database update --project backend/Libraries/Data --startup-project backend/API
   ```

---

## Migration Best Practices

### Naming Conventions

Use descriptive names that indicate what the migration does:

| ✅ Good Names | ❌ Bad Names |
|---------------|--------------|
| `AddUserProfile` | `Migration1` |
| `AddOrderStatusColumn` | `Update1` |
| `CreateProductTable` | `Changes` |
| `AddIndexOnEmail` | `Fix` |
| `RenameCustomerToClient` | `New` |

### Before Creating a Migration

1. Ensure the database is up-to-date with existing migrations
2. Double-check your model changes
3. Consider backward compatibility

### After Creating a Migration

1. Review the generated migration code
2. Test the migration locally
3. Test the rollback (`dotnet ef database update <PreviousMigration>`)

---

## Troubleshooting

### "The migration has already been applied"

You need to remove it from the database first:
```bash
dotnet ef database update <PreviousMigration> --project backend/Libraries/Data --startup-project backend/API
dotnet ef migrations remove --project backend/Libraries/Data --startup-project backend/API
```

### "No DbContext was found"

Ensure you're specifying the correct startup project:
```bash
--startup-project backend/API
```

### "Connection refused"

Make sure PostgreSQL is running:
```bash
docker ps  # Check if postgres container is running
```
