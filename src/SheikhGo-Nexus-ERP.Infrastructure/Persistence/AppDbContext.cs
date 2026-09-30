using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Common;
using SheikhGoNexusERP.Domain.Entities;

namespace SheikhGoNexusERP.Infrastructure.Persistence;

/// <summary>Phase 0–2 Nexus DbContext (Core, Procurement, Sales). Softaxe is never mapped here.</summary>
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
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<UnitConversion> UnitConversions => Set<UnitConversion>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentLine> StockAdjustmentLines => Set<StockAdjustmentLine>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();

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
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();

    // Sales
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DeliveryOrderLine> DeliveryOrderLines => Set<DeliveryOrderLine>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<CustomerPaymentApplication> CustomerPaymentApplications => Set<CustomerPaymentApplication>();

    // Finance
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PayableBalance> PayableBalances => Set<PayableBalance>();
    public DbSet<ReceivableBalance> ReceivableBalances => Set<ReceivableBalance>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<CashBankAccount> CashBankAccounts => Set<CashBankAccount>();
    public DbSet<CashBankTransaction> CashBankTransactions => Set<CashBankTransaction>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<SupplierPaymentApplication> SupplierPaymentApplications => Set<SupplierPaymentApplication>();
    public DbSet<GlAccountMapping> GlAccountMappings => Set<GlAccountMapping>();

    // HR
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollComponent> PayrollComponents => Set<PayrollComponent>();
    public DbSet<PayrollProfile> PayrollProfiles => Set<PayrollProfile>();
    public DbSet<PayrollProfileLine> PayrollProfileLines => Set<PayrollProfileLine>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollEmployee> PayrollEmployees => Set<PayrollEmployee>();
    public DbSet<PayrollLine> PayrollLines => Set<PayrollLine>();
    public DbSet<SalaryPayment> SalaryPayments => Set<SalaryPayment>();

    // Production
    public DbSet<BillOfMaterial> BillOfMaterials => Set<BillOfMaterial>();
    public DbSet<BillOfMaterialLine> BillOfMaterialLines => Set<BillOfMaterialLine>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<ProductionOrderComponent> ProductionOrderComponents => Set<ProductionOrderComponent>();
    public DbSet<ProductionOrderOutput> ProductionOrderOutputs => Set<ProductionOrderOutput>();
    public DbSet<ProductionVarianceLine> ProductionVarianceLines => Set<ProductionVarianceLine>();

    // Farm
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<FarmUnit> FarmUnits => Set<FarmUnit>();
    public DbSet<Flock> Flocks => Set<Flock>();
    public DbSet<MortalityRecord> MortalityRecords => Set<MortalityRecord>();
    public DbSet<FarmTreatment> FarmTreatments => Set<FarmTreatment>();
    public DbSet<FeedIssue> FeedIssues => Set<FeedIssue>();
    public DbSet<FeedIssueLine> FeedIssueLines => Set<FeedIssueLine>();
    public DbSet<FarmExpense> FarmExpenses => Set<FarmExpense>();

    // Quality / Lab
    public DbSet<LabTestType> LabTestTypes => Set<LabTestType>();
    public DbSet<LabSample> LabSamples => Set<LabSample>();
    public DbSet<LabTestResult> LabTestResults => Set<LabTestResult>();

    // Operations
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<VehicleAssignment> VehicleAssignments => Set<VehicleAssignment>();
    public DbSet<GatePass> GatePasses => Set<GatePass>();
    public DbSet<GatePassLine> GatePassLines => Set<GatePassLine>();

    // Weighbridge
    public DbSet<WeighbridgeScale> WeighbridgeScales => Set<WeighbridgeScale>();
    public DbSet<Weighment> Weighments => Set<Weighment>();

    // POS
    public DbSet<PosTerminal> PosTerminals => Set<PosTerminal>();
    public DbSet<PosShift> PosShifts => Set<PosShift>();
    public DbSet<PosSale> PosSales => Set<PosSale>();
    public DbSet<PosSaleLine> PosSaleLines => Set<PosSaleLine>();
    public DbSet<PosPayment> PosPayments => Set<PosPayment>();

    // Phase 10A / 10B / 10C / 10D AI
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<AiPendingAction> AiPendingActions => Set<AiPendingAction>();
    public DbSet<AiAgentRun> AiAgentRuns => Set<AiAgentRun>();
    public DbSet<AiAgentStep> AiAgentSteps => Set<AiAgentStep>();
    public DbSet<AiPrediction> AiPredictions => Set<AiPrediction>();

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

        modelBuilder.Entity<WarehouseLocation>().ToTable("WarehouseLocations", "inventory")
            .HasIndex(x => new { x.TenantId, x.WarehouseId, x.Code }).IsUnique();
        modelBuilder.Entity<WarehouseLocation>()
            .HasOne(x => x.Warehouse).WithMany(x => x.Locations).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UnitConversion>().ToTable("UnitConversions", "inventory");
        modelBuilder.Entity<UnitConversion>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UnitConversion>()
            .HasOne(x => x.FromUom).WithMany().HasForeignKey(x => x.FromUomId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UnitConversion>()
            .HasOne(x => x.ToUom).WithMany().HasForeignKey(x => x.ToUomId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>().ToTable("InventoryTransactions", "inventory");
        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(x => new { x.TenantId, x.SourceType, x.SourceEntityId, x.SourceLineId, x.WarehouseId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.WarehouseLocation).WithMany().HasForeignKey(x => x.WarehouseLocationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransfer>().ToTable("StockTransfers", "inventory")
            .HasIndex(x => new { x.TenantId, x.TransferNo }).IsUnique();
        modelBuilder.Entity<StockTransfer>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockTransfer>()
            .HasOne(x => x.FromWarehouse).WithMany().HasForeignKey(x => x.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockTransfer>()
            .HasOne(x => x.ToWarehouse).WithMany().HasForeignKey(x => x.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockTransferLine>().ToTable("StockTransferLines", "inventory");
        modelBuilder.Entity<StockTransferLine>()
            .HasOne(x => x.StockTransfer).WithMany(x => x.Lines).HasForeignKey(x => x.StockTransferId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockTransferLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAdjustment>().ToTable("StockAdjustments", "inventory")
            .HasIndex(x => new { x.TenantId, x.AdjustmentNo }).IsUnique();
        modelBuilder.Entity<StockAdjustment>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockAdjustment>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockAdjustmentLine>().ToTable("StockAdjustmentLines", "inventory");
        modelBuilder.Entity<StockAdjustmentLine>()
            .HasOne(x => x.StockAdjustment).WithMany(x => x.Lines).HasForeignKey(x => x.StockAdjustmentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockAdjustmentLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockCount>().ToTable("StockCounts", "inventory")
            .HasIndex(x => new { x.TenantId, x.CountNo }).IsUnique();
        modelBuilder.Entity<StockCount>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockCount>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockCountLine>().ToTable("StockCountLines", "inventory");
        modelBuilder.Entity<StockCountLine>()
            .HasOne(x => x.StockCount).WithMany(x => x.Lines).HasForeignKey(x => x.StockCountId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockCountLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

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
        modelBuilder.Entity<CustomerProfile>().ToTable("CustomerProfiles", "procurement")
            .HasIndex(x => x.PartyId).IsUnique();
        modelBuilder.Entity<CustomerProfile>()
            .HasOne(x => x.Party).WithOne(x => x.CustomerProfile)
            .HasForeignKey<CustomerProfile>(x => x.PartyId).OnDelete(DeleteBehavior.Cascade);
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
        modelBuilder.Entity<ReceivableBalance>().ToTable("ReceivableBalances", "finance")
            .HasIndex(x => new { x.TenantId, x.PartyId }).IsUnique();
        modelBuilder.Entity<ReceivableBalance>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalesOrder>().ToTable("SalesOrders", "sales")
            .HasIndex(x => new { x.TenantId, x.OrderNo }).IsUnique();
        modelBuilder.Entity<SalesOrder>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrder>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrder>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrderLine>().ToTable("SalesOrderLines", "sales");
        modelBuilder.Entity<SalesOrderLine>()
            .HasOne(x => x.SalesOrder).WithMany(x => x.Lines).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SalesOrderLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeliveryOrder>().ToTable("DeliveryOrders", "sales")
            .HasIndex(x => new { x.TenantId, x.DeliveryNo }).IsUnique();
        modelBuilder.Entity<DeliveryOrder>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryOrder>()
            .HasOne(x => x.SalesOrder).WithMany(x => x.DeliveryOrders).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryOrder>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryOrder>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryOrderLine>().ToTable("DeliveryOrderLines", "sales");
        modelBuilder.Entity<DeliveryOrderLine>()
            .HasOne(x => x.DeliveryOrder).WithMany(x => x.Lines).HasForeignKey(x => x.DeliveryOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DeliveryOrderLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryOrderLine>()
            .HasOne(x => x.SalesOrderLine).WithMany().HasForeignKey(x => x.SalesOrderLineId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalesInvoice>().ToTable("SalesInvoices", "sales")
            .HasIndex(x => new { x.TenantId, x.InvoiceNo }).IsUnique();
        modelBuilder.Entity<SalesInvoice>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesInvoice>()
            .HasOne(x => x.DeliveryOrder).WithMany().HasForeignKey(x => x.DeliveryOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesInvoice>()
            .HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesInvoice>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesInvoiceLine>().ToTable("SalesInvoiceLines", "sales");
        modelBuilder.Entity<SalesInvoiceLine>()
            .HasOne(x => x.SalesInvoice).WithMany(x => x.Lines).HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SalesInvoiceLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerPayment>().ToTable("CustomerPayments", "sales")
            .HasIndex(x => new { x.TenantId, x.PaymentNo }).IsUnique();
        modelBuilder.Entity<CustomerPayment>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerPayment>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerPayment>()
            .HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerPaymentApplication>().ToTable("CustomerPaymentApplications", "sales");
        modelBuilder.Entity<CustomerPaymentApplication>()
            .HasOne(x => x.CustomerPayment).WithMany(x => x.Applications).HasForeignKey(x => x.CustomerPaymentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CustomerPaymentApplication>()
            .HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JournalEntry>().ToTable("JournalEntries", "finance")
            .HasIndex(x => new { x.TenantId, x.JournalNo }).IsUnique();
        modelBuilder.Entity<JournalEntry>()
            .HasIndex(x => new { x.TenantId, x.SourceType, x.SourceEntityId });
        modelBuilder.Entity<JournalEntry>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<JournalEntry>()
            .HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<JournalEntry>()
            .HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<JournalEntry>()
            .HasOne(x => x.FinancialYear).WithMany().HasForeignKey(x => x.FinancialYearId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<JournalEntry>()
            .HasOne(x => x.FiscalPeriod).WithMany().HasForeignKey(x => x.FiscalPeriodId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<JournalEntryLine>().ToTable("JournalEntryLines", "finance");
        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(x => x.JournalEntry).WithMany(x => x.Lines).HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CashBankAccount>().ToTable("CashBankAccounts", "finance")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<CashBankAccount>()
            .HasOne(x => x.GlAccount).WithMany().HasForeignKey(x => x.GlAccountId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashBankTransaction>().ToTable("CashBankTransactions", "finance");
        modelBuilder.Entity<CashBankTransaction>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashBankTransaction>()
            .HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashBankTransaction>()
            .HasOne(x => x.OppositeCashBankAccount).WithMany().HasForeignKey(x => x.OppositeCashBankAccountId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashBankTransaction>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashBankTransaction>()
            .HasOne(x => x.JournalEntry).WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupplierPayment>().ToTable("SupplierPayments", "procurement")
            .HasIndex(x => new { x.TenantId, x.PaymentNo }).IsUnique();
        modelBuilder.Entity<SupplierPayment>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierPayment>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierPayment>()
            .HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierPaymentApplication>().ToTable("SupplierPaymentApplications", "procurement");
        modelBuilder.Entity<SupplierPaymentApplication>()
            .HasOne(x => x.SupplierPayment).WithMany(x => x.Applications).HasForeignKey(x => x.SupplierPaymentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SupplierPaymentApplication>()
            .HasOne(x => x.PurchaseInvoice).WithMany().HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GlAccountMapping>().ToTable("GlAccountMappings", "finance")
            .HasIndex(x => new { x.TenantId, x.MappingKey }).IsUnique();
        modelBuilder.Entity<GlAccountMapping>()
            .HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Department>().ToTable("Departments", "hr")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Designation>().ToTable("Designations", "hr")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

        modelBuilder.Entity<Employee>().ToTable("Employees", "hr")
            .HasIndex(x => new { x.TenantId, x.EmployeeCode }).IsUnique();
        modelBuilder.Entity<Employee>()
            .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Employee>()
            .HasOne(x => x.Designation).WithMany().HasForeignKey(x => x.DesignationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Employee>()
            .HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PayrollComponent>().ToTable("PayrollComponents", "hr")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

        modelBuilder.Entity<PayrollProfile>().ToTable("PayrollProfiles", "hr")
            .HasIndex(x => x.EmployeeId).IsUnique();
        modelBuilder.Entity<PayrollProfile>()
            .HasOne(x => x.Employee).WithOne(x => x.PayrollProfile)
            .HasForeignKey<PayrollProfile>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PayrollProfileLine>().ToTable("PayrollProfileLines", "hr");
        modelBuilder.Entity<PayrollProfileLine>()
            .HasOne(x => x.PayrollProfile).WithMany(x => x.Lines).HasForeignKey(x => x.PayrollProfileId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PayrollProfileLine>()
            .HasOne(x => x.PayrollComponent).WithMany().HasForeignKey(x => x.PayrollComponentId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>().ToTable("Attendances", "hr")
            .HasIndex(x => new { x.TenantId, x.EmployeeId, x.Date }).IsUnique();
        modelBuilder.Entity<Attendance>()
            .HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeaveType>().ToTable("LeaveTypes", "hr")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<LeaveBalance>().ToTable("LeaveBalances", "hr")
            .HasIndex(x => new { x.TenantId, x.EmployeeId, x.LeaveTypeId, x.Year }).IsUnique();
        modelBuilder.Entity<LeaveBalance>()
            .HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LeaveBalance>()
            .HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LeaveRequest>().ToTable("LeaveRequests", "hr")
            .HasIndex(x => new { x.TenantId, x.RequestNo }).IsUnique();
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PayrollPeriod>().ToTable("PayrollPeriods", "hr")
            .HasIndex(x => new { x.TenantId, x.Year, x.Month }).IsUnique();
        modelBuilder.Entity<PayrollRun>().ToTable("PayrollRuns", "hr")
            .HasIndex(x => new { x.TenantId, x.RunNo }).IsUnique();
        modelBuilder.Entity<PayrollRun>()
            .HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PayrollRun>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PayrollEmployee>().ToTable("PayrollEmployees", "hr");
        modelBuilder.Entity<PayrollEmployee>()
            .HasOne(x => x.PayrollRun).WithMany(x => x.Employees).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PayrollEmployee>()
            .HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PayrollLine>().ToTable("PayrollLines", "hr");
        modelBuilder.Entity<PayrollLine>()
            .HasOne(x => x.PayrollEmployee).WithMany(x => x.Lines).HasForeignKey(x => x.PayrollEmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PayrollLine>()
            .HasOne(x => x.PayrollComponent).WithMany().HasForeignKey(x => x.PayrollComponentId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalaryPayment>().ToTable("SalaryPayments", "hr")
            .HasIndex(x => new { x.TenantId, x.PaymentNo }).IsUnique();
        modelBuilder.Entity<SalaryPayment>()
            .HasOne(x => x.PayrollRun).WithMany(x => x.Payments).HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalaryPayment>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalaryPayment>()
            .HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillOfMaterial>().ToTable("BillOfMaterials", "production")
            .HasIndex(x => new { x.TenantId, x.Code, x.Version }).IsUnique();
        modelBuilder.Entity<BillOfMaterial>()
            .HasOne(x => x.FinishedProduct).WithMany().HasForeignKey(x => x.FinishedProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BillOfMaterialLine>().ToTable("BillOfMaterialLines", "production");
        modelBuilder.Entity<BillOfMaterialLine>()
            .HasOne(x => x.BillOfMaterial).WithMany(x => x.Lines).HasForeignKey(x => x.BillOfMaterialId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BillOfMaterialLine>()
            .HasOne(x => x.ComponentProduct).WithMany().HasForeignKey(x => x.ComponentProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BillOfMaterialLine>()
            .HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductionOrder>().ToTable("ProductionOrders", "production")
            .HasIndex(x => new { x.TenantId, x.OrderNo }).IsUnique();
        modelBuilder.Entity<ProductionOrder>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionOrder>()
            .HasOne(x => x.Bom).WithMany().HasForeignKey(x => x.BomId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionOrder>()
            .HasOne(x => x.FinishedProduct).WithMany().HasForeignKey(x => x.FinishedProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionOrder>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionOrderComponent>().ToTable("ProductionOrderComponents", "production");
        modelBuilder.Entity<ProductionOrderComponent>()
            .HasOne(x => x.ProductionOrder).WithMany(x => x.Components).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductionOrderComponent>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionOrderOutput>().ToTable("ProductionOrderOutputs", "production");
        modelBuilder.Entity<ProductionOrderOutput>()
            .HasOne(x => x.ProductionOrder).WithMany(x => x.Outputs).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductionOrderOutput>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionVarianceLine>().ToTable("ProductionVarianceLines", "production");
        modelBuilder.Entity<ProductionVarianceLine>()
            .HasOne(x => x.ProductionOrder).WithMany(x => x.Variances).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductionVarianceLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Farm>().ToTable("Farms", "farm")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Farm>()
            .HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FarmUnit>().ToTable("FarmUnits", "farm")
            .HasIndex(x => new { x.TenantId, x.FarmId, x.Code }).IsUnique();
        modelBuilder.Entity<FarmUnit>()
            .HasOne(x => x.Farm).WithMany(x => x.Units).HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Flock>().ToTable("Flocks", "farm")
            .HasIndex(x => new { x.TenantId, x.BatchNo }).IsUnique();
        modelBuilder.Entity<Flock>()
            .HasOne(x => x.Farm).WithMany(x => x.Flocks).HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Flock>()
            .HasOne(x => x.FarmUnit).WithMany().HasForeignKey(x => x.FarmUnitId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MortalityRecord>().ToTable("MortalityRecords", "farm");
        modelBuilder.Entity<MortalityRecord>()
            .HasOne(x => x.Flock).WithMany(x => x.Mortalities).HasForeignKey(x => x.FlockId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MortalityRecord>()
            .HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FarmTreatment>().ToTable("FarmTreatments", "farm")
            .HasIndex(x => new { x.TenantId, x.TreatmentNo }).IsUnique();
        modelBuilder.Entity<FarmTreatment>()
            .HasOne(x => x.Flock).WithMany(x => x.Treatments).HasForeignKey(x => x.FlockId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FarmTreatment>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FarmTreatment>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FarmTreatment>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeedIssue>().ToTable("FeedIssues", "farm")
            .HasIndex(x => new { x.TenantId, x.IssueNo }).IsUnique();
        modelBuilder.Entity<FeedIssue>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeedIssue>()
            .HasOne(x => x.Farm).WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeedIssue>()
            .HasOne(x => x.Flock).WithMany(x => x.FeedIssues).HasForeignKey(x => x.FlockId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeedIssue>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeedIssueLine>().ToTable("FeedIssueLines", "farm");
        modelBuilder.Entity<FeedIssueLine>()
            .HasOne(x => x.FeedIssue).WithMany(x => x.Lines).HasForeignKey(x => x.FeedIssueId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FeedIssueLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FarmExpense>().ToTable("FarmExpenses", "farm")
            .HasIndex(x => new { x.TenantId, x.ExpenseNo }).IsUnique();
        modelBuilder.Entity<FarmExpense>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FarmExpense>()
            .HasOne(x => x.Farm).WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FarmExpense>()
            .HasOne(x => x.Flock).WithMany().HasForeignKey(x => x.FlockId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FarmExpense>()
            .HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LabTestType>().ToTable("LabTestTypes", "quality")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<LabSample>().ToTable("LabSamples", "quality")
            .HasIndex(x => new { x.TenantId, x.SampleNo }).IsUnique();
        modelBuilder.Entity<LabSample>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LabSample>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LabSample>()
            .HasOne(x => x.Flock).WithMany().HasForeignKey(x => x.FlockId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LabTestResult>().ToTable("LabTestResults", "quality");
        modelBuilder.Entity<LabTestResult>()
            .HasOne(x => x.Sample).WithMany(x => x.Results).HasForeignKey(x => x.SampleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LabTestResult>()
            .HasOne(x => x.TestType).WithMany().HasForeignKey(x => x.TestTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LabTestResult>()
            .HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Operations
        modelBuilder.Entity<VehicleType>().ToTable("VehicleTypes", "operations")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Vehicle>().ToTable("Vehicles", "operations")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Vehicle>()
            .HasOne(x => x.VehicleType).WithMany().HasForeignKey(x => x.VehicleTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Driver>().ToTable("Drivers", "operations")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Driver>()
            .HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VehicleAssignment>().ToTable("VehicleAssignments", "operations");
        modelBuilder.Entity<VehicleAssignment>()
            .HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VehicleAssignment>()
            .HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GatePass>().ToTable("GatePasses", "operations")
            .HasIndex(x => new { x.TenantId, x.GatePassNo }).IsUnique();
        modelBuilder.Entity<GatePass>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GatePass>()
            .HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GatePass>()
            .HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GatePass>()
            .HasOne(x => x.Weighment).WithMany().HasForeignKey(x => x.WeighmentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GatePassLine>().ToTable("GatePassLines", "operations");
        modelBuilder.Entity<GatePassLine>()
            .HasOne(x => x.GatePass).WithMany(x => x.Lines).HasForeignKey(x => x.GatePassId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<GatePassLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        // Weighbridge
        modelBuilder.Entity<WeighbridgeScale>().ToTable("WeighbridgeScales", "weighbridge")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<Weighment>().ToTable("Weighments", "weighbridge")
            .HasIndex(x => new { x.TenantId, x.TicketNo }).IsUnique();
        modelBuilder.Entity<Weighment>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Weighment>()
            .HasOne(x => x.Scale).WithMany().HasForeignKey(x => x.ScaleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Weighment>()
            .HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Weighment>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Weighment>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Weighment>()
            .HasOne(x => x.GatePass).WithMany().HasForeignKey(x => x.GatePassId).OnDelete(DeleteBehavior.Restrict);

        // POS
        modelBuilder.Entity<PosTerminal>().ToTable("PosTerminals", "pos")
            .HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        modelBuilder.Entity<PosTerminal>()
            .HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosTerminal>()
            .HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosShift>().ToTable("PosShifts", "pos")
            .HasIndex(x => new { x.TenantId, x.ShiftNo }).IsUnique();
        modelBuilder.Entity<PosShift>()
            .HasOne(x => x.Terminal).WithMany().HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosShift>()
            .HasOne(x => x.OpenedByUser).WithMany().HasForeignKey(x => x.OpenedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosShift>()
            .HasOne(x => x.ClosedByUser).WithMany().HasForeignKey(x => x.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosSale>().ToTable("PosSales", "pos")
            .HasIndex(x => new { x.TenantId, x.SaleNo }).IsUnique();
        modelBuilder.Entity<PosSale>()
            .HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosSale>()
            .HasOne(x => x.Shift).WithMany(x => x.Sales).HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosSale>()
            .HasOne(x => x.Terminal).WithMany().HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosSale>()
            .HasOne(x => x.Party).WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosSale>()
            .HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosSale>()
            .HasOne(x => x.DeliveryOrder).WithMany().HasForeignKey(x => x.DeliveryOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosSale>()
            .HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosSaleLine>().ToTable("PosSaleLines", "pos");
        modelBuilder.Entity<PosSaleLine>()
            .HasOne(x => x.PosSale).WithMany(x => x.Lines).HasForeignKey(x => x.PosSaleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PosSaleLine>()
            .HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosPayment>().ToTable("PosPayments", "pos");
        modelBuilder.Entity<PosPayment>()
            .HasOne(x => x.PosSale).WithMany(x => x.Payments).HasForeignKey(x => x.PosSaleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PosPayment>()
            .HasOne(x => x.CashBankAccount).WithMany().HasForeignKey(x => x.CashBankAccountId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PosPayment>()
            .HasOne(x => x.CustomerPayment).WithMany().HasForeignKey(x => x.CustomerPaymentId).OnDelete(DeleteBehavior.Restrict);

        // Phase 10A / 10B / 10C / 10D AI
        modelBuilder.Entity<AiConversation>().ToTable("Conversations", "ai");
        modelBuilder.Entity<AiConversation>()
            .HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AiMessage>().ToTable("Messages", "ai");
        modelBuilder.Entity<AiMessage>()
            .HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AiPendingAction>().ToTable("PendingActions", "ai");
        modelBuilder.Entity<AiPendingAction>()
            .HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AiPendingAction>().HasIndex(x => new { x.TenantId, x.UserId, x.Status });
        modelBuilder.Entity<AiPendingAction>()
            .HasOne(x => x.AgentRun).WithMany().HasForeignKey(x => x.AgentRunId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AiPendingAction>()
            .HasOne(x => x.AgentStep).WithMany().HasForeignKey(x => x.AgentStepId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AiAgentRun>().ToTable("AgentRuns", "ai");
        modelBuilder.Entity<AiAgentRun>()
            .HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AiAgentRun>().HasIndex(x => new { x.TenantId, x.UserId, x.Status });
        modelBuilder.Entity<AiAgentStep>().ToTable("AgentSteps", "ai");
        modelBuilder.Entity<AiAgentStep>()
            .HasOne(x => x.AgentRun).WithMany(x => x.Steps).HasForeignKey(x => x.AgentRunId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AiAgentStep>().HasIndex(x => new { x.AgentRunId, x.StepNo });
        modelBuilder.Entity<AiPrediction>().ToTable("Predictions", "ai");
        modelBuilder.Entity<AiPrediction>().HasIndex(x => new { x.TenantId, x.UserId, x.CreatedAt });

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
