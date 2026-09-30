using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Common;
using SoftaxeERP.Domain.Entities;

namespace SoftaxeERP.Infrastructure.Persistence;

/// <summary>Phase 0 only — do not map Farm/POS/Sales/HR/etc. here.</summary>
public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options)
    {
        _tenant = tenant;
    }

    public Guid CurrentTenantId => _tenant.TenantId;

    // Core
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<FinancialYear> FinancialYears => Set<FinancialYear>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Inventory
    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();

    // Documents
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentLine> DocumentLines => Set<DocumentLine>();
    public DbSet<DocumentNumberSequence> DocumentNumberSequences => Set<DocumentNumberSequence>();
    public DbSet<Verification> Verifications => Set<Verification>();
    public DbSet<Approval> Approvals => Set<Approval>();

    // Procurement
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<SupplierProfile> SupplierProfiles => Set<SupplierProfile>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();

    // Finance stub
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PayableBalance> PayableBalances => Set<PayableBalance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<Tenant>().ToTable("Tenants", "core").HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Tenant>().HasIndex(x => x.Slug);
        modelBuilder.Entity<Company>().ToTable("Companies", "core");
        modelBuilder.Entity<Branch>().ToTable("Branches", "core")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Branch>()
            .HasOne(x => x.Company).WithMany(x => x.Branches)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FinancialYear>().ToTable("FinancialYears", "core")
            .HasIndex(x => new { x.TenantId, x.CompanyId, x.Code }).IsUnique();
        modelBuilder.Entity<FiscalPeriod>().ToTable("FiscalPeriods", "core");
        modelBuilder.Entity<FiscalPeriod>()
            .HasOne(x => x.FinancialYear).WithMany(x => x.Periods)
            .HasForeignKey(x => x.FinancialYearId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AuditLog>().ToTable("AuditLogs", "core");

        modelBuilder.Entity<User>().ToTable("Users", "identity")
            .HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        modelBuilder.Entity<Role>().ToTable("Roles", "identity")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Permission>().ToTable("Permissions", "identity")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<UserRole>().ToTable("UserRoles", "identity")
            .HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RolePermission>().ToTable("RolePermissions", "identity")
            .HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductGroup>().ToTable("ProductGroups", "inventory")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<ProductCategory>().ToTable("ProductCategories", "inventory");
        modelBuilder.Entity<Brand>().ToTable("Brands", "inventory")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<UnitOfMeasure>().ToTable("UnitsOfMeasure", "inventory")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Product>().ToTable("Products", "inventory")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Product>()
            .HasOne(x => x.ProductGroup).WithMany(x => x.Products).HasForeignKey(x => x.ProductGroupId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Product>()
            .HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Product>()
            .HasOne(x => x.Brand).WithMany(x => x.Products).HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Product>()
            .HasOne(x => x.UnitOfMeasure).WithMany(x => x.Products).HasForeignKey(x => x.UnitOfMeasureId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Warehouse>().ToTable("Warehouses", "inventory")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Warehouse>()
            .HasOne(x => x.Branch).WithMany(x => x.Warehouses).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<WarehouseStock>().ToTable("WarehouseStocks", "inventory")
            .HasIndex(x => new { x.TenantId, x.ProductId, x.WarehouseId }).IsUnique();
        modelBuilder.Entity<WarehouseStock>()
            .HasOne(x => x.Product).WithMany(x => x.Stocks).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WarehouseStock>()
            .HasOne(x => x.Warehouse).WithMany(x => x.Stocks).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentType>().ToTable("DocumentTypes", "documents")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Document>().ToTable("Documents", "documents")
            .HasIndex(x => new { x.TenantId, x.DocumentNo }).IsUnique();
        modelBuilder.Entity<Document>()
            .HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Document>()
            .HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Document>()
            .HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Document>()
            .HasOne(x => x.FinancialYear).WithMany().HasForeignKey(x => x.FinancialYearId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Document>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentLine>().ToTable("DocumentLines", "documents");
        modelBuilder.Entity<DocumentLine>()
            .HasOne(x => x.Document).WithMany(x => x.Lines).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DocumentNumberSequence>().ToTable("DocumentNumberSequences", "documents")
            .HasIndex(x => new { x.TenantId, x.CompanyId, x.BranchId, x.DocumentTypeId, x.FinancialYearId }).IsUnique();
        modelBuilder.Entity<DocumentNumberSequence>()
            .HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentNumberSequence>()
            .HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentNumberSequence>()
            .HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentNumberSequence>()
            .HasOne(x => x.FinancialYear).WithMany().HasForeignKey(x => x.FinancialYearId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Verification>().ToTable("Verifications", "documents");
        modelBuilder.Entity<Verification>()
            .HasOne(x => x.Document).WithMany(x => x.Verifications).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Approval>().ToTable("Approvals", "documents");
        modelBuilder.Entity<Approval>()
            .HasOne(x => x.Document).WithMany(x => x.Approvals).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Party>().ToTable("Parties", "procurement")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<SupplierProfile>().ToTable("SupplierProfiles", "procurement")
            .HasIndex(x => x.PartyId).IsUnique();
        modelBuilder.Entity<SupplierProfile>()
            .HasOne(x => x.Party).WithOne(x => x.SupplierProfile)
            .HasForeignKey<SupplierProfile>(x => x.PartyId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PurchaseOrder>().ToTable("PurchaseOrders", "procurement")
            .HasIndex(x => new { x.TenantId, x.OrderNo }).IsUnique();
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrderLine>().ToTable("PurchaseOrderLines", "procurement");
        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(x => x.PurchaseOrder).WithMany(x => x.Lines).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GoodsReceipt>().ToTable("GoodsReceipts", "procurement")
            .HasIndex(x => new { x.TenantId, x.ReceiptNo }).IsUnique();
        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(x => x.PurchaseOrder).WithMany(x => x.GoodsReceipts).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GoodsReceiptLine>().ToTable("GoodsReceiptLines", "procurement");
        modelBuilder.Entity<GoodsReceiptLine>()
            .HasOne(x => x.GoodsReceipt).WithMany(x => x.Lines).HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<GoodsReceiptLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GoodsReceiptLine>()
            .HasOne(x => x.PurchaseOrderLine).WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseInvoice>().ToTable("PurchaseInvoices", "procurement")
            .HasIndex(x => new { x.TenantId, x.InvoiceNo }).IsUnique();
        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(x => x.GoodsReceipt).WithMany().HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseInvoiceLine>().ToTable("PurchaseInvoiceLines", "procurement");
        modelBuilder.Entity<PurchaseInvoiceLine>()
            .HasOne(x => x.PurchaseInvoice).WithMany(x => x.Lines).HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PurchaseInvoiceLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Account>().ToTable("Accounts", "finance")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Account>()
            .HasOne(x => x.Parent).WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PayableBalance>().ToTable("PayableBalances", "finance")
            .HasIndex(x => new { x.TenantId, x.PartyId }).IsUnique();
        modelBuilder.Entity<PayableBalance>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)) continue;
            // Tenants are platform-scoped: soft-delete only (platform admin lists all)
            if (entityType.ClrType == typeof(Tenant))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildSoftDeleteOnlyFilter(entityType.ClrType));
                continue;
            }
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(BuildTenantSoftDeleteFilter(entityType.ClrType));
        }
    }

    private static System.Linq.Expressions.LambdaExpression BuildSoftDeleteOnlyFilter(Type entityType)
    {
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var isDeleted = System.Linq.Expressions.Expression.Property(param, nameof(BaseEntity.IsDeleted));
        var notDeleted = System.Linq.Expressions.Expression.Equal(isDeleted, System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(notDeleted, param);
    }

    private System.Linq.Expressions.LambdaExpression BuildTenantSoftDeleteFilter(Type entityType)
    {
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var tenantId = System.Linq.Expressions.Expression.Property(param, nameof(BaseEntity.TenantId));
        var isDeleted = System.Linq.Expressions.Expression.Property(param, nameof(BaseEntity.IsDeleted));
        var currentTenant = System.Linq.Expressions.Expression.Property(
            System.Linq.Expressions.Expression.Constant(this), nameof(CurrentTenantId));
        var tenantMatch = System.Linq.Expressions.Expression.Equal(tenantId, currentTenant);
        var notDeleted = System.Linq.Expressions.Expression.Equal(isDeleted, System.Linq.Expressions.Expression.Constant(false));
        var body = System.Linq.Expressions.Expression.AndAlso(tenantMatch, notDeleted);
        return System.Linq.Expressions.Expression.Lambda(body, param);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty)
                    entry.Entity.TenantId = _tenant.TenantId;
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
