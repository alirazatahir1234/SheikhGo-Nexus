namespace SheikhGoNexusERP.Domain.Enums;

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public enum PartyType
{
    Customer = 1,
    Supplier = 2,
    Both = 3
}

public enum DocumentStatus
{
    Draft = 0,
    Submitted = 1,
    Verified = 2,
    Approved = 3,
    Posted = 4,
    Cancelled = 5,
    /// <summary>Payroll run calculated; submit only from this state for PR.</summary>
    Calculated = 6
}

public enum IndustryType
{
    FeedMill = 1,
    PoultryFarm = 2,
    FlourMill = 3,
    Distribution = 4,
    Packing = 5,
    Trading = 6,
    Other = 99
}

/// <summary>Legacy Softaxe voucher codes — reference only; Nexus uses DocumentType.Code.</summary>
public enum VoucherType
{
    JV = 1,
    CP = 2,
    CR = 3,
    BP = 4,
    BR = 5,
    CpFreight = 10,
    CpAd = 11,
    BpAd = 12,
    CpSl = 13,
    BpSl = 14,
    DoSales = 15,
    JvRm = 16,
    JvAdj = 17,
    StkTrf = 18,
    Sr = 19,
    Pr = 20,
    CpPaid = 21
}

public enum JournalSourceType
{
    Manual = 0,
    SalesInvoice = 1,
    CustomerPayment = 2,
    PurchaseInvoice = 3,
    SupplierPayment = 4,
    DeliveryCogs = 5,
    CashBank = 6,
    StockAdjustment = 7,
    PayrollRun = 8,
    SalaryPayment = 9,
    ProductionIssue = 10,
    ProductionReceipt = 11,
    FarmExpense = 12
}

public enum EmploymentType
{
    Permanent = 1,
    Contract = 2,
    Temporary = 3,
    Intern = 4
}

public enum EmploymentStatus
{
    Active = 1,
    Inactive = 2,
    Terminated = 3
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    HalfDay = 4,
    Overtime = 5
}

public enum PayrollComponentType
{
    Earning = 1,
    Deduction = 2
}

public enum PayrollPeriodStatus
{
    Open = 1,
    Closed = 2
}

public static class GlMappingKeys
{
    public const string PayrollExpense = "PayrollExpense";
    public const string SalaryPayable = "SalaryPayable";
    public const string TaxPayable = "TaxPayable";
    public const string OtherPayable = "OtherPayable";
    public const string Inventory = "Inventory";
    public const string WorkInProcess = "WorkInProcess";
    public const string FarmExpense = "FarmExpense";
    public const string SalesRevenue = "SalesRevenue";
    public const string Cogs = "Cogs";
}

public enum InventoryTxnSourceType
{
    GoodsReceipt = 1,
    Delivery = 2,
    TransferOut = 3,
    TransferIn = 4,
    Adjustment = 5,
    Opening = 6,
    StockCount = 7,
    ProductionConsumption = 8,
    ProductionOutput = 9,
    FarmFeedIssue = 10,
    FarmMedication = 11
}

public enum ProductionOrderStatus
{
    Draft = 0,
    Approved = 1,
    Released = 2,
    InProduction = 3,
    Completed = 4,
    Closed = 5,
    Cancelled = 6
}

public enum ProductionVarianceKind
{
    Wastage = 1,
    ByProduct = 2
}

public enum CashBankKind
{
    Cash = 1,
    Bank = 2
}

public enum CashBankTxnType
{
    Receipt = 1,
    Payment = 2,
    Transfer = 3
}

public enum StockQualityStatus
{
    Unrestricted = 0,
    Hold = 1,
    Rejected = 2
}

public enum FarmType
{
    Poultry = 1,
    Livestock = 2,
    Other = 9
}

public enum FlockStatus
{
    Active = 1,
    Closed = 2,
    Cancelled = 3
}

public enum FarmTreatmentKind
{
    Vaccination = 1,
    Medication = 2
}

public enum FarmExpenseCategory
{
    Feed = 1,
    Medicine = 2,
    Labor = 3,
    Utilities = 4,
    Other = 9
}

public enum LabSampleSourceType
{
    Purchase = 1,
    Production = 2,
    Inventory = 3,
    Farm = 4,
    Other = 9
}

public enum LabSampleStatus
{
    Draft = 0,
    Submitted = 1,
    InTest = 2,
    Completed = 3,
    Cancelled = 4
}

public enum LabDecision
{
    Pass = 1,
    Fail = 2,
    Hold = 3
}

public enum LabResultStatus
{
    Draft = 0,
    Approved = 1
}

public enum GatePassDirection
{
    Inward = 1,
    Outward = 2,
    Return = 3,
    Transfer = 4
}

public enum WeighmentStatus
{
    Draft = 0,
    FirstTaken = 1,
    Completed = 2,
    Cancelled = 3
}

public enum WeighmentDirection
{
    Inward = 1,
    Outward = 2
}

public enum PosShiftStatus
{
    Open = 1,
    Closed = 2
}

public enum PosTenderType
{
    Cash = 1,
    Card = 2,
    Other = 3
}
