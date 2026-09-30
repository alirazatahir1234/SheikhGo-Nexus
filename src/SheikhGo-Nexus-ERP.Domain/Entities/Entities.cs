using SoftaxeERP.Domain.Common;
using SoftaxeERP.Domain.Enums;

namespace SoftaxeERP.Domain.Entities;

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
    public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsRawMaterial { get; set; }
    public bool IsFinishedGood { get; set; } = true;
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
}

public class WarehouseStock : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public decimal Quantity { get; set; }
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
}

public class SupplierProfile : AuditableEntity
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
    public decimal OpeningBalance { get; set; }
    public int Level { get; set; } = 1;
}

public class PayableBalance : AuditableEntity
{
    public Guid PartyId { get; set; }
    public Party? Party { get; set; }
    public decimal Balance { get; set; }
}
