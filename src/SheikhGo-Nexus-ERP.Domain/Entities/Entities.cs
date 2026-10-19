using SheikhGoNexusERP.Domain.Common;
using SheikhGoNexusERP.Domain.Enums;

namespace SheikhGoNexusERP.Domain.Entities;

// ─── Core ───────────────────────────────────────────────────────────────────

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public IndustryType Industry { get; set; } = IndustryType.FeedMill;
    public string? EnabledModulesJson { get; set; }
    public bool IsActive { get; set; } = true;
    public Company? Company { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Company : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string Country { get; set; } = "Pakistan";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Currency { get; set; } = "PKR";
    public string CurrencySymbol { get; set; } = "Rs";
    public string? Ntn { get; set; }
    public string? Strn { get; set; }
    public string? OwnerName { get; set; }
    public IndustryType Industry { get; set; } = IndustryType.FeedMill;
    public bool SaleModule { get; set; } = true;
    public bool PurchaseModule { get; set; } = true;
    public bool InventoryModule { get; set; } = true;
    public bool AccountsModule { get; set; } = true;
    public decimal DefaultSaleTaxPercent { get; set; }
    public decimal DefaultPurchaseTaxPercent { get; set; }
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}

public class Branch : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}

public class FinancialYear : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsClosed { get; set; }
    public ICollection<FiscalPeriod> Periods { get; set; } = new List<FiscalPeriod>();
}

public class FiscalPeriod : AuditableEntity
{
    public Guid FinancialYearId { get; set; }
    public FinancialYear? FinancialYear { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PeriodNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}

// ─── Identity ───────────────────────────────────────────────────────────────

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsPlatformAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class Role : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Module { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
}

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}

// ─── Inventory ──────────────────────────────────────────────────────────────

public class ProductGroup : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class ProductCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Brand : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class UnitOfMeasure : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Primary barcode for POS scan (HID scanners).</summary>
    public string? Barcode { get; set; }
    public Guid? ProductGroupId { get; set; }
    public ProductGroup? ProductGroup { get; set; }
    public Guid? CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
    /// <summary>Weighted-average unit cost for perpetual inventory / COGS.</summary>
    public decimal AverageCost { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRawMaterial { get; set; }
    public bool IsFinishedGood { get; set; } = true;
    /// <summary>When true, POS expects fractional qty (kg/g); rate is per base UOM.</summary>
    public bool SellByWeight { get; set; }
    public ICollection<WarehouseStock> Stocks { get; set; } = new List<WarehouseStock>();
}

public class Warehouse : AuditableEntity
{
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsDefault { get; set; }
    public ICollection<WarehouseStock> Stocks { get; set; } = new List<WarehouseStock>();
    public ICollection<WarehouseLocation> Locations { get; set; } = new List<WarehouseLocation>();
}

public class WarehouseLocation : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class WarehouseStock : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public decimal Quantity { get; set; }
    public StockQualityStatus QualityStatus { get; set; } = StockQualityStatus.Unrestricted;
}

public class UnitConversion : AuditableEntity
{
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid FromUomId { get; set; }
    public UnitOfMeasure? FromUom { get; set; }
    public Guid ToUomId { get; set; }
    public UnitOfMeasure? ToUom { get; set; }
    /// <summary>ToQty = FromQty × Factor (e.g. 1 Bag → 25 KG ⇒ Factor = 25).</summary>
    public decimal Factor { get; set; }
    public bool IsActive { get; set; } = true;
}

public class InventoryTransaction : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? WarehouseLocationId { get; set; }
    public WarehouseLocation? WarehouseLocation { get; set; }
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public InventoryTxnSourceType SourceType { get; set; }
    public Guid SourceEntityId { get; set; }
    public Guid? SourceLineId { get; set; }
    public DateTime TxnDate { get; set; } = DateTime.UtcNow.Date;
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Narration { get; set; }
}

public class StockTransfer : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string TransferNo { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; } = DateTime.UtcNow.Date;
    public Guid FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    public Guid ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Remarks { get; set; }
    public ICollection<StockTransferLine> Lines { get; set; } = new List<StockTransferLine>();
}

