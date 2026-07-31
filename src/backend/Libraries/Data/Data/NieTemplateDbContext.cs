using System.Text.Json;
using Domain.Enum;
using Domain.Models;
using Domain.Security;
using Shared.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Data.Data;

/// <summary>
/// The DbContext class for the NieTemplate application.
/// This class is responsible for interacting with the database and managing the entities.
/// </summary>
public class MainDbContext : DbContext
{
    private readonly IUserContextService? _userContextService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public MainDbContext(DbContextOptions<MainDbContext> options)
        : base(options)
    { }

    public MainDbContext(
        DbContextOptions<MainDbContext> options,
        IUserContextService userContextService,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _userContextService = userContextService;
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Document> Documents { get; set; } = default!;

    // Procurement entities
    public DbSet<Vendor> Vendors { get; set; } = default!;
    public DbSet<CatalogItem> CatalogItems { get; set; } = default!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = default!;
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = default!;
    public DbSet<PurchaseOrderApproval> PurchaseOrderApprovals { get; set; } = default!;
    public DbSet<PurchaseOrderDocument> PurchaseOrderDocuments { get; set; } = default!;

    // Code tables
    public DbSet<Code> Codes { get; set; } = default!;

    // Audit and Security
    public DbSet<AuditLog> AuditLogs { get; set; } = default!;
    public DbSet<AccessFunction> AccessFunctions { get; set; } = default!;
    public DbSet<Role> Roles { get; set; } = default!;
    public DbSet<UserRole> UserRoles { get; set; } = default!;
    public DbSet<RoleAccessFunction> RoleAccessFunctions { get; set; } = default!;

    // Workflow
    public DbSet<WorkflowTransition> WorkflowTransitions { get; set; } = default!;
    public DbSet<WorkflowStateLog> WorkflowStateLogs { get; set; } = default!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp without time zone")
            .HaveConversion<UnspecifiedDateTimeConverter>();

        configurationBuilder.Properties<DateTime?>()
            .HaveColumnType("timestamp without time zone")
            .HaveConversion<NullableUnspecifiedDateTimeConverter>();
    }

