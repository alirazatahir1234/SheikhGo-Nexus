using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Services;

namespace SheikhGoNexusERP.Infrastructure.Persistence;

/// <summary>Minimal Phase 0/1 demo data for SheikhGo_Nexus_Dev.</summary>
public static class DbSeeder
{
    public static readonly (string Code, string Name, string Module)[] PermissionCatalog =
    [
        ("Inventory.Product.View", "View products", "Inventory"),
        ("Inventory.Product.Create", "Create products", "Inventory"),
        ("Inventory.Product.Edit", "Edit products", "Inventory"),
        ("Inventory.Warehouse.View", "View warehouses", "Inventory"),
        ("Inventory.Warehouse.Create", "Create warehouses", "Inventory"),
        ("Inventory.Transfer.View", "View stock transfers", "Inventory"),
        ("Inventory.Transfer.Create", "Create stock transfers", "Inventory"),
        ("Inventory.Transfer.Approve", "Approve stock transfers", "Inventory"),
        ("Inventory.Transfer.Post", "Post stock transfers", "Inventory"),
        ("Inventory.Adjustment.View", "View stock adjustments", "Inventory"),
        ("Inventory.Adjustment.Create", "Create stock adjustments", "Inventory"),
        ("Inventory.Adjustment.Approve", "Approve stock adjustments", "Inventory"),
        ("Inventory.Adjustment.Post", "Post stock adjustments", "Inventory"),
        ("Inventory.StockCount.View", "View stock counts", "Inventory"),
        ("Inventory.StockCount.Create", "Create stock counts", "Inventory"),
        ("Inventory.StockCount.Approve", "Approve stock counts", "Inventory"),
        ("Inventory.StockCount.Post", "Post stock counts", "Inventory"),
        ("Inventory.Reports.View", "View inventory reports", "Inventory"),

        ("Procurement.PurchaseOrder.View", "View purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Create", "Create purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Edit", "Edit purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Submit", "Submit purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Verify", "Verify purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Approve", "Approve purchase orders", "Procurement"),
        ("Procurement.PurchaseOrder.Post", "Post purchase orders", "Procurement"),

        ("Procurement.GoodsReceipt.View", "View goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Create", "Create goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Edit", "Edit goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Submit", "Submit goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Verify", "Verify goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Approve", "Approve goods receipts", "Procurement"),
        ("Procurement.GoodsReceipt.Post", "Post goods receipts", "Procurement"),

        ("Procurement.PurchaseInvoice.View", "View purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Create", "Create purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Edit", "Edit purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Submit", "Submit purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Verify", "Verify purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Approve", "Approve purchase invoices", "Procurement"),
        ("Procurement.PurchaseInvoice.Post", "Post purchase invoices", "Procurement"),

        ("Sales.Customer.View", "View customers", "Sales"),
        ("Sales.Customer.Create", "Create customers", "Sales"),
        ("Sales.Order.View", "View sales orders", "Sales"),
        ("Sales.Order.Create", "Create sales orders", "Sales"),
        ("Sales.Order.Edit", "Edit sales orders", "Sales"),
        ("Sales.Order.Submit", "Submit sales orders", "Sales"),
        ("Sales.Order.Verify", "Verify sales orders", "Sales"),
        ("Sales.Order.Approve", "Approve sales orders", "Sales"),
        ("Sales.Order.Post", "Post sales orders", "Sales"),
        ("Sales.Delivery.View", "View delivery orders", "Sales"),
        ("Sales.Delivery.Create", "Create delivery orders", "Sales"),
        ("Sales.Delivery.Edit", "Edit delivery orders", "Sales"),
        ("Sales.Delivery.Submit", "Submit delivery orders", "Sales"),
        ("Sales.Delivery.Verify", "Verify delivery orders", "Sales"),
        ("Sales.Delivery.Approve", "Approve delivery orders", "Sales"),
        ("Sales.Delivery.Post", "Post delivery orders", "Sales"),
        ("Sales.Invoice.View", "View sales invoices", "Sales"),
        ("Sales.Invoice.Create", "Create sales invoices", "Sales"),
        ("Sales.Invoice.Edit", "Edit sales invoices", "Sales"),
        ("Sales.Invoice.Submit", "Submit sales invoices", "Sales"),
        ("Sales.Invoice.Verify", "Verify sales invoices", "Sales"),
        ("Sales.Invoice.Approve", "Approve sales invoices", "Sales"),
        ("Sales.Invoice.Post", "Post sales invoices", "Sales"),
        ("Sales.Payment.View", "View customer payments", "Sales"),
        ("Sales.Payment.Create", "Create customer payments", "Sales"),
        ("Sales.Payment.Edit", "Edit customer payments", "Sales"),
        ("Sales.Payment.Submit", "Submit customer payments", "Sales"),
        ("Sales.Payment.Post", "Post customer payments", "Sales"),

        ("Finance.Account.View", "View chart of accounts", "Finance"),
        ("Finance.Account.Create", "Create accounts", "Finance"),
        ("Finance.Account.Edit", "Edit accounts", "Finance"),
        ("Finance.Journal.View", "View journal entries", "Finance"),
        ("Finance.Journal.Create", "Create journal entries", "Finance"),
        ("Finance.Journal.Post", "Post journal entries", "Finance"),
        ("Finance.Period.View", "View financial periods", "Finance"),
        ("Finance.Period.Close", "Close financial periods", "Finance"),
        ("Finance.Ledger.View", "View general ledger", "Finance"),
        ("Finance.Reports.View", "View finance reports", "Finance"),
        ("Finance.CashBank.View", "View cash/bank accounts", "Finance"),
        ("Finance.CashBank.Create", "Create cash/bank accounts", "Finance"),
        ("Finance.CashBank.Post", "Post cash/bank transactions", "Finance"),
        ("Finance.SupplierPayment.View", "View supplier payments", "Finance"),
        ("Finance.SupplierPayment.Create", "Create supplier payments", "Finance"),
        ("Finance.SupplierPayment.Post", "Post supplier payments", "Finance"),

        ("HR.Employee.View", "View employees", "HR"),
        ("HR.Employee.Create", "Create employees", "HR"),
        ("HR.Attendance.View", "View attendance", "HR"),
        ("HR.Attendance.Create", "Create attendance", "HR"),
        ("HR.Leave.View", "View leave", "HR"),
        ("HR.Leave.Create", "Create leave requests", "HR"),
        ("HR.Leave.Approve", "Approve leave requests", "HR"),
        ("HR.Payroll.View", "View payroll", "HR"),
        ("HR.Payroll.Create", "Create payroll", "HR"),
        ("HR.Payroll.Calculate", "Calculate payroll", "HR"),
        ("HR.Payroll.Approve", "Approve payroll", "HR"),
        ("HR.Payroll.Post", "Post payroll", "HR"),

        ("Production.BOM.View", "View BOMs", "Production"),
        ("Production.BOM.Create", "Create BOMs", "Production"),
        ("Production.BOM.Approve", "Approve BOMs", "Production"),
        ("Production.Order.View", "View production orders", "Production"),
        ("Production.Order.Create", "Create production orders", "Production"),
        ("Production.Order.Approve", "Approve production orders", "Production"),
        ("Production.Order.Release", "Release production orders", "Production"),
        ("Production.Order.Start", "Start production orders", "Production"),
        ("Production.Order.Complete", "Complete production orders", "Production"),
        ("Production.Order.Close", "Close production orders", "Production"),
        ("Production.Consumption.View", "View production consumption", "Production"),
        ("Production.Consumption.Create", "Create production consumption", "Production"),
        ("Production.Consumption.Post", "Post production consumption", "Production"),
        ("Production.Output.View", "View production output", "Production"),
        ("Production.Output.Create", "Create production output", "Production"),
        ("Production.Output.Post", "Post production output", "Production"),
        ("Production.Reports.View", "View production reports", "Production"),

        ("Farm.View", "View farms", "Farm"),
        ("Farm.Create", "Create farms", "Farm"),
        ("Farm.Edit", "Edit farms", "Farm"),
        ("Flock.View", "View flocks", "Farm"),
        ("Flock.Create", "Create flocks", "Farm"),
        ("Flock.Edit", "Edit flocks", "Farm"),
        ("Farm.Feed.View", "View feed issues", "Farm"),
        ("Farm.Feed.Issue", "Issue feed", "Farm"),
        ("Farm.Mortality.View", "View mortality", "Farm"),
        ("Farm.Mortality.Create", "Record mortality", "Farm"),
        ("Farm.Treatment.View", "View treatments", "Farm"),
        ("Farm.Treatment.Create", "Create treatments", "Farm"),
        ("Farm.Expense.View", "View farm expenses", "Farm"),
        ("Farm.Expense.Create", "Create farm expenses", "Farm"),

        ("Lab.TestType.View", "View lab test types", "Lab"),
        ("Lab.TestType.Create", "Create lab test types", "Lab"),
        ("Lab.Sample.View", "View lab samples", "Lab"),
        ("Lab.Sample.Create", "Create lab samples", "Lab"),
        ("Lab.Result.View", "View lab results", "Lab"),
        ("Lab.Result.Create", "Create lab results", "Lab"),
        ("Lab.Result.Approve", "Approve lab results", "Lab"),
        ("Lab.Quality.Release", "Release quality hold", "Lab"),
        ("Lab.Quality.Hold", "Hold quality stock", "Lab"),

        ("Operations.Vehicle.View", "View vehicles", "Operations"),
        ("Operations.Vehicle.Create", "Create vehicles", "Operations"),
        ("Operations.Driver.View", "View drivers", "Operations"),
        ("Operations.Driver.Create", "Create drivers", "Operations"),
        ("Operations.GatePass.View", "View gate passes", "Operations"),
        ("Operations.GatePass.Create", "Create gate passes", "Operations"),
        ("Operations.GatePass.Approve", "Approve gate passes", "Operations"),
        ("Operations.GatePass.Post", "Post gate passes", "Operations"),

        ("Weighbridge.Scale.View", "View weighbridge scales", "Weighbridge"),
        ("Weighbridge.Ticket.View", "View weighments", "Weighbridge"),
        ("Weighbridge.Ticket.Create", "Create weighments", "Weighbridge"),
        ("Weighbridge.Ticket.Weigh", "Record weighment weights", "Weighbridge"),
        ("Weighbridge.Ticket.Complete", "Complete weighments", "Weighbridge"),

        ("POS.Terminal.View", "View POS terminals", "POS"),
        ("POS.Terminal.Create", "Create POS terminals", "POS"),
        ("POS.Shift.View", "View POS shifts", "POS"),
        ("POS.Shift.Open", "Open POS shifts", "POS"),
        ("POS.Shift.Close", "Close POS shifts", "POS"),
        ("POS.Sale.View", "View POS sales", "POS"),
        ("POS.Sale.Create", "Create POS sales", "POS"),

        ("Reports.Executive.View", "View executive dashboard", "Reports"),
        ("Reports.Finance.View", "View finance reports", "Reports"),
        ("Reports.Sales.View", "View sales reports", "Reports"),
        ("Reports.Procurement.View", "View procurement reports", "Reports"),
        ("Reports.Inventory.View", "View inventory reports", "Reports"),
        ("Reports.Predictive.View", "View predictive inventory forecasts", "Reports"),
        ("Reports.HR.View", "View HR reports", "Reports"),
        ("Reports.Production.View", "View production reports", "Reports"),
        ("Reports.Farm.View", "View farm reports", "Reports"),
        ("Reports.Quality.View", "View quality reports", "Reports"),
        ("Reports.Operations.View", "View operations reports", "Reports"),
        ("Reports.POS.View", "View POS reports", "Reports"),

        ("Ai.Chat.View", "Use AI assistant", "Ai"),
        ("Ai.Chat.Query", "Run AI read-only tools", "Ai"),
        ("Ai.Action.Propose", "Propose AI draft actions", "Ai"),
        ("Ai.Action.Confirm", "Confirm AI draft actions", "Ai"),
        ("Ai.Agent.Run", "Start and view AI agent runs", "Ai"),
        ("Ai.Agent.Confirm", "Confirm AI agent-prepared actions", "Ai"),
        ("Ai.Agent.Cancel", "Cancel AI agent runs", "Ai"),

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

        await EnsureDemoAdminPasswordAsync(db);
        await EnsureSweetsPosCatalogAsync(db);
        await SeedPermissionsAsync(db);
        await EnsureDocumentTypeFlagsAsync(db);
        await EnsureSalesDocumentTypesAsync(db);
        await EnsureSalesDemoDataAsync(db);
        await EnsureFinancePhase3Async(db);
        await EnsureInventoryPhase4Async(db);
        await EnsureHrPhase5Async(db);
        await EnsureProductionPhase6Async(db);
        await EnsureFarmQualityPhase7Async(db);
        await EnsureOperationsPosPhase8Async(db);
        await EnsureBiPhase9Async(db);
    }