public class StockTransferLine : BaseEntity
{
    public Guid StockTransferId { get; set; }
    public StockTransfer? StockTransfer { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public Guid? FromLocationId { get; set; }
    public WarehouseLocation? FromLocation { get; set; }
    public Guid? ToLocationId { get; set; }
    public WarehouseLocation? ToLocation { get; set; }
}

public class StockAdjustment : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string AdjustmentNo { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow.Date;
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public Guid? StockCountId { get; set; }
    public StockCount? StockCount { get; set; }
    public ICollection<StockAdjustmentLine> Lines { get; set; } = new List<StockAdjustmentLine>();
}

public class StockAdjustmentLine : BaseEntity
{
    public Guid StockAdjustmentId { get; set; }
    public StockAdjustment? StockAdjustment { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal QtyDelta { get; set; }
    public Guid? WarehouseLocationId { get; set; }
    public WarehouseLocation? WarehouseLocation { get; set; }
    public string? LineReason { get; set; }
}

public class StockCount : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string CountNo { get; set; } = string.Empty;
    public DateTime CountDate { get; set; } = DateTime.UtcNow.Date;
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public DateTime? SnapshotAt { get; set; }
    public string? Remarks { get; set; }
    public Guid? LinkedAdjustmentId { get; set; }
    public ICollection<StockCountLine> Lines { get; set; } = new List<StockCountLine>();
}

public class StockCountLine : BaseEntity
{
    public Guid StockCountId { get; set; }
    public StockCount? StockCount { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal SystemQty { get; set; }
    public decimal CountedQty { get; set; }
    public decimal Variance { get; set; }
    public Guid? WarehouseLocationId { get; set; }
    public WarehouseLocation? WarehouseLocation { get; set; }
}

// ─── Documents ──────────────────────────────────────────────────────────────

public class DocumentType : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool RequiresVerification { get; set; }
    public bool RequiresApproval { get; set; }
    public string? Prefix { get; set; }
    public bool AffectsInventory { get; set; }
    public bool AffectsGl { get; set; }
}

public class Document : AuditableEntity
{
    public Guid DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid? FinancialYearId { get; set; }
    public FinancialYear? FinancialYear { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow.Date;
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Narration { get; set; }
    public Guid? PartyId { get; set; }
    public Party? Party { get; set; }
    public ICollection<DocumentLine> Lines { get; set; } = new List<DocumentLine>();
    public ICollection<Verification> Verifications { get; set; } = new List<Verification>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}

public class DocumentLine : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public int LineNo { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Remarks { get; set; }
}

public class DocumentNumberSequence : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public Guid FinancialYearId { get; set; }
    public FinancialYear? FinancialYear { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public int CurrentNumber { get; set; }
    public int Padding { get; set; } = 6;
}

public class Verification : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Remarks { get; set; }
    public bool IsVerified { get; set; }
}

public class Approval : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Remarks { get; set; }
    public bool IsApproved { get; set; }
}

// ─── Procurement ────────────────────────────────────────────────────────────

public class Party : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PartyType PartyType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? TaxNo { get; set; }
    public bool IsActive { get; set; } = true;
    public SupplierProfile? SupplierProfile { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }
}

public class SupplierProfile : AuditableEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public string? PaymentTerms { get; set; }
    public decimal CreditLimit { get; set; }
    public int PaymentDays { get; set; }
}

public class CustomerProfile : AuditableEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public string? PaymentTerms { get; set; }
    public decimal CreditLimit { get; set; }
    public int PaymentDays { get; set; }
}

public class PurchaseOrder : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow.Date;
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public string? Remarks { get; set; }
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
}

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal ReceivedQty { get; set; }
}

public class GoodsReceipt : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow.Date;
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Remarks { get; set; }
    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}

public class GoodsReceiptLine : BaseEntity
{
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class PurchaseInvoice : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Remarks { get; set; }
    public ICollection<PurchaseInvoiceLine> Lines { get; set; } = new List<PurchaseInvoiceLine>();
}

public class PurchaseInvoiceLine : BaseEntity
{
    public Guid PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

// ─── Finance stub ───────────────────────────────────────────────────────────

public class Account : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public Guid? ParentId { get; set; }
    public Account? Parent { get; set; }
    public ICollection<Account> Children { get; set; } = new List<Account>();
    public bool IsPostable { get; set; } = true;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal OpeningBalance { get; set; }
    public int Level { get; set; } = 1;
}

public class PayableBalance : AuditableEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public decimal Balance { get; set; }
}

public class ReceivableBalance : AuditableEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public decimal Balance { get; set; }
}

public class JournalEntry : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid FinancialYearId { get; set; }
    public FinancialYear? FinancialYear { get; set; }
    public Guid? FiscalPeriodId { get; set; }
    public FiscalPeriod? FiscalPeriod { get; set; }
    public string JournalNo { get; set; } = string.Empty;
    public DateTime PostingDate { get; set; } = DateTime.UtcNow.Date;
    public JournalSourceType SourceType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Narration { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}

