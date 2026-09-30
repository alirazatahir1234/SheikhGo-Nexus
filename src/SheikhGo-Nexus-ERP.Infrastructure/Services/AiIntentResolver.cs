using System.Globalization;
using System.Text.RegularExpressions;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Infrastructure.Services;

public static class AiIntents
{
    public const string SalesSummary = "SalesSummary";
    public const string CustomerOutstanding = "CustomerOutstanding";
    public const string InventoryBalance = "InventoryBalance";
    public const string InventoryDemandForecast = "InventoryDemandForecast";
    public const string PurchaseSummary = "PurchaseSummary";
    public const string SupplierOutstanding = "SupplierOutstanding";
    public const string TrialBalance = "TrialBalance";
    public const string ProfitLoss = "ProfitLoss";
    public const string BalanceSheet = "BalanceSheet";

    public const string CreatePurchaseOrderDraft = "CreatePurchaseOrderDraft";
    public const string CreateSalesOrderDraft = "CreateSalesOrderDraft";
    public const string CreateStockTransferDraft = "CreateStockTransferDraft";
    public const string CreateCustomerPaymentDraft = "CreateCustomerPaymentDraft";
    public const string CreateSupplierPaymentDraft = "CreateSupplierPaymentDraft";
    public const string CreateJournalEntryDraft = "CreateJournalEntryDraft";

    public const string RunInventoryReplenishmentAgent = "RunInventoryReplenishmentAgent";
    public const string AgentNotAvailable = "AgentNotAvailable";

    public const string Unknown = "Unknown";
    public const string DeniedUnsafe = "DeniedUnsafe";

    public static bool IsActionIntent(string intent) =>
        intent is CreatePurchaseOrderDraft or CreateSalesOrderDraft or CreateStockTransferDraft
            or CreateCustomerPaymentDraft or CreateSupplierPaymentDraft or CreateJournalEntryDraft;

    public static bool IsAgentIntent(string intent) =>
        intent is RunInventoryReplenishmentAgent or AgentNotAvailable;
}

public class AiIntentResolver : IAiIntentResolver
{
    private static readonly string[] UnsafeMarkers =
    [
        "raw sql", "execute sql", "drop table", "delete from", "truncate ",
        "ignore permission", "bypass permission", "bypass rbac",
        "ignore confirmation", "skip confirmation", "without confirmation", "auto confirm",
        "softaxe", "connect to softaxe", "premierfood", "connection string",
        "ignore previous", "system prompt", "jailbreak",
        "approve payroll", "approve the po", "approve this po", "approve purchase order",
        "post the invoice", "post invoice", "post the payment", "post payment",
        "post the journal", "post journal", "post this po", "post the po",
        "delete record", "delete the", "change inventory qty", "force stock"
    ];

