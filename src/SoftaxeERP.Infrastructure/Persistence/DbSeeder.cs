using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Domain.Enums;
using SoftaxeERP.Infrastructure.Services;

namespace SoftaxeERP.Infrastructure.Persistence;

/// <summary>Minimal Phase 0/1 demo data for SheikhGo_Nexus_Dev.</summary>
public static class DbSeeder
{
    public static readonly (string Code, string Name, string Module)[] PermissionCatalog =
    [
        ("Inventory.Product.View", "View products", "Inventory"),
        ("Inventory.Product.Create", "Create products", "Inventory"),
        ("Inventory.Product.Edit", "Edit products", "Inventory"),

        ("Procurement.PurchaseOrder.View", "View purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Create", "Create purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Edit", "Edit purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Submit", "Submit purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Approve", "Approve purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Post", "Post purchase orders", "Procurement"),

        ("Procurement.GoodsReceipt.View", "View goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Create", "Create goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Edit", "Edit goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Submit", "Submit goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Approve", "Approve goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Post", "Post goods receipts", "Procurement"),

        ("Procurement.PurchaseInvoice.View", "View purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Create", "Create purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Edit", "Edit purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Submit", "Submit purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Approve", "Approve purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Post", "Post purchase invoices", "Procurement"),

        ("Admin.Company.View", "View company", "Admin"),
        ("Admin.Company.Edit", "Edit company", "Admin"),
        ("Admin.Branch.View", "View branches", "Admin"),
        ("Admin.Branch.Create", "Create branches", "Admin"),
        ("Admin.Branch.Edit", "Edit branches", "Admin"),
        ("Admin.Role.View", "View roles", "Admin"),
        ("Admin.Role.Create", "Create roles", "Admin"),
        ("Admin.Role.Edit", "Edit role permissions", "Admin"),
        ("Admin.Permission.View", "View permissions", "Admin"),
        ("Admin.User.Edit", "Assign user roles", "Admin"),
        ("Admin.FinancialYear.View", "View financial years", "Admin"),
        ("Admin.FinancialYear.Create", "Create financial years", "Admin"),
        ("Admin.FinancialYear.Edit", "Edit financial years", "Admin"),
        ("Admin.Audit.View", "View audit logs", "Admin"),
        ("Admin.DocumentType.View", "View document types", "Admin"),
        ("Admin.DocumentType.Edit", "Edit document types", "Admin")
    ];

    public static async Task SeedAsync(AppDbContext db, ITenantContext tenant)
    {
        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync())
            await SeedCoreAsync(db, tenant);

