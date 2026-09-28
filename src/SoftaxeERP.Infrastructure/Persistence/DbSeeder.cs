using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Domain.Enums;
using SoftaxeERP.Infrastructure.Services;

namespace SoftaxeERP.Infrastructure.Persistence;

/// <summary>Minimal Phase 0 demo data. It deliberately does not seed deferred modules.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ITenantContext tenant)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync()) return;

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

        var bag = new UnitOfMeasure { TenantId = tenantId, Code = "Bag", Name = "Bag" };
        var category = new ProductCategory { TenantId = tenantId, Name = "General" };
        var warehouse = new Warehouse { TenantId = tenantId, BranchId = branch.Id, Code = "MAIN", Name = "Main Warehouse", IsDefault = true };
        db.AddRange(bag, category, warehouse);
        await db.SaveChangesAsync();
        var product = new Product { TenantId = tenantId, Code = "DEMO-001", Name = "Demo Product", CategoryId = category.Id, UnitOfMeasureId = bag.Id, PurchasePrice = 100, SalePrice = 125 };
        db.Products.Add(product);
        db.WarehouseStocks.Add(new WarehouseStock { TenantId = tenantId, Product = product, WarehouseId = warehouse.Id, Quantity = 0 });
        db.DocumentTypes.AddRange(
            new DocumentType { TenantId = tenantId, Code = "PO", Name = "Purchase Order", Prefix = "PO" },
            new DocumentType { TenantId = tenantId, Code = "GRN", Name = "Goods Receipt", Prefix = "GRN", AffectsInventory = true },
            new DocumentType { TenantId = tenantId, Code = "PI", Name = "Purchase Invoice", Prefix = "PI", AffectsGl = true });
        var supplier = new Party { TenantId = tenantId, Code = "SUP-001", Name = "Demo Supplier", PartyType = PartyType.Supplier };
        db.Parties.Add(supplier);
        db.SupplierProfiles.Add(new SupplierProfile { TenantId = tenantId, Party = supplier, PaymentTerms = "Net 30" });
        db.Accounts.Add(new Account { TenantId = tenantId, Code = "2100", Name = "Accounts Payable", AccountType = AccountType.Liability, IsPostable = true, IsSystem = true });
        await db.SaveChangesAsync();
    }
}