    public AiIntentResolution Resolve(string message)
    {
        var text = (message ?? "").Trim();
        var lower = text.ToLowerInvariant();
        var (from, to, asOf) = ResolvePeriod(lower);
        var amount = ResolveAmount(lower);
        var slots = ParseActionSlots(text, lower);

        if (UnsafeMarkers.Any(m => lower.Contains(m)))
            return new AiIntentResolution(AiIntents.DeniedUnsafe, from, to, asOf, amount, "Unsafe or disallowed request.", slots);

        // Forbidden direct post/approve language (even without other markers)
        if (Regex.IsMatch(lower, @"\b(approve|post)\b.*\b(po|purchase orders?|sales orders?|invoices?|payments?|journals?|payroll)\b")
            || Regex.IsMatch(lower, @"\b(po|purchase orders?|sales orders?|invoices?|payments?|journals?|payroll)\b.*\b(approve|post)\b")
            || ContainsAny(lower, "approve these", "post these", "approve and post", "post and approve"))
            return new AiIntentResolution(AiIntents.DeniedUnsafe, from, to, asOf, amount, "AI cannot approve or post documents.", slots);

        // Phase 10C agent intents (before single-shot create)
        if (IsReplenishmentAgent(lower))
            return new(AiIntents.RunInventoryReplenishmentAgent, from, to, asOf, amount, null, slots);

        if (ContainsAny(lower, "sales agent", "finance agent", "finance assistant", "reporting agent", "management assistant", "procurement agent")
            && !IsReplenishmentAgent(lower))
            return new(AiIntents.AgentNotAvailable, from, to, asOf, amount, "Only Inventory Replenishment agent is available in Phase 10C.", slots);

        // Action intents before read-only query intents when create/draft language is present
        if (IsCreateLanguage(lower))
        {
            if (ContainsAny(lower, "journal", "journal entry", "journal voucher", " jv "))
                return new(AiIntents.CreateJournalEntryDraft, from, to, asOf, amount, null, slots);

            if (ContainsAny(lower, "stock transfer", "transfer stock", "transfer inventory", "warehouse transfer"))
                return new(AiIntents.CreateStockTransferDraft, from, to, asOf, amount, null, slots);

            if (ContainsAny(lower, "supplier payment", "pay supplier", "payment to supplier", "pay the supplier"))
                return new(AiIntents.CreateSupplierPaymentDraft, from, to, asOf, amount, null, slots);

            if (ContainsAny(lower, "customer payment", "receive payment", "payment from customer", "record payment from", "pay from customer"))
                return new(AiIntents.CreateCustomerPaymentDraft, from, to, asOf, amount, null, slots);

            if (ContainsAny(lower, "sales order", "so for", "create so", "draft so", "order for customer"))
                return new(AiIntents.CreateSalesOrderDraft, from, to, asOf, amount, null, slots);

            if (ContainsAny(lower, "purchase order", "po for", "create po", "draft po", "order from", "buy ", "purchase "))
                return new(AiIntents.CreatePurchaseOrderDraft, from, to, asOf, amount, null, slots);

            if (ContainsAny(lower, "payment") && ContainsAny(lower, "customer", "from "))
                return new(AiIntents.CreateCustomerPaymentDraft, from, to, asOf, amount, null, slots);

            if (ContainsAny(lower, "payment") && ContainsAny(lower, "supplier", "vendor"))
                return new(AiIntents.CreateSupplierPaymentDraft, from, to, asOf, amount, null, slots);
        }

        if (ContainsAny(lower, "trial balance", "trialbalance", "tb report"))
            return new(AiIntents.TrialBalance, from, to, asOf, amount, null);

        if (ContainsAny(lower, "balance sheet", "balancesheet", "statement of financial position"))
            return new(AiIntents.BalanceSheet, from, to, asOf, amount, null);

        if (ContainsAny(lower, "profit", "p&l", "p and l", "income statement", "net profit", "gross profit"))
            return new(AiIntents.ProfitLoss, from, to, asOf, amount, null);

        if (ContainsAny(lower, "owe supplier", "owe suppliers", "supplier outstanding", "accounts payable", " a/p", " ap ", "payables", "what do we owe"))
            return new(AiIntents.SupplierOutstanding, from, to, asOf, amount, null);

        if (ContainsAny(lower, "purchase summary", "purchases this", "this month's purchase", "procurement summary", "buying summary"))
            return new(AiIntents.PurchaseSummary, from, to, asOf, amount, null);

        // Predictive (before generic inventory balance)
        if (ContainsAny(lower,
                "forecast", "stock-out", "stock out", "run out", "demand forecast", "demand for",
                "days of stock", "how many days", "reorder recommendation", "recommended order",
                "stockout risk", "will we run out", "predict inventory", "predictive"))
            return new(AiIntents.InventoryDemandForecast, from, to, asOf, amount, null);

        if (ContainsAny(lower, "inventory", "stock", "on hand", "warehouse value", "current stock", "stock balance"))
            return new(AiIntents.InventoryBalance, from, to, asOf, amount, null);

        if (ContainsAny(lower, "owe us", "who owes", "customers owe", "customer owe", "owe more", "owing", "customer outstanding", "receivable", " a/r", " ar ", "total ar", "accounts receivable"))
            return new(AiIntents.CustomerOutstanding, from, to, asOf, amount, null);

        if (ContainsAny(lower, "sales", "revenue this", "this month's sale", "sales summary", "sold"))
            return new(AiIntents.SalesSummary, from, to, asOf, amount, null);

        if (ContainsAny(lower, "purchase", "bought", "procurement"))
            return new(AiIntents.PurchaseSummary, from, to, asOf, amount, null);

        return new(AiIntents.Unknown, from, to, asOf, amount, "Could not map to a supported intent.");
    }