public class JournalEntryLine : BaseEntity
{
    public Guid JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public int LineNo { get; set; }
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
    public Guid? PartyId { get; set; }
    public Party? Party { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
}

public class CashBankAccount : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CashBankKind AccountKind { get; set; }
    public Guid GlAccountId { get; set; }
    public Account? GlAccount { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
}

public class CashBankTransaction : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid CashBankAccountId { get; set; }
    public CashBankAccount? CashBankAccount { get; set; }
    public Guid? OppositeCashBankAccountId { get; set; }
    public CashBankAccount? OppositeCashBankAccount { get; set; }
    public Guid? PartyId { get; set; }
    public Party? Party { get; set; }
    public decimal Amount { get; set; }
    public CashBankTxnType TxnType { get; set; }
    public DateTime PostingDate { get; set; } = DateTime.UtcNow.Date;
    public Guid? JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Narration { get; set; }
}

public class SupplierPayment : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string PaymentNo { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public Guid? CashBankAccountId { get; set; }
    public CashBankAccount? CashBankAccount { get; set; }
    public decimal Amount { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Remarks { get; set; }
    public ICollection<SupplierPaymentApplication> Applications { get; set; } = new List<SupplierPaymentApplication>();
}

public class SupplierPaymentApplication : BaseEntity
{
    public Guid SupplierPaymentId { get; set; }
    public SupplierPayment? SupplierPayment { get; set; }
    public Guid PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public decimal AmountApplied { get; set; }
}

// ─── Sales ──────────────────────────────────────────────────────────────────

public class SalesOrder : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow.Date;
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public string? Remarks { get; set; }
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
    public ICollection<DeliveryOrder> DeliveryOrders { get; set; } = new List<DeliveryOrder>();
}

public class SalesOrderLine : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal DeliveredQty { get; set; }
}

public class DeliveryOrder : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public string DeliveryNo { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; } = DateTime.UtcNow.Date;
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Remarks { get; set; }
    public ICollection<DeliveryOrderLine> Lines { get; set; } = new List<DeliveryOrderLine>();
}