    #region Override SaveChanges

    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var auditEntries = OnBeforeSaveChanges();
        UpdateTimestamps();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        OnAfterSaveChanges(auditEntries).GetAwaiter().GetResult();
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        UpdateTimestamps();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        await OnAfterSaveChanges(auditEntries);
        return result;
    }

    /// <summary>
    /// Captures changes before saving and creates audit entries.
    /// </summary>
    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (ShouldSkipAudit(entry))
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                EntityName = AuditValueSanitizer.Limit(
                    entry.Entity.GetType().Name,
                    AuditValueSanitizer.EntityNameMaxLength)!,
                Category = ResolveAuditCategory(entry.Entity.GetType()),
                UserId = AuditValueSanitizer.Limit(
                    GetCurrentUserId(),
                    AuditValueSanitizer.UserIdMaxLength),
                UserName = AuditValueSanitizer.Limit(
                    _userContextService?.UserName,
                    AuditValueSanitizer.UserNameMaxLength),
                IpAddress = AuditValueSanitizer.Limit(
                    GetClientIpAddress(),
                    AuditValueSanitizer.IpAddressMaxLength),
                UserAgent = AuditValueSanitizer.Limit(
                    GetUserAgent(),
                    AuditValueSanitizer.UserAgentMaxLength),
                CorrelationId = AuditValueSanitizer.Limit(
                    GetCorrelationId(),
                    AuditValueSanitizer.CorrelationIdMaxLength),
                SessionId = AuditValueSanitizer.FingerprintSessionId(
                    _userContextService?.SessionId),
                RequestMethod = AuditValueSanitizer.Limit(
                    _httpContextAccessor?.HttpContext?.Request?.Method,
                    AuditValueSanitizer.RequestMethodMaxLength),
                RequestUrl = AuditValueSanitizer.Limit(
                    _httpContextAccessor?.HttpContext?.Request?.Path.Value,
                    AuditValueSanitizer.RequestUrlMaxLength)
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    auditEntry.Action = EAuditAction.Create;
                    foreach (var property in entry.Properties)
                    {
                        if (!ShouldAuditProperty(property))
                            continue;

                        if (property.Metadata.IsPrimaryKey())
                        {
                            auditEntry.EntityId = property.CurrentValue?.ToString();

                            if (property.IsTemporary)
                            {
                                auditEntry.HasTemporaryProperties = true;
                                auditEntry.TemporaryProperties.Add(property);
                            }
                        }

                        auditEntry.NewValues[property.Metadata.Name] = property.CurrentValue;
                    }
                    break;

                case EntityState.Deleted:
                    auditEntry.Action = EAuditAction.Delete;
                    foreach (var property in entry.Properties)
                    {
                        if (!ShouldAuditProperty(property))
                            continue;

                        if (property.Metadata.IsPrimaryKey())
                        {
                            auditEntry.EntityId = property.CurrentValue?.ToString();
                        }
                        auditEntry.OldValues[property.Metadata.Name] = property.OriginalValue;
                    }
                    break;

                case EntityState.Modified:
                    auditEntry.Action = EAuditAction.Update;
                    foreach (var property in entry.Properties)
                    {
                        if (!ShouldAuditProperty(property))
                            continue;

                        if (property.Metadata.IsPrimaryKey())
                        {
                            auditEntry.EntityId = property.CurrentValue?.ToString();
                            continue;
                        }

                        if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                        {
                            auditEntry.ChangedProperties.Add(property.Metadata.Name);
                            auditEntry.OldValues[property.Metadata.Name] = property.OriginalValue;
                            auditEntry.NewValues[property.Metadata.Name] = property.CurrentValue;
                        }
                    }
                    // Only audit if there are actual changes
                    if (auditEntry.ChangedProperties.Count == 0)
                        continue;
                    break;

                default:
                    continue;
            }

            auditEntries.Add(auditEntry);
        }

        return auditEntries;
    }

    /// <summary>
    /// Saves audit entries after the main save completes.
    /// </summary>
    private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries.Count == 0)
            return;

        foreach (var auditEntry in auditEntries)
        {
            // Get the final primary key for newly created entities
            if (auditEntry.HasTemporaryProperties)
            {
                foreach (var prop in auditEntry.TemporaryProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        auditEntry.EntityId = prop.CurrentValue?.ToString();
                    }
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            AuditLogs.Add(auditEntry.ToAuditLog());
        }

        await base.SaveChangesAsync();
    }

    private string? GetClientIpAddress()
    {
        return _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString();
    }

    private string? GetCorrelationId()
    {
        return _httpContextAccessor?.HttpContext?.TraceIdentifier;
    }

    private static bool ShouldSkipAudit(EntityEntry entry)
    {
        return entry.Entity is AuditLog
            || entry.State == EntityState.Detached
            || entry.State == EntityState.Unchanged
            || entry.Entity is not TimestampedEntity;
    }

    private static bool ShouldAuditProperty(PropertyEntry property)
    {
        if (property.Metadata.IsShadowProperty())
            return false;

        return property.Metadata.Name is not nameof(TimestampedEntity.CreatedOn)
            and not nameof(TimestampedEntity.CreatedBy)
            and not nameof(TimestampedEntity.UpdatedOn)
            and not nameof(TimestampedEntity.UpdatedBy);
    }

    private static EAuditCategory ResolveAuditCategory(Type entityType)
    {
        return entityType == typeof(AccessFunction)
               || entityType == typeof(Role)
               || entityType == typeof(RoleAccessFunction)
               || entityType == typeof(UserRole)
            ? EAuditCategory.AccessControl
            : EAuditCategory.Data;
    }

    /// <summary>
    /// Updates the CreatedOn and UpdatedOn timestamps for entities that implement TimestampedEntity.
    /// Also sets CreatedBy and UpdatedBy from the current user context.
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is TimestampedEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        var now = Shared.Helpers.DateTimeHelper.Now;
        var currentUserId = GetCurrentUserId();

        foreach (var entityEntry in entries)
        {
            var entity = (TimestampedEntity)entityEntry.Entity;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedOn = now;
                entity.CreatedBy = currentUserId;
            }
            else
            {
                // Don't update CreatedOn or CreatedBy for modified entities
                entityEntry.Property("CreatedOn").IsModified = false;
                entityEntry.Property("CreatedBy").IsModified = false;
            }

            entity.UpdatedOn = now;
            entity.UpdatedBy = currentUserId;
        }
    }

    private string? GetCurrentUserId()
    {
        return _userContextService?.UserId;
    }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            entity.HasIndex(e => new { e.Category, e.Timestamp });
            entity.HasIndex(e => new { e.Severity, e.Timestamp });
            entity.Property(e => e.OldValues).HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnType("jsonb");
            entity.Property(e => e.ChangedProperties).HasColumnType("jsonb");
            entity.Property(e => e.AdditionalData).HasColumnType("jsonb");
        });

        // Role configuration
        modelBuilder.Entity<AccessFunction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.Type, e.Module, e.DisplayOrder });
            entity.Property(e => e.Code).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Module).HasMaxLength(80).IsRequired();
            entity.Property(e => e.ResourceName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Route).HasMaxLength(200);
            entity.Property(e => e.HttpMethod).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // RoleAccessFunction configuration
        modelBuilder.Entity<RoleAccessFunction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoleId, e.AccessFunctionId }).IsUnique();
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RoleAccessFunctions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AccessFunction)
                  .WithMany(accessFunction => accessFunction.RoleAccessFunctions)
                  .HasForeignKey(e => e.AccessFunctionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Document configuration (polymorphic owner; no hard FK from this layer)
        modelBuilder.Entity<Document>()
            .HasIndex(e => new { e.OwnerType, e.OwnerId });

        // Procurement relationships
        modelBuilder.Entity<Vendor>()
            .HasMany(v => v.CatalogItems)
            .WithOne(c => c.Vendor)
            .HasForeignKey(c => c.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vendor>()
            .HasIndex(v => v.Code).IsUnique();

        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(po => po.Lines)
            .WithOne(l => l.PurchaseOrder)
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(po => po.Approvals)
            .WithOne(a => a.PurchaseOrder)
            .HasForeignKey(a => a.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(po => po.Documents)
            .WithOne(d => d.PurchaseOrder)
            .HasForeignKey(d => d.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Vendor)
            .WithMany(v => v.PurchaseOrders)
            .HasForeignKey(po => po.VendorId);

        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(po => po.PoNumber).IsUnique();

        modelBuilder.Entity<PurchaseOrder>()
            .Property(po => po.TotalAmount).HasColumnType("decimal(18,2)");

        // Persist EApprovalStage as its string name for readability + safe forward-compat
        modelBuilder.Entity<PurchaseOrderApproval>()
            .Property(a => a.ApprovalStage)
            .HasConversion<string>();

        modelBuilder.Entity<PurchaseOrderLine>()
            .Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<PurchaseOrderLine>()
            .Property(l => l.LineTotal).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<CatalogItem>()
            .Property(c => c.UnitPrice).HasColumnType("decimal(18,2)");

        // Code table configuration
        modelBuilder.Entity<Code>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<Code>()
            .HasIndex(c => new { c.Type, c.Name })
            .IsUnique();

        // Workflow configuration
        modelBuilder.Entity<WorkflowTransition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FromState, e.ToState, e.RequiredRole }).IsUnique();
            entity.Property(e => e.FromState).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ToState).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RequiredRole).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayLabel).HasMaxLength(200);
            entity.Property(e => e.UiConditions).HasMaxLength(500);
        });

        modelBuilder.Entity<WorkflowStateLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId });
            entity.HasIndex(e => e.TransitionedAt);
            entity.Property(e => e.FromState).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ToState).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            entity.Property(e => e.PerformedByUserId).HasMaxLength(100);
            entity.Property(e => e.PerformedByName).HasMaxLength(200);
            entity.Property(e => e.PerformedByRole).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(200);
            entity.Property(e => e.OwnerType).HasMaxLength(100).IsRequired();
        });

        // All initial data needed for the application to run should be seeded through this method
        // There cannot be any SQL files used for data seeding, as this architecture uses code-first approach
        // Please find the migration.txt folder in Data project and run the command for migrations.
        // For every model change, create a migration and push it. This will automatically deployed in server
        // as the project startup looks for non applied migrations and migrate itself. (In Program.cs)
        #region Data Seeding

        //Repeat this for other tables that need to be seeded with data
        modelBuilder.Entity<Code>()
            .HasData(
                new Code
                {
                    Id = 1,
                    Type = ECodeType.TITLE.ToString(),
                    Name = ECodeName.MR.ToString(),
                    Description = "",
                    DisplayName = "Mr.",
                    DisplayOrder = 1,
                    IsActive = true
                },
                new Code
                {
                    Id = 2,
                    Type = ECodeType.TITLE.ToString(),
                    Name = ECodeName.MRS.ToString(),
                    Description = "",
                    DisplayName = "Mrs.",
                    DisplayOrder = 2,
                    IsActive = true
                },
                new Code
                {
                    Id = 3,
                    Type = ECodeType.USER_TYPE.ToString(),
                    Name = ECodeName.ADMIN.ToString(),
                    Description = "",
                    DisplayName = "Administrator",
                    DisplayOrder = 3,
                    IsActive = true
                },
                new Code
                {
                    Id = 4,
                    Type = ECodeType.USER_TYPE.ToString(),
                    Name = ECodeName.USER.ToString(),
                    Description = "",
                    DisplayName = "Non-Admin User",
                    DisplayOrder = 4,
                    IsActive = true
                },
                // Vendor Categories
                new Code { Id = 5, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.IT_SERVICES.ToString(), Description = "", DisplayName = "IT Services", DisplayOrder = 5, IsActive = true },
                new Code { Id = 6, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.OFFICE_SUPPLIES.ToString(), Description = "", DisplayName = "Office Supplies", DisplayOrder = 6, IsActive = true },
                new Code { Id = 7, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.MAINTENANCE.ToString(), Description = "", DisplayName = "Maintenance", DisplayOrder = 7, IsActive = true },
                new Code { Id = 8, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.CONSULTING.ToString(), Description = "", DisplayName = "Consulting", DisplayOrder = 8, IsActive = true },
                new Code { Id = 9, Type = ECodeType.VENDOR_CATEGORY.ToString(), Name = ECodeName.LOGISTICS.ToString(), Description = "", DisplayName = "Logistics", DisplayOrder = 9, IsActive = true },
                // Catalog Categories
                new Code { Id = 10, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.HARDWARE.ToString(), Description = "", DisplayName = "Hardware", DisplayOrder = 10, IsActive = true },
                new Code { Id = 11, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.SOFTWARE.ToString(), Description = "", DisplayName = "Software", DisplayOrder = 11, IsActive = true },
                new Code { Id = 12, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.FURNITURE.ToString(), Description = "", DisplayName = "Furniture", DisplayOrder = 12, IsActive = true },
                new Code { Id = 13, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.STATIONERY.ToString(), Description = "", DisplayName = "Stationery", DisplayOrder = 13, IsActive = true },
                new Code { Id = 14, Type = ECodeType.CATALOG_CATEGORY.ToString(), Name = ECodeName.CLEANING.ToString(), Description = "", DisplayName = "Cleaning", DisplayOrder = 14, IsActive = true },
                // Units of Measure
                new Code { Id = 15, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.EACH.ToString(), Description = "", DisplayName = "Each", DisplayOrder = 15, IsActive = true },
                new Code { Id = 16, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.BOX.ToString(), Description = "", DisplayName = "Box", DisplayOrder = 16, IsActive = true },
                new Code { Id = 17, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.PACK.ToString(), Description = "", DisplayName = "Pack", DisplayOrder = 17, IsActive = true },
                new Code { Id = 18, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.SET.ToString(), Description = "", DisplayName = "Set", DisplayOrder = 18, IsActive = true },
                new Code { Id = 19, Type = ECodeType.UNIT_OF_MEASURE.ToString(), Name = ECodeName.HOUR.ToString(), Description = "", DisplayName = "Hour", DisplayOrder = 19, IsActive = true },
                // Delivery Locations
                new Code { Id = 20, Type = ECodeType.DELIVERY_LOCATION.ToString(), Name = ECodeName.MAIN_OFFICE.ToString(), Description = "", DisplayName = "Main Office", DisplayOrder = 20, IsActive = true },
                new Code { Id = 21, Type = ECodeType.DELIVERY_LOCATION.ToString(), Name = ECodeName.WAREHOUSE.ToString(), Description = "", DisplayName = "Warehouse", DisplayOrder = 21, IsActive = true },
                new Code { Id = 22, Type = ECodeType.DELIVERY_LOCATION.ToString(), Name = ECodeName.BRANCH_OFFICE.ToString(), Description = "", DisplayName = "Branch Office", DisplayOrder = 22, IsActive = true },
                // Currencies
                new Code { Id = 23, Type = ECodeType.CURRENCY.ToString(), Name = ECodeName.SGD.ToString(), Description = "", DisplayName = "SGD - Singapore Dollar", DisplayOrder = 23, IsActive = true },
                new Code { Id = 24, Type = ECodeType.CURRENCY.ToString(), Name = ECodeName.USD.ToString(), Description = "", DisplayName = "USD - US Dollar", DisplayOrder = 24, IsActive = true }
        );

        var accessFunctionSeeds = AccessFunctionCatalog.AccessFunctions
            .Select((definition, index) => new AccessFunction
            {
                Id = index + 1,
                Code = definition.Code,
                Name = definition.Name,
                Description = definition.Description,
                Module = definition.Module,
                Type = definition.Type,
                ResourceName = definition.ResourceName,
                Route = definition.Route,
                HttpMethod = definition.HttpMethod,
                IsActive = true,
                IsSystemFunction = true,
                DisplayOrder = definition.DisplayOrder
            })
            .ToList();

        var accessFunctionIdLookup = accessFunctionSeeds.ToDictionary(seed => seed.Code, seed => seed.Id);

        modelBuilder.Entity<AccessFunction>().HasData(accessFunctionSeeds);

        modelBuilder.Entity<Role>().HasData(
            AccessFunctionCatalog.Roles.Select(role => new Role
            {
                Id = role.Id,
                Code = role.Code,
                Name = role.Name,
                Description = role.Description,
                IsActive = true,
                IsSystemRole = true,
                DisplayOrder = role.DisplayOrder
            }));

        var roleAccessFunctionSeedId = 1;
        var roleAccessFunctionSeeds = AccessFunctionCatalog.Roles
            .SelectMany(role => role.AccessFunctionCodes.Select(code => new RoleAccessFunction
            {
                Id = roleAccessFunctionSeedId++,
                RoleId = role.Id,
                AccessFunctionId = accessFunctionIdLookup[code]
            }))
            .ToList();

        modelBuilder.Entity<RoleAccessFunction>().HasData(roleAccessFunctionSeeds);

        // Seed default user roles for development
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole
            {
                Id = 1,
                UserId = "xyfong",
                RoleId = (int)ERole.Administrator,
                AssignedOn = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                IsActive = true
            },
            new UserRole
            {
                Id = 2,
                UserId = "devia",
                RoleId = (int)ERole.Administrator,
                AssignedOn = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                IsActive = true
            }
        );

        // Seed default workflow transitions for PurchaseOrder workflow
        modelBuilder.Entity<WorkflowTransition>().HasData(
            new WorkflowTransition { Id = 1, FromState = "Draft", ToState = "Submitted", RequiredRole = "Administrator", DisplayLabel = "Submit for Review", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 },
            new WorkflowTransition { Id = 2, FromState = "Submitted", ToState = "UnderReview", RequiredRole = "Manager", DisplayLabel = "Start Review", RequiresRemarks = false, IsActive = true, DisplayOrder = 1 },
            new WorkflowTransition { Id = 3, FromState = "UnderReview", ToState = "Approved", RequiredRole = "Manager", DisplayLabel = "Approve", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 },
            new WorkflowTransition { Id = 4, FromState = "UnderReview", ToState = "Rejected", RequiredRole = "Manager", DisplayLabel = "Reject", RequiresRemarks = true, IsActive = true, DisplayOrder = 2 },
            new WorkflowTransition { Id = 5, FromState = "UnderReview", ToState = "ReturnedForRevision", RequiredRole = "Manager", DisplayLabel = "Return for Revision", RequiresRemarks = true, IsActive = true, DisplayOrder = 3 },
            new WorkflowTransition { Id = 6, FromState = "ReturnedForRevision", ToState = "Submitted", RequiredRole = "Administrator", DisplayLabel = "Resubmit", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 },
            new WorkflowTransition { Id = 7, FromState = "Approved", ToState = "Completed", RequiredRole = "Administrator", DisplayLabel = "Mark as Completed", RequiresRemarks = false, IsActive = true, DisplayOrder = 1 },
            new WorkflowTransition { Id = 8, FromState = "Draft", ToState = "Cancelled", RequiredRole = "Administrator", DisplayLabel = "Cancel", RequiresRemarks = true, IsActive = true, DisplayOrder = 2 },
            new WorkflowTransition { Id = 9, FromState = "Submitted", ToState = "Cancelled", RequiredRole = "Administrator", DisplayLabel = "Cancel", RequiresRemarks = true, IsActive = true, DisplayOrder = 2 },
            new WorkflowTransition { Id = 10, FromState = "Rejected", ToState = "Draft", RequiredRole = "Administrator", DisplayLabel = "Re-open as Draft", RequiresRemarks = true, IsActive = true, DisplayOrder = 1 }
        );

        #endregion
    }
}