    /// <summary>Keep demo admin login working: admin@softaxe.local / Admin@123.</summary>
    public static async Task EnsureDemoAdminPasswordAsync(AppDbContext db)
    {
        var admin = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == "admin@softaxe.local");
        if (admin is null) return;

        admin.PasswordHash = AuthService.HashPassword("Admin@123");
        admin.IsActive = true;
        admin.IsDeleted = false;
        await db.SaveChangesAsync();
    }

    /// <summary>Demo sweets SKUs with barcodes for POS scan testing.</summary>
    public static async Task EnsureSweetsPosCatalogAsync(AppDbContext db)
    {
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        var sweetsCat = await db.ProductCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == "Sweets");
        if (sweetsCat is null)
        {
            sweetsCat = new ProductCategory { TenantId = tenantId, Name = "Sweets" };
            db.ProductCategories.Add(sweetsCat);
            await db.SaveChangesAsync();
        }

        var kg = await db.UnitsOfMeasure.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "KG");
        if (kg is null)
        {
            kg = new UnitOfMeasure { TenantId = tenantId, Code = "KG", Name = "Kilogram" };
            db.UnitsOfMeasure.Add(kg);
            await db.SaveChangesAsync();
        }
        var pcs = await db.UnitsOfMeasure.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "PCS");
        if (pcs is null)
        {
            pcs = new UnitOfMeasure { TenantId = tenantId, Code = "PCS", Name = "Piece" };
            db.UnitsOfMeasure.Add(pcs);
            await db.SaveChangesAsync();
        }

        var mainWh = await db.Warehouses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && (x.Code == "MAIN" || x.IsDefault));

        var items = new (string Code, string Name, string Barcode, bool ByWeight, Guid UomId, decimal Sale, decimal Cost, decimal Stock)[]
        {
            ("SWT-GJ", "Gulab Jamun", "8901001000001", true, kg.Id, 1200m, 700m, 50m),
            ("SWT-BF", "Barfi", "8901001000002", true, kg.Id, 2000m, 1100m, 40m),
            ("SWT-LD", "Laddu", "8901001000003", true, kg.Id, 1500m, 800m, 35m),
            ("SWT-BOX", "Mixed Sweets Box", "8901001000004", false, pcs.Id, 2500m, 1400m, 80m),
        };

        foreach (var item in items)
        {
            var p = await db.Products.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == item.Code);
            if (p is null)
            {
                p = new Product
                {
                    TenantId = tenantId,
                    Code = item.Code,
                    Name = item.Name,
                    Barcode = item.Barcode,
                    CategoryId = sweetsCat.Id,
                    UnitOfMeasureId = item.UomId,
                    SalePrice = item.Sale,
                    PurchasePrice = item.Cost,
                    AverageCost = item.Cost,
                    SellByWeight = item.ByWeight,
                    IsFinishedGood = true,
                    IsRawMaterial = false,
                    IsActive = true,
                    ReorderLevel = 5
                };
                db.Products.Add(p);
                await db.SaveChangesAsync();
            }
            else
            {
                p.Barcode ??= item.Barcode;
                p.SellByWeight = item.ByWeight;
                p.SalePrice = item.Sale;
                p.AverageCost = item.Cost;
            }

            if (mainWh is null) continue;
            var stock = await db.WarehouseStocks.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == p.Id && x.WarehouseId == mainWh.Id);
            if (stock is null)
            {
                db.WarehouseStocks.Add(new WarehouseStock
                {
                    TenantId = tenantId,
                    ProductId = p.Id,
                    WarehouseId = mainWh.Id,
                    Quantity = item.Stock,
                    QualityStatus = StockQualityStatus.Unrestricted
                });
            }
            else if (stock.Quantity < 5)
            {
                stock.Quantity = item.Stock;
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Phase 9: COGS GL mapping for P&amp;L (account 5100).</summary>
    public static async Task EnsureBiPhase9Async(AppDbContext db)
    {
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        if (!await db.GlAccountMappings.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.MappingKey == GlMappingKeys.Cogs))
        {
            var cogs = await db.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "5100");
            if (cogs is not null)
            {
                db.GlAccountMappings.Add(new GlAccountMapping
                {
                    TenantId = tenantId,
                    MappingKey = GlMappingKeys.Cogs,
                    AccountId = cogs.Id,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
        }
    }

    /// <summary>Idempotent flags for PO/GRN/PI/SO/DO/SI/CP lifecycle.</summary>
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
                case "SO":
                    t.RequiresVerification = false;
                    t.RequiresApproval = true;
                    break;
                case "DO":
                    t.RequiresVerification = true;
                    t.RequiresApproval = false;
                    t.AffectsInventory = true;
                    break;
                case "SI":
                    t.RequiresVerification = true;
                    t.RequiresApproval = true;
                    t.AffectsGl = true;
                    break;
                case "CP":
                    t.RequiresVerification = false;
                    t.RequiresApproval = false;
                    t.AffectsGl = true;
                    break;
                case "JV":
                    t.RequiresVerification = false;
                    t.RequiresApproval = true;
                    t.AffectsGl = true;
                    break;
                case "SPAY":
                    t.RequiresVerification = false;
                    t.RequiresApproval = false;
                    t.AffectsGl = true;
                    break;
                case "LV":
                    t.RequiresVerification = false;
                    t.RequiresApproval = true;
                    break;
                case "PR":
                    t.RequiresVerification = true;
                    t.RequiresApproval = true;
                    t.AffectsGl = true;
                    break;
                case "HPAY":
                    t.RequiresVerification = false;
                    t.RequiresApproval = false;
                    t.AffectsGl = true;
                    break;
                case "MO":
                    t.RequiresVerification = false;
                    t.RequiresApproval = true;
                    t.AffectsInventory = true;
                    t.AffectsGl = true;
                    break;
                case "FI":
                    t.RequiresVerification = false;
                    t.RequiresApproval = true;
                    t.AffectsInventory = true;
                    break;
                case "FMT":
                    t.RequiresVerification = false;
                    t.RequiresApproval = false;
                    t.AffectsInventory = true;
                    break;
                case "FEX":
                    t.RequiresVerification = false;
                    t.RequiresApproval = false;
                    t.AffectsGl = true;
                    break;
                case "WBT":
                    t.RequiresVerification = false;
                    t.RequiresApproval = false;
                    break;
                case "GP":
                    t.RequiresVerification = false;
                    t.RequiresApproval = true;
                    break;
                case "POS":
                    t.RequiresVerification = false;
                    t.RequiresApproval = false;
                    break;
            }
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Ensure SO/DO/SI/CP document types exist for existing tenants.</summary>
    public static async Task EnsureSalesDocumentTypesAsync(AppDbContext db)
    {
        var tenants = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).ToListAsync();
        foreach (var tenantId in tenants)
        {
            var existing = await db.DocumentTypes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.Code)
                .ToListAsync();
            var codes = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
            void AddIfMissing(string code, string name, string prefix, bool verify, bool approve, bool inv = false, bool gl = false)
            {
                if (codes.Contains(code)) return;
                db.DocumentTypes.Add(new DocumentType
                {
                    TenantId = tenantId,
                    Code = code,
                    Name = name,
                    Prefix = prefix,
                    RequiresVerification = verify,
                    RequiresApproval = approve,
                    AffectsInventory = inv,
                    AffectsGl = gl
                });
            }
            AddIfMissing("SO", "Sales Order", "SO", false, true);
            AddIfMissing("DO", "Delivery Order", "DO", true, false, inv: true);
            AddIfMissing("SI", "Sales Invoice", "SI", true, true, gl: true);
            AddIfMissing("CP", "Customer Payment", "CP", false, false, gl: true);
            AddIfMissing("JV", "Journal Voucher", "JV", false, true, gl: true);
            AddIfMissing("SPAY", "Supplier Payment", "SPAY", false, false, gl: true);
            AddIfMissing("ST", "Stock Transfer", "ST", false, true, inv: true);
            AddIfMissing("SA", "Stock Adjustment", "SA", false, true, inv: true, gl: true);
            AddIfMissing("SC", "Stock Count", "SC", false, true, inv: true);
            AddIfMissing("LV", "Leave Request", "LV", false, true);
            AddIfMissing("PR", "Payroll Run", "PR", true, true, gl: true);
            AddIfMissing("HPAY", "Salary Payment", "HPAY", false, false, gl: true);
            AddIfMissing("MO", "Manufacturing Order", "MO", false, true, inv: true, gl: true);
            AddIfMissing("FI", "Feed Issue", "FI", false, true, inv: true);
            AddIfMissing("FMT", "Farm Medication Treatment", "FMT", false, false, inv: true);
            AddIfMissing("FEX", "Farm Expense", "FEX", false, false, gl: true);
            AddIfMissing("WBT", "Weighbridge Ticket", "WBT", false, false);
            AddIfMissing("GP", "Gate Pass", "GP", false, true);
            AddIfMissing("POS", "POS Sale", "POS", false, false);
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Demo customer + opening stock for sales acceptance.</summary>
    public static async Task EnsureSalesDemoDataAsync(AppDbContext db)
    {
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        if (!await db.Parties.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "CUS-001"))
        {
            var customer = new Party
            {
                TenantId = tenantId,
                Code = "CUS-001",
                Name = "Demo Customer",
                PartyType = PartyType.Customer
            };
            db.Parties.Add(customer);
            db.CustomerProfiles.Add(new CustomerProfile
            {
                TenantId = tenantId,
                Party = customer,
                PaymentTerms = "Net 30",
                CreditLimit = 100000
            });
        }

        if (!await db.Accounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "1100"))
        {
            db.Accounts.Add(new Account
            {
                TenantId = tenantId,
                Code = "1100",
                Name = "Accounts Receivable",
                AccountType = AccountType.Asset,
                IsPostable = true,
                IsSystem = true
            });
        }

        var demoProduct = await db.Products.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "DEMO-001");
        if (demoProduct is not null)
        {
            var demoStock = await db.WarehouseStocks.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == demoProduct.Id);
            if (demoStock is not null && demoStock.Quantity < 100)
                demoStock.Quantity = 100;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Phase 3: full CoA, monthly periods, cash/bank, AverageCost, JV/SPAY types.</summary>
    public static async Task EnsureFinancePhase3Async(AppDbContext db)
    {
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        var existingCodes = await db.Accounts.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Code)
            .ToListAsync();
        var have = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var acct in TenantBootstrap.DefaultChartOfAccounts(tenantId))
        {
            if (have.Contains(acct.Code)) continue;
            db.Accounts.Add(acct);
        }
        await db.SaveChangesAsync();

        var products = await db.Products.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.AverageCost == 0).ToListAsync();
        foreach (var p in products)
            p.AverageCost = p.PurchasePrice > 0 ? p.PurchasePrice : 100m;

        var company = await db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId);
        var fy = company is null ? null : await db.FinancialYears.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.CompanyId == company.Id && x.IsCurrent);
        if (fy is not null)
        {
            var periodCount = await db.FiscalPeriods.IgnoreQueryFilters().CountAsync(x => x.FinancialYearId == fy.Id);
            if (periodCount == 0)
            {
                for (var m = 1; m <= 12; m++)
                {
                    var start = new DateTime(fy.StartDate.Year, m, 1);
                    var end = start.AddMonths(1).AddDays(-1);
                    if (end > fy.EndDate) end = fy.EndDate;
                    if (start > fy.EndDate) break;
                    db.FiscalPeriods.Add(new FiscalPeriod
                    {
                        TenantId = tenantId,
                        FinancialYearId = fy.Id,
                        Name = start.ToString("MMMM yyyy"),
                        PeriodNumber = m,
                        StartDate = start,
                        EndDate = end
                    });
                }
            }
        }

        var cashGl = await db.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "1300");
        var bankGl = await db.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "1400");
        if (cashGl is not null && !await db.CashBankAccounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "CASH"))
        {
            db.CashBankAccounts.Add(new CashBankAccount
            {
                TenantId = tenantId,
                Code = "CASH",
                Name = "Main Cash",
                AccountKind = CashBankKind.Cash,
                GlAccountId = cashGl.Id,
                IsDefault = true
            });
        }
        if (bankGl is not null && !await db.CashBankAccounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "BANK"))
        {
            db.CashBankAccounts.Add(new CashBankAccount
            {
                TenantId = tenantId,
                Code = "BANK",
                Name = "Main Bank",
                AccountKind = CashBankKind.Bank,
                GlAccountId = bankGl.Id
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Phase 4: Branch warehouse, locations, UOM conversion sample, ST/SA/SC types, CoA 5600.</summary>
    public static async Task EnsureInventoryPhase4Async(AppDbContext db)
    {
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        if (!await db.Accounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "5600"))
        {
            db.Accounts.Add(new Account
            {
                TenantId = tenantId,
                Code = "5600",
                Name = "Inventory Adjustment",
                AccountType = AccountType.Expense,
                Level = 2,
                IsPostable = true,
                IsSystem = true
            });
        }

        var branch = await db.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault)
            ?? await db.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId);
        if (branch is not null && !await db.Warehouses.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "BRANCH"))
        {
            db.Warehouses.Add(new Warehouse
            {
                TenantId = tenantId,
                BranchId = branch.Id,
                Code = "BRANCH",
                Name = "Branch Warehouse",
                IsDefault = false
            });
        }

        var mainWh = await db.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "MAIN");
        if (mainWh is not null && !await db.WarehouseLocations.IgnoreQueryFilters().AnyAsync(x => x.WarehouseId == mainWh.Id))
        {
            db.WarehouseLocations.AddRange(
                new WarehouseLocation { TenantId = tenantId, WarehouseId = mainWh.Id, Code = "A-01", Name = "Aisle A-01" },
                new WarehouseLocation { TenantId = tenantId, WarehouseId = mainWh.Id, Code = "A-02", Name = "Aisle A-02" });
        }

        var bag = await db.UnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "BAG");
        var kg = await db.UnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "KG");
        if (kg is null && bag is not null)
        {
            kg = new UnitOfMeasure { TenantId = tenantId, Code = "KG", Name = "Kilogram" };
            db.UnitsOfMeasure.Add(kg);
            await db.SaveChangesAsync();
        }
        if (bag is not null && kg is not null &&
            !await db.UnitConversions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.FromUomId == bag.Id && x.ToUomId == kg.Id))
        {
            db.UnitConversions.Add(new UnitConversion
            {
                TenantId = tenantId,
                FromUomId = bag.Id,
                ToUomId = kg.Id,
                Factor = 25m,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Phase 5: HR CoA liabilities, GL mappings, org masters, EMP-001, components, leave type.</summary>
    public static async Task EnsureHrPhase5Async(AppDbContext db)
    {
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        void EnsureAccount(string code, string name, AccountType type)
        {
            if (db.Accounts.IgnoreQueryFilters().Any(x => x.TenantId == tenantId && x.Code == code)) return;
            db.Accounts.Add(new Account
            {
                TenantId = tenantId,
                Code = code,
                Name = name,
                AccountType = type,
                Level = 2,
                IsPostable = true,
                IsSystem = true
            });
        }

        EnsureAccount("2200", "Salary Payable", AccountType.Liability);
        EnsureAccount("2210", "Tax Payable", AccountType.Liability);
        EnsureAccount("2220", "Other Payroll Payable", AccountType.Liability);
        EnsureAccount("5200", "Salaries Expense", AccountType.Expense);
        await db.SaveChangesAsync();

        async Task EnsureMapping(string key, string accountCode)
        {
            if (await db.GlAccountMappings.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.MappingKey == key))
                return;
            var acct = await db.Accounts.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId && x.Code == accountCode);
            db.GlAccountMappings.Add(new GlAccountMapping
            {
                TenantId = tenantId,
                MappingKey = key,
                AccountId = acct.Id,
                IsActive = true
            });
        }

        await EnsureMapping(GlMappingKeys.PayrollExpense, "5200");
        await EnsureMapping(GlMappingKeys.SalaryPayable, "2200");
        await EnsureMapping(GlMappingKeys.TaxPayable, "2210");
        await EnsureMapping(GlMappingKeys.OtherPayable, "2220");

        if (!await db.Departments.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "ADMIN"))
        {
            db.Departments.Add(new Department { TenantId = tenantId, Code = "ADMIN", Name = "Administration" });
        }
        if (!await db.Designations.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "MGR"))
        {
            db.Designations.Add(new Designation { TenantId = tenantId, Code = "MGR", Name = "Manager" });
        }

        void EnsureComponent(string code, string name, PayrollComponentType type, string? glKey, bool system = true)
        {
            if (db.PayrollComponents.IgnoreQueryFilters().Any(x => x.TenantId == tenantId && x.Code == code)) return;
            db.PayrollComponents.Add(new PayrollComponent
            {
                TenantId = tenantId,
                Code = code,
                Name = name,
                ComponentType = type,
                IsSystem = system,
                AffectsGross = type == PayrollComponentType.Earning,
                GlMappingKey = glKey
            });
        }

        EnsureComponent("BASIC", "Basic Salary", PayrollComponentType.Earning, null);
        EnsureComponent("ALLOW", "Allowance", PayrollComponentType.Earning, null);
        EnsureComponent("TAX", "Tax", PayrollComponentType.Deduction, GlMappingKeys.TaxPayable);
        EnsureComponent("OTHER_DED", "Other Deduction", PayrollComponentType.Deduction, GlMappingKeys.OtherPayable);

        if (!await db.LeaveTypes.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "ANNUAL"))
        {
            db.LeaveTypes.Add(new LeaveType
            {
                TenantId = tenantId,
                Code = "ANNUAL",
                Name = "Annual Leave",
                Paid = true,
                AnnualAllowanceDays = 12
            });
        }

        await db.SaveChangesAsync();

        var branch = await db.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault)
            ?? await db.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId);
        var dept = await db.Departments.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId && x.Code == "ADMIN");
        var desig = await db.Designations.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId && x.Code == "MGR");

        if (branch is not null && !await db.Employees.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.EmployeeCode == "EMP-001"))
        {
            var emp = new Employee
            {
                TenantId = tenantId,
                EmployeeCode = "EMP-001",
                Name = "Demo Employee",
                DepartmentId = dept.Id,
                DesignationId = desig.Id,
                BranchId = branch.Id,
                JoiningDate = new DateTime(DateTime.UtcNow.Year, 1, 1),
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.Permanent,
                BasicSalary = 100_000m,
                IsActive = true
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            var allow = await db.PayrollComponents.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId && x.Code == "ALLOW");
            var tax = await db.PayrollComponents.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId && x.Code == "TAX");
            var other = await db.PayrollComponents.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId && x.Code == "OTHER_DED");
            var profile = new PayrollProfile { TenantId = tenantId, EmployeeId = emp.Id };
            db.PayrollProfiles.Add(profile);
            await db.SaveChangesAsync();
            db.PayrollProfileLines.AddRange(
                new PayrollProfileLine { TenantId = tenantId, PayrollProfileId = profile.Id, PayrollComponentId = allow.Id, Amount = 20_000m },
                new PayrollProfileLine { TenantId = tenantId, PayrollProfileId = profile.Id, PayrollComponentId = tax.Id, Amount = 5_000m },
                new PayrollProfileLine { TenantId = tenantId, PayrollProfileId = profile.Id, PayrollComponentId = other.Id, Amount = 2_000m });

            var annual = await db.LeaveTypes.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId && x.Code == "ANNUAL");
            var year = DateTime.UtcNow.Year;
            db.LeaveBalances.Add(new LeaveBalance
            {
                TenantId = tenantId,
                EmployeeId = emp.Id,
                LeaveTypeId = annual.Id,
                Year = year,
                Entitled = 12,
                Used = 0,
                Remaining = 12
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Phase 6: WIP CoA, inventory mappings, RM/FG products, BOM, MO doc type.</summary>
    public static async Task EnsureProductionPhase6Async(AppDbContext db)
    {
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        if (!await db.Accounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "1210"))
        {
            db.Accounts.Add(new Account
            {
                TenantId = tenantId,
                Code = "1210",
                Name = "Work in Process",
                AccountType = AccountType.Asset,
                Level = 2,
                IsPostable = true,
                IsSystem = true
            });
        }
        if (!await db.Accounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "1200"))
        {
            db.Accounts.Add(new Account
            {
                TenantId = tenantId,
                Code = "1200",
                Name = "Inventory",
                AccountType = AccountType.Asset,
                Level = 2,
                IsPostable = true,
                IsSystem = true
            });
        }
        await db.SaveChangesAsync();

        async Task EnsureMapping(string key, string accountCode)
        {
            if (await db.GlAccountMappings.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.MappingKey == key))
                return;
            var acct = await db.Accounts.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId && x.Code == accountCode);
            db.GlAccountMappings.Add(new GlAccountMapping
            {
                TenantId = tenantId,
                MappingKey = key,
                AccountId = acct.Id,
                IsActive = true
            });
        }
        await EnsureMapping(GlMappingKeys.Inventory, "1200");
        await EnsureMapping(GlMappingKeys.WorkInProcess, "1210");

        var category = await db.ProductCategories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId)
            ?? new ProductCategory { TenantId = tenantId, Name = "General" };
        if (category.Id == Guid.Empty) { db.ProductCategories.Add(category); await db.SaveChangesAsync(); }

        var kg = await db.UnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "KG");
        if (kg is null)
        {
            kg = new UnitOfMeasure { TenantId = tenantId, Code = "KG", Name = "Kilogram" };
            db.UnitsOfMeasure.Add(kg);
            await db.SaveChangesAsync();
        }
        var pcs = await db.UnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "PCS");
        if (pcs is null)
        {
            pcs = new UnitOfMeasure { TenantId = tenantId, Code = "PCS", Name = "Piece" };
            db.UnitsOfMeasure.Add(pcs);
            await db.SaveChangesAsync();
        }

        async Task<Product> EnsureProduct(string code, string name, bool rm, bool fg, Guid uomId, decimal avg, decimal purchase)
        {
            var p = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code);
            if (p is not null) return p;
            p = new Product
            {
                TenantId = tenantId,
                Code = code,
                Name = name,
                CategoryId = category.Id,
                UnitOfMeasureId = uomId,
                IsRawMaterial = rm,
                IsFinishedGood = fg,
                AverageCost = avg,
                PurchasePrice = purchase,
                SalePrice = fg ? avg * 1.25m : purchase
            };
            db.Products.Add(p);
            await db.SaveChangesAsync();
            return p;
        }

        var rmA = await EnsureProduct("RM-A", "Raw Material A", true, false, kg.Id, 100m, 100m);
        var rmB = await EnsureProduct("RM-B", "Raw Material B", true, false, kg.Id, 50m, 50m);
        var fgA = await EnsureProduct("FG-A", "Finished Product A", false, true, pcs.Id, 0m, 0m);

        var mainWh = await db.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "MAIN")
            ?? await db.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault);
        if (mainWh is not null)
        {
            async Task EnsureStock(Guid productId, decimal qty)
            {
                var s = await db.WarehouseStocks.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == productId && x.WarehouseId == mainWh.Id);
                if (s is null)
                {
                    db.WarehouseStocks.Add(new WarehouseStock
                    {
                        TenantId = tenantId,
                        ProductId = productId,
                        WarehouseId = mainWh.Id,
                        Quantity = qty
                    });
                }
                else if (productId == rmA.Id || productId == rmB.Id)
                {
                    // Keep at least acceptance opening if depleted below seed (only bump if zero)
                    if (s.Quantity <= 0) s.Quantity = qty;
                }
            }
            await EnsureStock(rmA.Id, 20m);
            await EnsureStock(rmB.Id, 10m);
            await EnsureStock(fgA.Id, 0m);
        }

        if (!await db.BillOfMaterials.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "BOM-FGA"))
        {
            var bom = new BillOfMaterial
            {
                TenantId = tenantId,
                Code = "BOM-FGA",
                Name = "BOM Finished Product A",
                FinishedProductId = fgA.Id,
                Version = 1,
                EffectiveFrom = DateTime.UtcNow.Date,
                IsActive = true
            };
            db.BillOfMaterials.Add(bom);
            await db.SaveChangesAsync();
            db.BillOfMaterialLines.AddRange(
                new BillOfMaterialLine { TenantId = tenantId, BillOfMaterialId = bom.Id, LineNo = 1, ComponentProductId = rmA.Id, QtyPer = 2m, UomId = kg.Id },
                new BillOfMaterialLine { TenantId = tenantId, BillOfMaterialId = bom.Id, LineNo = 2, ComponentProductId = rmB.Id, QtyPer = 1m, UomId = kg.Id });
        }

        await db.SaveChangesAsync();
    }

    public static async Task EnsureFarmQualityPhase7Async(AppDbContext db)
    {
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        if (!await db.Accounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "5700"))
        {
            db.Accounts.Add(new Account
            {
                TenantId = tenantId,
                Code = "5700",
                Name = "Farm Expense",
                AccountType = AccountType.Expense,
                Level = 2,
                IsPostable = true,
                IsSystem = true
            });
            await db.SaveChangesAsync();
        }

        if (!await db.GlAccountMappings.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.MappingKey == GlMappingKeys.FarmExpense))
        {
            var acct = await db.Accounts.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId && x.Code == "5700");
            db.GlAccountMappings.Add(new GlAccountMapping
            {
                TenantId = tenantId,
                MappingKey = GlMappingKeys.FarmExpense,
                AccountId = acct.Id,
                IsActive = true
            });
        }

        var category = await db.ProductCategories.IgnoreQueryFilters().FirstAsync(x => x.TenantId == tenantId);
        var kg = await db.UnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "KG")
            ?? new UnitOfMeasure { TenantId = tenantId, Code = "KG", Name = "Kilogram" };
        if (kg.Id == Guid.Empty) { db.UnitsOfMeasure.Add(kg); await db.SaveChangesAsync(); }

        var feed = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "FEED-A");
        if (feed is null)
        {
            feed = new Product
            {
                TenantId = tenantId,
                Code = "FEED-A",
                Name = "Farm Feed A",
                CategoryId = category.Id,
                UnitOfMeasureId = kg.Id,
                IsRawMaterial = true,
                IsFinishedGood = false,
                AverageCost = 40m,
                PurchasePrice = 40m,
                SalePrice = 40m
            };
            db.Products.Add(feed);
            await db.SaveChangesAsync();
        }

        var mainWh = await db.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "MAIN")
            ?? await db.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault);
        if (mainWh is not null)
        {
            var stock = await db.WarehouseStocks.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == feed.Id && x.WarehouseId == mainWh.Id);
            if (stock is null)
            {
                db.WarehouseStocks.Add(new WarehouseStock
                {
                    TenantId = tenantId,
                    ProductId = feed.Id,
                    WarehouseId = mainWh.Id,
                    Quantity = 500m,
                    QualityStatus = StockQualityStatus.Unrestricted
                });
            }
            else if (stock.Quantity <= 0)
            {
                stock.Quantity = 500m;
            }
        }

        var branch = await db.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault);
        var farm = await db.Farms.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "FARM-A");
        if (farm is null)
        {
            farm = new Farm
            {
                TenantId = tenantId,
                Code = "FARM-A",
                Name = "Farm A",
                FarmType = FarmType.Poultry,
                BranchId = branch?.Id,
                IsActive = true
            };
            db.Farms.Add(farm);
            await db.SaveChangesAsync();
            db.FarmUnits.Add(new FarmUnit
            {
                TenantId = tenantId,
                FarmId = farm.Id,
                Code = "SHED-01",
                Name = "Shed 01",
                Capacity = 15000,
                IsActive = true
            });
        }

        if (!await db.LabTestTypes.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "MOISTURE"))
        {
            db.LabTestTypes.Add(new LabTestType
            {
                TenantId = tenantId,
                Code = "MOISTURE",
                Name = "Moisture",
                Unit = "%",
                MaxValue = 14m,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Phase 8: Operations vehicles/gate, weighbridge, POS terminal/party/CARD/SalesRevenue.</summary>
    public static async Task EnsureOperationsPosPhase8Async(AppDbContext db)
    {
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync();
        if (tenantId == Guid.Empty) return;

        if (!await db.GlAccountMappings.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.MappingKey == GlMappingKeys.SalesRevenue))
        {
            var salesGl = await db.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "4100");
            if (salesGl is not null)
            {
                db.GlAccountMappings.Add(new GlAccountMapping
                {
                    TenantId = tenantId,
                    MappingKey = GlMappingKeys.SalesRevenue,
                    AccountId = salesGl.Id,
                    IsActive = true
                });
            }
        }

        var bankGl = await db.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "1400");
        if (bankGl is not null && !await db.CashBankAccounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "CARD"))
        {
            db.CashBankAccounts.Add(new CashBankAccount
            {
                TenantId = tenantId,
                Code = "CARD",
                Name = "Card Clearing",
                AccountKind = CashBankKind.Bank,
                GlAccountId = bankGl.Id
            });
        }

        if (!await db.Parties.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "CUS-POS"))
        {
            var walkIn = new Party
            {
                TenantId = tenantId,
                Code = "CUS-POS",
                Name = "POS Walk-in",
                PartyType = PartyType.Customer
            };
            db.Parties.Add(walkIn);
            db.CustomerProfiles.Add(new CustomerProfile
            {
                TenantId = tenantId,
                Party = walkIn,
                PaymentTerms = "Cash",
                CreditLimit = 0
            });
        }

        var truckType = await db.VehicleTypes.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "TRUCK");
        if (truckType is null)
        {
            truckType = new VehicleType { TenantId = tenantId, Code = "TRUCK", Name = "Truck", IsActive = true };
            db.VehicleTypes.Add(truckType);
            await db.SaveChangesAsync();
        }

        if (!await db.Vehicles.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "TEST-001"))
        {
            db.Vehicles.Add(new Vehicle
            {
                TenantId = tenantId,
                Code = "TEST-001",
                VehicleTypeId = truckType.Id,
                Make = "Demo",
                Model = "Weighbridge",
                IsActive = true
            });
        }

        if (!await db.WeighbridgeScales.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "WB-1"))
        {
            db.WeighbridgeScales.Add(new WeighbridgeScale
            {
                TenantId = tenantId,
                Code = "WB-1",
                Name = "Main Scale",
                Location = "Gate",
                IsActive = true
            });
        }

        var mainWh = await db.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "MAIN")
            ?? await db.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault);
        var branch = await db.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault);
        if (mainWh is not null && !await db.PosTerminals.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == "POS-1"))
        {
            db.PosTerminals.Add(new PosTerminal
            {
                TenantId = tenantId,
                Code = "POS-1",
                Name = "Front Counter",
                WarehouseId = mainWh.Id,
                BranchId = branch?.Id,
                IsActive = true
            });
        }

        var demo = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "DEMO-001");
        if (demo is not null && mainWh is not null)
        {
            var stock = await db.WarehouseStocks.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ProductId == demo.Id && x.WarehouseId == mainWh.Id);
            if (stock is null)
            {
                db.WarehouseStocks.Add(new WarehouseStock
                {
                    TenantId = tenantId,
                    ProductId = demo.Id,
                    WarehouseId = mainWh.Id,
                    Quantity = 500m,
                    QualityStatus = StockQualityStatus.Unrestricted
                });
            }
            else if (stock.Quantity < 50)
            {
                stock.Quantity = 500m;
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedCoreAsync(AppDbContext db, ITenantContext tenant)
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        tenant.Set(tenantId, null);
        var now = DateTime.UtcNow;
        var tenantEntity = new Tenant { Id = tenantId, TenantId = tenantId, Name = "SheikhGo-Nexus-ERP Demo", Code = "DEMO", Slug = "demo", Industry = IndustryType.FeedMill };
        db.Tenants.Add(tenantEntity);
        var company = new Company { TenantId = tenantId, Name = "SheikhGo-Nexus-ERP Demo", ShortName = "NEXUS", City = "Multan", Email = "admin@softaxe.local", Industry = IndustryType.FeedMill };
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
        var product = new Product { TenantId = tenantId, Code = "DEMO-001", Name = "Demo Product", CategoryId = category.Id, UnitOfMeasureId = bag.Id, PurchasePrice = 100, SalePrice = 125, AverageCost = 100 };
        db.Products.Add(product);
        db.WarehouseStocks.Add(new WarehouseStock { TenantId = tenantId, Product = product, WarehouseId = warehouse.Id, Quantity = 100 });
        db.DocumentTypes.AddRange(
            new DocumentType { TenantId = tenantId, Code = "PO", Name = "Purchase Order", Prefix = "PO", RequiresVerification = false, RequiresApproval = true },
            new DocumentType { TenantId = tenantId, Code = "GRN", Name = "Goods Receipt", Prefix = "GRN", RequiresVerification = true, RequiresApproval = false, AffectsInventory = true },
            new DocumentType { TenantId = tenantId, Code = "PI", Name = "Purchase Invoice", Prefix = "PI", RequiresVerification = true, RequiresApproval = true, AffectsGl = true },
            new DocumentType { TenantId = tenantId, Code = "SO", Name = "Sales Order", Prefix = "SO", RequiresVerification = false, RequiresApproval = true },
            new DocumentType { TenantId = tenantId, Code = "DO", Name = "Delivery Order", Prefix = "DO", RequiresVerification = true, RequiresApproval = false, AffectsInventory = true },
            new DocumentType { TenantId = tenantId, Code = "SI", Name = "Sales Invoice", Prefix = "SI", RequiresVerification = true, RequiresApproval = true, AffectsGl = true },
            new DocumentType { TenantId = tenantId, Code = "CP", Name = "Customer Payment", Prefix = "CP", RequiresVerification = false, RequiresApproval = false, AffectsGl = true },
            new DocumentType { TenantId = tenantId, Code = "JV", Name = "Journal Voucher", Prefix = "JV", RequiresVerification = false, RequiresApproval = true, AffectsGl = true },
            new DocumentType { TenantId = tenantId, Code = "SPAY", Name = "Supplier Payment", Prefix = "SPAY", RequiresVerification = false, RequiresApproval = false, AffectsGl = true },
            new DocumentType { TenantId = tenantId, Code = "ST", Name = "Stock Transfer", Prefix = "ST", RequiresVerification = false, RequiresApproval = true, AffectsInventory = true },
            new DocumentType { TenantId = tenantId, Code = "SA", Name = "Stock Adjustment", Prefix = "SA", RequiresVerification = false, RequiresApproval = true, AffectsInventory = true, AffectsGl = true },
            new DocumentType { TenantId = tenantId, Code = "SC", Name = "Stock Count", Prefix = "SC", RequiresVerification = false, RequiresApproval = true, AffectsInventory = true });
        var supplier = new Party { TenantId = tenantId, Code = "SUP-001", Name = "Demo Supplier", PartyType = PartyType.Supplier };
        db.Parties.Add(supplier);
        db.SupplierProfiles.Add(new SupplierProfile { TenantId = tenantId, Party = supplier, PaymentTerms = "Net 30" });
        var customer = new Party { TenantId = tenantId, Code = "CUS-001", Name = "Demo Customer", PartyType = PartyType.Customer };
        db.Parties.Add(customer);
        db.CustomerProfiles.Add(new CustomerProfile { TenantId = tenantId, Party = customer, PaymentTerms = "Net 30", CreditLimit = 100000 });
        db.Accounts.AddRange(TenantBootstrap.DefaultChartOfAccounts(tenantId));
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
