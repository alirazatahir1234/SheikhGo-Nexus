using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? ClientIp { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? FinancialYearId { get; private set; }

    public void Set(Guid tenantId, Guid? userId, string? clientIp = null, Guid? companyId = null, Guid? branchId = null, Guid? financialYearId = null)
    {
        TenantId = tenantId;
        UserId = userId;
        ClientIp = clientIp;
        CompanyId = companyId;
        BranchId = branchId;
        FinancialYearId = financialYearId;
    }
}