/// <summary>
/// Helper class for building audit log entries from entity changes.
/// </summary>
internal class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public string EntityName { get; set; } = default!;
    public string? EntityId { get; set; }
    public EAuditAction Action { get; set; }
    public EAuditCategory Category { get; set; }
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<string> ChangedProperties { get; } = new();
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestUrl { get; set; }
    public bool HasTemporaryProperties { get; set; }
    public List<PropertyEntry> TemporaryProperties { get; } = new();

    public AuditLog ToAuditLog()
    {
        return new AuditLog
        {
            EntityName = AuditValueSanitizer.Limit(
                EntityName,
                AuditValueSanitizer.EntityNameMaxLength)!,
            EntityId = AuditValueSanitizer.Limit(
                EntityId,
                AuditValueSanitizer.EntityIdMaxLength),
            Action = Action,
            Category = Category,
            Severity = Action == EAuditAction.Delete ? EAuditSeverity.Warning : EAuditSeverity.Info,
            OldValues = OldValues.Count > 0 ? JsonSerializer.Serialize(OldValues) : null,
            NewValues = NewValues.Count > 0 ? JsonSerializer.Serialize(NewValues) : null,
            ChangedProperties = ChangedProperties.Count > 0 ? JsonSerializer.Serialize(ChangedProperties) : null,
            UserId = AuditValueSanitizer.Limit(
                UserId,
                AuditValueSanitizer.UserIdMaxLength),
            UserName = AuditValueSanitizer.Limit(
                UserName,
                AuditValueSanitizer.UserNameMaxLength),
            Timestamp = Shared.Helpers.DateTimeHelper.Now,
            IpAddress = AuditValueSanitizer.Limit(
                IpAddress,
                AuditValueSanitizer.IpAddressMaxLength),
            UserAgent = AuditValueSanitizer.Limit(
                UserAgent,
                AuditValueSanitizer.UserAgentMaxLength),
            CorrelationId = AuditValueSanitizer.Limit(
                CorrelationId,
                AuditValueSanitizer.CorrelationIdMaxLength),
            SessionId = AuditValueSanitizer.Limit(
                SessionId,
                AuditValueSanitizer.SessionIdMaxLength),
            RequestMethod = AuditValueSanitizer.Limit(
                RequestMethod,
                AuditValueSanitizer.RequestMethodMaxLength),
            RequestUrl = AuditValueSanitizer.Limit(
                RequestUrl,
                AuditValueSanitizer.RequestUrlMaxLength),
            Outcome = "Success"
        };
    }
}
