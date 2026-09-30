using System.Text.Json;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Domain.Enums;

namespace SoftaxeERP.Infrastructure.Persistence;

public static class TenantBootstrap
{
    public static List<Account> DefaultChartOfAccounts(Guid tenantId)
    {
        Account A(string code, string name, AccountType type, int level, bool postable = true) =>
            new()
            {
                TenantId = tenantId,
                Code = code,
                Name = name,
                AccountType = type,
                Level = level,
                IsPostable = postable,
                IsSystem = true
            };

        return
        [
            A("1000", "Assets", AccountType.Asset, 1, false),
            A("1100", "Accounts Receivable", AccountType.Asset, 2),
            A("1200", "Inventory", AccountType.Asset, 2),
            A("1300", "Cash in Hand", AccountType.Asset, 2),
            A("1400", "Bank", AccountType.Asset, 2),
            A("2000", "Liabilities", AccountType.Liability, 1, false),
            A("2100", "Accounts Payable", AccountType.Liability, 2),
            A("3000", "Equity", AccountType.Equity, 1, false),
            A("3100", "Capital", AccountType.Equity, 2),
            A("4000", "Revenue", AccountType.Revenue, 1, false),
            A("4100", "Sales Revenue", AccountType.Revenue, 2),
            A("5000", "Expenses", AccountType.Expense, 1, false),
            A("5100", "Cost of Goods Sold", AccountType.Expense, 2),
            A("5200", "Salaries Expense", AccountType.Expense, 2),
            A("5300", "Freight Expense", AccountType.Expense, 2),
        ];
    }

    public static string SerializeModules(IEnumerable<string> modules) =>
        JsonSerializer.Serialize(modules.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

    public static IReadOnlyList<string> DeserializeModules(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return SoftaxeERP.Application.DTOs.SoftaxeModuleCatalog.Default;
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? SoftaxeERP.Application.DTOs.SoftaxeModuleCatalog.Default.ToArray();
        }
        catch
        {
            return SoftaxeERP.Application.DTOs.SoftaxeModuleCatalog.Default;
        }
    }

    public static string ToSlug(string name)
    {
        var slug = new string(name.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return slug.Trim('-');
    }
}
