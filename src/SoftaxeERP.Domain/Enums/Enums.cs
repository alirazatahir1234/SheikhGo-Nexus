namespace SoftaxeERP.Domain.Enums;

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
    Cancelled = 5
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