    private static bool IsCreateLanguage(string lower) =>
        ContainsAny(lower, "create ", "draft ", "prepare ", "make a ", "make an ", "raise a ", "raise an ", "add a ", "new ");

    private static bool IsReplenishmentAgent(string lower)
    {
        var stockCue = ContainsAny(lower, "minimum stock", "min stock", "reorder", "replenish", "below minimum", "low stock", "stock below", "shortages", "shortage");
        var actionCue = ContainsAny(lower, "purchase order", "prepare po", "prepare purchase", "find products", "which products", "check which", "replenishment");
        if (stockCue && (actionCue || ContainsAny(lower, "prepare", "find", "check", "suggest")))
            return true;
        if (ContainsAny(lower, "inventory replenishment", "replenishment agent", "reorder agent"))
            return true;
        return false;
    }

    private static AiActionSlots ParseActionSlots(string text, string lower)
    {
        decimal? qty = null;
        decimal? rate = null;
        decimal? payAmount = null;
        string? product = null;
        string? party = null;
        string? fromWh = null;
        string? toWh = null;
        string? debit = null;
        string? credit = null;

        var qtyMatch = Regex.Match(lower, @"\b(\d+(?:\.\d+)?)\s*(?:units?|pcs|pieces|qty|quantity)?\b");
        if (qtyMatch.Success && decimal.TryParse(qtyMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var q))
            qty = q;

        var amountMatch = Regex.Match(lower, @"(?:pay|payment(?:\s+of)?|amount(?:\s+of)?)\s*([\d,]+(?:\.\d+)?)");
        if (amountMatch.Success && decimal.TryParse(amountMatch.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var a))
            payAmount = a;

        var rateMatch = Regex.Match(lower, @"(?:@|at|rate)\s*([\d,]+(?:\.\d+)?)");
        if (rateMatch.Success && decimal.TryParse(rateMatch.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var r))
            rate = r;

        // "for 500 Demo Product from Demo Supplier" / "of Finished Product A from ABC"
        var poMatch = Regex.Match(text,
            @"(?:for|of)\s+(?:\d+(?:\.\d+)?\s+)?(.+?)\s+from\s+(.+?)(?:\.|$)",
            RegexOptions.IgnoreCase);
        if (poMatch.Success)
        {
            product = CleanHint(poMatch.Groups[1].Value);
            party = CleanHint(poMatch.Groups[2].Value);
        }

        var soMatch = Regex.Match(text,
            @"(?:for|of)\s+(?:\d+(?:\.\d+)?\s+)?(.+?)\s+(?:to|for)\s+(?:customer\s+)?(.+?)(?:\.|$)",
            RegexOptions.IgnoreCase);
        if (!poMatch.Success && soMatch.Success && lower.Contains("sales"))
        {
            product = CleanHint(soMatch.Groups[1].Value);
            party = CleanHint(soMatch.Groups[2].Value);
        }

        var fromCust = Regex.Match(text, @"(?:from|for)\s+(?:customer\s+)?(.+?)(?:\s+for\s+|\s+amount|\.|$)", RegexOptions.IgnoreCase);
        if (party is null && (lower.Contains("customer payment") || lower.Contains("payment from")))
            party = CleanHint(fromCust.Success ? fromCust.Groups[1].Value : null);

        var toSup = Regex.Match(text, @"(?:to|for)\s+(?:supplier\s+)?(.+?)(?:\s+for\s+|\s+amount|\.|$)", RegexOptions.IgnoreCase);
        if (party is null && (lower.Contains("supplier payment") || lower.Contains("pay supplier")))
            party = CleanHint(toSup.Success ? toSup.Groups[1].Value : null);

        var transfer = Regex.Match(text,
            @"(?:from)\s+(.+?)\s+to\s+(.+?)(?:\.|$)",
            RegexOptions.IgnoreCase);
        if (transfer.Success && lower.Contains("transfer"))
        {
            fromWh = CleanHint(transfer.Groups[1].Value);
            toWh = CleanHint(transfer.Groups[2].Value);
            var prodInTransfer = Regex.Match(text,
                @"(?:transfer|of)\s+(?<qty>\d+(?:\.\d+)?)\s+(?<prod>.+?)\s+from\s+",
                RegexOptions.IgnoreCase);
            if (!prodInTransfer.Success)
                prodInTransfer = Regex.Match(text,
                    @"of\s+(?<prod>.+?)\s+from\s+",
                    RegexOptions.IgnoreCase);
            if (prodInTransfer.Success)
            {
                product = CleanHint(prodInTransfer.Groups["prod"].Value);
                if (prodInTransfer.Groups["qty"].Success &&
                    decimal.TryParse(prodInTransfer.Groups["qty"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var tq))
                    qty = tq;
            }
        }

        var debitM = Regex.Match(text, @"debit\s+(.+?)(?:\s+credit|\s+and\s+credit|,|\.|$)", RegexOptions.IgnoreCase);
        var creditM = Regex.Match(text, @"credit\s+(.+?)(?:\s+for\s+amount|\s+amount|\s+debit|,|\.|$)", RegexOptions.IgnoreCase);
        if (debitM.Success) debit = CleanHint(debitM.Groups[1].Value);
        if (creditM.Success) credit = CleanHint(creditM.Groups[1].Value);

        Guid? forcedCompany = null;
        Guid? forcedBranch = null;
        foreach (Match m in Regex.Matches(text, @"\b([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\b"))
        {
            if (Guid.TryParse(m.Groups[1].Value, out var g))
            {
                if (lower.Contains("company") && forcedCompany is null) forcedCompany = g;
                else if (lower.Contains("branch") && forcedBranch is null) forcedBranch = g;
            }
        }
        if (Regex.IsMatch(lower, @"other company|different company|another company|cross[- ]company"))
            forcedCompany ??= Guid.Parse("11111111-1111-1111-1111-111111111111");
        if (Regex.IsMatch(lower, @"other branch|different branch|another branch|cross[- ]branch"))
            forcedBranch ??= Guid.Parse("22222222-2222-2222-2222-222222222222");

        return new AiActionSlots(qty, payAmount, rate, party, product, fromWh, toWh, debit, credit, null, forcedCompany, forcedBranch);
    }

    private static string? CleanHint(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = Regex.Replace(s.Trim(), @"\s+", " ");
        t = Regex.Replace(t, @"^(units?|of|the|a|an)\s+", "", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"^\d+(?:\.\d+)?\s+", "");
        t = Regex.Replace(t, @"\s+for\s+amount.*$", "", RegexOptions.IgnoreCase);
        return string.IsNullOrWhiteSpace(t) ? null : t.Trim(' ', '.', ',');
    }

    private static bool ContainsAny(string hay, params string[] needles) =>
        needles.Any(n => hay.Contains(n, StringComparison.Ordinal));

    private static (DateTime From, DateTime To, DateTime AsOf) ResolvePeriod(string lower)
    {
        var today = DateTime.UtcNow.Date;
        if (lower.Contains("today"))
            return (today, today, today);
        if (lower.Contains("this week") || lower.Contains("current week"))
        {
            var diff = ((int)today.DayOfWeek + 6) % 7;
            var start = today.AddDays(-diff);
            return (start, today, today);
        }
        if (lower.Contains("ytd") || lower.Contains("year to date") || lower.Contains("this year"))
            return (new DateTime(today.Year, 1, 1), today, today);
        return (new DateTime(today.Year, today.Month, 1), today, today);
    }

    private static decimal? ResolveAmount(string lower)
    {
        var m = Regex.Match(lower, @"(?:more than|greater than|over|>)\s*([\d,]+(?:\.\d+)?)\s*(m|million)?");
        if (m.Success)
        {
            var n = decimal.Parse(m.Groups[1].Value.Replace(",", ""), CultureInfo.InvariantCulture);
            if (m.Groups[2].Success) n *= 1_000_000m;
            return n;
        }
        m = Regex.Match(lower, @"([\d,]+(?:\.\d+)?)\s*(m|million)");
        if (m.Success)
        {
            var n = decimal.Parse(m.Groups[1].Value.Replace(",", ""), CultureInfo.InvariantCulture);
            if (m.Groups[2].Success) n *= 1_000_000m;
            return n;
        }
        return null;
    }
}
