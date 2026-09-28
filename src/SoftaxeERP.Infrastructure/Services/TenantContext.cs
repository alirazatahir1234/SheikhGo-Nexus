using SoftaxeERP.Application.Interfaces;

namespace SoftaxeERP.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }

    public void Set(Guid tenantId, Guid? userId)
    {
        TenantId = tenantId;
        UserId = userId;
    }
}