public class DeliveryOrderLine : BaseEntity
{
    public Guid DeliveryOrderId { get; set; }
    public DeliveryOrder? DeliveryOrder { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? SalesOrderLineId { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class SalesInvoice : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid? DeliveryOrderId { get; set; }
    public DeliveryOrder? DeliveryOrder { get; set; }
    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Remarks { get; set; }
    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();
}

public class SalesInvoiceLine : BaseEntity
{
    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class CustomerPayment : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string PaymentNo { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public Guid? CashBankAccountId { get; set; }
    public CashBankAccount? CashBankAccount { get; set; }
    public decimal Amount { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Remarks { get; set; }
    public ICollection<CustomerPaymentApplication> Applications { get; set; } = new List<CustomerPaymentApplication>();
}

public class CustomerPaymentApplication : BaseEntity
{
    public Guid CustomerPaymentId { get; set; }
    public CustomerPayment? CustomerPayment { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
    public decimal AmountApplied { get; set; }
}

public class GlAccountMapping : AuditableEntity
{
    public string MappingKey { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public bool IsActive { get; set; } = true;
}

// ─── HR & Payroll ───────────────────────────────────────────────────────────

public class Department : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Designation : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Employee : AuditableEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? DesignationId { get; set; }
    public Designation? Designation { get; set; }
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public DateTime JoiningDate { get; set; } = DateTime.UtcNow.Date;
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;
    public decimal BasicSalary { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public PayrollProfile? PayrollProfile { get; set; }
}

public class PayrollComponent : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PayrollComponentType ComponentType { get; set; }
    public bool IsSystem { get; set; }
    public bool AffectsGross { get; set; } = true;
    public string? GlMappingKey { get; set; }
}

public class PayrollProfile : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<PayrollProfileLine> Lines { get; set; } = new List<PayrollProfileLine>();
}

public class PayrollProfileLine : BaseEntity
{
    public Guid PayrollProfileId { get; set; }
    public PayrollProfile? PayrollProfile { get; set; }
    public Guid PayrollComponentId { get; set; }
    public PayrollComponent? PayrollComponent { get; set; }
    public decimal Amount { get; set; }
}

public class Attendance : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public decimal? OvertimeHours { get; set; }
    public string? Remarks { get; set; }
}

public class LeaveType : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Paid { get; set; } = true;
    public decimal AnnualAllowanceDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LeaveBalance : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public int Year { get; set; }
    public decimal Entitled { get; set; }
    public decimal Used { get; set; }
    public decimal Remaining { get; set; }
}

public class LeaveRequest : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string RequestNo { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal Days { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Remarks { get; set; }
}

public class PayrollPeriod : AuditableEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Open;
}

public class PayrollRun : AuditableEntity
{
    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string RunNo { get; set; } = string.Empty;
    public DateTime RunDate { get; set; } = DateTime.UtcNow.Date;
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal GrossTotal { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal NetTotal { get; set; }
    public string? Remarks { get; set; }
    public ICollection<PayrollEmployee> Employees { get; set; } = new List<PayrollEmployee>();
    public ICollection<SalaryPayment> Payments { get; set; } = new List<SalaryPayment>();
}

public class PayrollEmployee : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal Basic { get; set; }
    public decimal Gross { get; set; }
    public decimal Deductions { get; set; }
    public decimal Net { get; set; }
    public ICollection<PayrollLine> Lines { get; set; } = new List<PayrollLine>();
}

public class PayrollLine : BaseEntity
{
    public Guid PayrollEmployeeId { get; set; }
    public PayrollEmployee? PayrollEmployee { get; set; }
    public Guid PayrollComponentId { get; set; }
    public PayrollComponent? PayrollComponent { get; set; }
    public PayrollComponentType ComponentType { get; set; }
    public decimal Amount { get; set; }
}

public class SalaryPayment : AuditableEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string PaymentNo { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;
    public Guid CashBankAccountId { get; set; }
    public CashBankAccount? CashBankAccount { get; set; }
    public decimal Amount { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Remarks { get; set; }
}

// ─── Production ─────────────────────────────────────────────────────────────

public class BillOfMaterial : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid FinishedProductId { get; set; }
    public Product? FinishedProduct { get; set; }
    public int Version { get; set; } = 1;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EffectiveTo { get; set; }
    public decimal ScrapPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<BillOfMaterialLine> Lines { get; set; } = new List<BillOfMaterialLine>();
}

public class BillOfMaterialLine : BaseEntity
{
    public Guid BillOfMaterialId { get; set; }
    public BillOfMaterial? BillOfMaterial { get; set; }
    public int LineNo { get; set; }
    public Guid ComponentProductId { get; set; }
    public Product? ComponentProduct { get; set; }
    public decimal QtyPer { get; set; }
    public Guid? UomId { get; set; }
    public UnitOfMeasure? Uom { get; set; }
    public decimal ScrapPercent { get; set; }
}

public class ProductionOrder : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow.Date;
    public Guid BomId { get; set; }
    public BillOfMaterial? Bom { get; set; }
    public Guid FinishedProductId { get; set; }
    public Product? FinishedProduct { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public decimal QtyOrdered { get; set; }
    public decimal QtyCompleted { get; set; }
    public ProductionOrderStatus Status { get; set; } = ProductionOrderStatus.Draft;
    public DocumentStatus DocumentStatus { get; set; } = DocumentStatus.Draft;
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal UnitCost { get; set; }
    public string? Remarks { get; set; }
    public ICollection<ProductionOrderComponent> Components { get; set; } = new List<ProductionOrderComponent>();
    public ICollection<ProductionOrderOutput> Outputs { get; set; } = new List<ProductionOrderOutput>();
    public ICollection<ProductionVarianceLine> Variances { get; set; } = new List<ProductionVarianceLine>();
}

public class ProductionOrderComponent : BaseEntity
{
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal ActualQty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class ProductionOrderOutput : BaseEntity
{
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsByProduct { get; set; }
}

public class ProductionVarianceLine : BaseEntity
{
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }
    public ProductionVarianceKind Kind { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Narration { get; set; }
}

// ─── Farm ───────────────────────────────────────────────────────────────────

public class Farm : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FarmType FarmType { get; set; } = FarmType.Poultry;
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<FarmUnit> Units { get; set; } = new List<FarmUnit>();
    public ICollection<Flock> Flocks { get; set; } = new List<Flock>();
}

public class FarmUnit : AuditableEntity
{
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Flock : AuditableEntity
{
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public Guid? FarmUnitId { get; set; }
    public FarmUnit? FarmUnit { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public string BirdType { get; set; } = string.Empty;
    public DateTime PlacementDate { get; set; } = DateTime.UtcNow.Date;
    public int QtyPlaced { get; set; }
    public int QtyCurrent { get; set; }
    public int QtyMortality { get; set; }
    public FlockStatus Status { get; set; } = FlockStatus.Active;
    public string? Remarks { get; set; }
    public ICollection<MortalityRecord> Mortalities { get; set; } = new List<MortalityRecord>();
    public ICollection<FarmTreatment> Treatments { get; set; } = new List<FarmTreatment>();
    public ICollection<FeedIssue> FeedIssues { get; set; } = new List<FeedIssue>();
}

public class MortalityRecord : AuditableEntity
{
    public Guid FlockId { get; set; }
    public Flock? Flock { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public int Qty { get; set; }
    public string? Reason { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
}

public class FarmTreatment : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string TreatmentNo { get; set; } = string.Empty;
    public Guid FlockId { get; set; }
    public Flock? Flock { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public FarmTreatmentKind Kind { get; set; } = FarmTreatmentKind.Vaccination;
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public decimal Qty { get; set; }
    public string? BatchNo { get; set; }
    public string? Narration { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
}

public class FeedIssue : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string IssueNo { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public Guid FlockId { get; set; }
    public Flock? Flock { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Remarks { get; set; }
    public ICollection<FeedIssueLine> Lines { get; set; } = new List<FeedIssueLine>();
}

public class FeedIssueLine : BaseEntity
{
    public Guid FeedIssueId { get; set; }
    public FeedIssue? FeedIssue { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class FarmExpense : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string ExpenseNo { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow.Date;
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public Guid? FlockId { get; set; }
    public Flock? Flock { get; set; }
    public FarmExpenseCategory Category { get; set; } = FarmExpenseCategory.Other;
    public decimal Amount { get; set; }
    public Guid? CashBankAccountId { get; set; }
    public CashBankAccount? CashBankAccount { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Narration { get; set; }
}

// ─── Quality / Lab ──────────────────────────────────────────────────────────

public class LabTestType : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LabSample : AuditableEntity
{
    public string SampleNo { get; set; } = string.Empty;
    public DateTime SampleDate { get; set; } = DateTime.UtcNow.Date;
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public LabSampleSourceType SourceType { get; set; } = LabSampleSourceType.Inventory;
    public Guid? SourceEntityId { get; set; }
    public Guid? FlockId { get; set; }
    public Flock? Flock { get; set; }
    public LabSampleStatus Status { get; set; } = LabSampleStatus.Draft;
    public string? Remarks { get; set; }
    public ICollection<LabTestResult> Results { get; set; } = new List<LabTestResult>();
}

public class LabTestResult : AuditableEntity
{
    public Guid SampleId { get; set; }
    public LabSample? Sample { get; set; }
    public Guid TestTypeId { get; set; }
    public LabTestType? TestType { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
    public decimal Value { get; set; }
    public LabDecision Decision { get; set; } = LabDecision.Pass;
    public LabResultStatus Status { get; set; } = LabResultStatus.Draft;
    public Guid? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Narration { get; set; }
}

// ─── Operations ─────────────────────────────────────────────────────────────

public class VehicleType : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Vehicle : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid VehicleTypeId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Driver : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string? LicenseNo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class VehicleAssignment : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public DateTime FromDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ToDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class GatePass : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string GatePassNo { get; set; } = string.Empty;
    public GatePassDirection Direction { get; set; } = GatePassDirection.Outward;
    public DateTime PassDate { get; set; } = DateTime.UtcNow.Date;
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public string? SourceDocType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public Guid? WeighmentId { get; set; }
    public Weighment? Weighment { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? Remarks { get; set; }
    public ICollection<GatePassLine> Lines { get; set; } = new List<GatePassLine>();
}

public class GatePassLine : BaseEntity
{
    public Guid GatePassId { get; set; }
    public GatePass? GatePass { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public string? Narration { get; set; }
}

// ─── Weighbridge ────────────────────────────────────────────────────────────

public class WeighbridgeScale : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Weighment : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public Guid? ScaleId { get; set; }
    public WeighbridgeScale? Scale { get; set; }
    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public string VehicleNo { get; set; } = string.Empty;
    public Guid? PartyId { get; set; }
    public Party? Party { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public WeighmentDirection Direction { get; set; } = WeighmentDirection.Inward;
    public decimal FirstWeight { get; set; }
    public DateTime? FirstWeightAt { get; set; }
    public decimal? SecondWeight { get; set; }
    public DateTime? SecondWeightAt { get; set; }
    public decimal? NetWeight { get; set; }
    public WeighmentStatus Status { get; set; } = WeighmentStatus.Draft;
    public string? LinkedDocumentType { get; set; }
    public Guid? LinkedDocumentId { get; set; }
    public Guid? GatePassId { get; set; }
    public GatePass? GatePass { get; set; }
    public string? Remarks { get; set; }
}

// ─── POS ────────────────────────────────────────────────────────────────────

public class PosTerminal : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PosShift : AuditableEntity
{
    public Guid TerminalId { get; set; }
    public PosTerminal? Terminal { get; set; }
    public string ShiftNo { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public Guid OpenedByUserId { get; set; }
    public User? OpenedByUser { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public User? ClosedByUser { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? Variance { get; set; }
    public PosShiftStatus Status { get; set; } = PosShiftStatus.Open;
    public ICollection<PosSale> Sales { get; set; } = new List<PosSale>();
}

public class PosSale : AuditableEntity
{
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public string SaleNo { get; set; } = string.Empty;
    public Guid ShiftId { get; set; }
    public PosShift? Shift { get; set; }
    public Guid TerminalId { get; set; }
    public PosTerminal? Terminal { get; set; }
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow.Date;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal GrandTotal { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    /// <summary>Client-generated key to prevent duplicate POS posts.</summary>
    public string? IdempotencyKey { get; set; }
    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public Guid? DeliveryOrderId { get; set; }
    public DeliveryOrder? DeliveryOrder { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
    public Guid? SaleReturnId { get; set; }
    public string? Remarks { get; set; }
    public ICollection<PosSaleLine> Lines { get; set; } = new List<PosSaleLine>();
    public ICollection<PosPayment> Payments { get; set; } = new List<PosPayment>();
}

public class PosSaleLine : BaseEntity
{
    public Guid PosSaleId { get; set; }
    public PosSale? PosSale { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class PosPayment : BaseEntity
{
    public Guid PosSaleId { get; set; }
    public PosSale? PosSale { get; set; }
    public PosTenderType Tender { get; set; } = PosTenderType.Cash;
    public decimal Amount { get; set; }
    public Guid? CashBankAccountId { get; set; }
    public CashBankAccount? CashBankAccount { get; set; }
    public Guid? CustomerPaymentId { get; set; }
    public CustomerPayment? CustomerPayment { get; set; }
}

// ─── Phase 10A AI ───────────────────────────────────────────────────────────

public class AiConversation : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Title { get; set; } = "New chat";
    public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}

public class AiMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public AiConversation? Conversation { get; set; }
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? ToolsJson { get; set; }
}

// ─── Phase 10B AI Actions ───────────────────────────────────────────────────

public class AiPendingAction : AuditableEntity
{
    public Guid ConversationId { get; set; }
    public AiConversation? Conversation { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid FinancialYearId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string PermissionRequired { get; set; } = string.Empty;
    public string PreviewJson { get; set; } = "{}";
    public string PayloadJson { get; set; } = "{}";
    public Guid? ResultDocumentId { get; set; }
    public string? ResultDocumentNo { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid? AgentRunId { get; set; }
    public AiAgentRun? AgentRun { get; set; }
    public Guid? AgentStepId { get; set; }
    public AiAgentStep? AgentStep { get; set; }
}

// ─── Phase 10C AI Agents ────────────────────────────────────────────────────

public class AiAgentRun : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid FinancialYearId { get; set; }
    public Guid? ConversationId { get; set; }
    public AiConversation? Conversation { get; set; }
    public string AgentType { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string Status { get; set; } = "Planning";
    public string PlanJson { get; set; } = "{}";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public ICollection<AiAgentStep> Steps { get; set; } = new List<AiAgentStep>();
}

public class AiAgentStep : BaseEntity
{
    public Guid AgentRunId { get; set; }
    public AiAgentRun? AgentRun { get; set; }
    public int StepNo { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public string Status { get; set; } = "Pending";
    public bool RequiresConfirmation { get; set; }
    public Guid? LinkedPendingActionId { get; set; }
    public DateTime? ExecutedAt { get; set; }
}

// ─── Phase 10D Predictive ───────────────────────────────────────────────────

public class AiPrediction : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string PredictionType { get; set; } = "InventoryDemandForecast";
    public Guid? ProductId { get; set; }
    public int HorizonDays { get; set; }
    public string Confidence { get; set; } = "InsufficientData";
    public string ResultJson { get; set; } = "{}";
}