        await SeedPermissionsAsync(db);
        await EnsureDocumentTypeFlagsAsync(db);
    }

    /// <summary>Idempotent flags for PO/GRN/PI lifecycle (existing DBs from Phase 0).</summary>
    public static async Task EnsureDocumentTypeFlagsAsync(AppDbContext db)
    {
        var types = await db.DocumentTypes.IgnoreQueryFilters().ToListAsync();
        foreach (var t in types)
        {
            switch (t.Code.ToUpperInvariant())
            {
                case "PO":
                    t.RequiresVerification = false;
                    t.RequiresApproval = true;
                    break;
                case "GRN":
                    t.RequiresVerification = true;
                    t.RequiresApproval = false;
                    t.AffectsInventory = true;
                    break;
                case "PI":
                    t.RequiresVerification = true;
                    t.RequiresApproval = true;
                    t.AffectsGl = true;
                    break;
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedCoreAsync(AppDbContext db, ITenantContext tenant)
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        tenant.Set(tenantId, null);
        var now = DateTime.UtcNow;
        var tenantEntity = new Tenant { Id = tenantId, TenantId = tenantId, Name = "Softaxe Demo", Code = "DEMO", Slug = "demo", Industry = IndustryType.FeedMill };
        db.Tenants.Add(tenantEntity);
        var company = new Company { TenantId = tenantId, Name = "Softaxe Demo", ShortName = "SDEMO", City = "Multan", Email = "admin@softaxe.local", Industry = IndustryType.FeedMill };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var branch = new Branch { TenantId = tenantId, CompanyId = company.Id, Code = "MAIN", Name = "Main Branch", IsDefault = true };
        var year = new FinancialYear { TenantId = tenantId, CompanyId = company.Id, Code = now.Year.ToString(), StartDate = new DateTime(now.Year, 1, 1), EndDate = new DateTime(now.Year, 12, 31), IsCurrent = true };
        var adminRole = new Role { TenantId = tenantId, Code = "Admin", Name = "Administrator" };
        var admin = new User { TenantId = tenantId, Email = "admin@softaxe.local", FullName = "System Admin", IsPlatformAdmin = true, PasswordHash = AuthService.HashPassword("Admin@123") };
        db.AddRange(branch, year, adminRole, admin);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new UserRole { TenantId = tenantId, UserId = admin.Id, RoleId = adminRole.Id });

        var bag = new UnitOfMeasure { TenantId = tenantId, Code = "BAG", Name = "Bag" };
        var category = new ProductCategory { TenantId = tenantId, Name = "General" };
        var warehouse = new Warehouse { TenantId = tenantId, BranchId = branch.Id, Code = "MAIN", Name = "Main Warehouse", IsDefault = true };
        db.AddRange(bag, category, warehouse);
        await db.SaveChangesAsync();
        var product = new Product { TenantId = tenantId, Code = "DEMO-001", Name = "Demo Product", CategoryId = category.Id, UnitOfMeasureId = bag.Id, PurchasePrice = 100, SalePrice = 125 };
        db.Products.Add(product);
        db.WarehouseStocks.Add(new WarehouseStock { TenantId = tenantId, Product = product, WarehouseId = warehouse.Id, Quantity = 0 });
        db.DocumentTypes.AddRange(
            new DocumentType { TenantId = tenantId, Code = "PO", Name = "Purchase Order", Prefix = "PO", RequiresVerification = false, RequiresApproval = true },
            new DocumentType { TenantId = tenantId, Code = "GRN", Name = "Goods Receipt", Prefix = "GRN", RequiresVerification = true, RequiresApproval = false, AffectsInventory = true },
            new DocumentType { TenantId = tenantId, Code = "PI", Name = "Purchase Invoice", Prefix = "PI", RequiresVerification = true, RequiresApproval = true, AffectsGl = true });
        var supplier = new Party { TenantId = tenantId, Code = "SUP-001", Name = "Demo Supplier", PartyType = PartyType.Supplier };
        db.Parties.Add(supplier);
        db.SupplierProfiles.Add(new SupplierProfile { TenantId = tenantId, Party = supplier, PaymentTerms = "Net 30" });
        db.Accounts.Add(new Account { TenantId = tenantId, Code = "2100", Name = "Accounts Payable", AccountType = AccountType.Liability, IsPostable = true, IsSystem = true });
        await db.SaveChangesAsync();
    }

    /// <summary>Idempotent permission catalog + assign all to Admin role(s).</summary>
    public static async Task SeedPermissionsAsync(AppDbContext db)
    {
        var existing = await db.Permissions.IgnoreQueryFilters().ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        foreach (var (code, name, module) in PermissionCatalog)
        {
            if (existing.ContainsKey(code)) continue;
            db.Permissions.Add(new Permission
            {
                TenantId = tenantId,
                Code = code,
                Name = name,
                Module = module
            });
        }
        await db.SaveChangesAsync();

        var allPermissions = await db.Permissions.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync();
        var adminRoles = await db.Roles.IgnoreQueryFilters()
            .Include(x => x.RolePermissions)
            .Where(x => x.TenantId == tenantId && x.Code == "Admin")
            .ToListAsync();

        foreach (var role in adminRoles)
        {
            var have = role.RolePermissions.Select(x => x.PermissionId).ToHashSet();
            foreach (var permission in allPermissions)
            {
                if (have.Contains(permission.Id)) continue;
                db.RolePermissions.Add(new RolePermission
                {
                    TenantId = tenantId,
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
