using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class PermissionService(AppDbContext db, ITenantContext tenant) : IPermissionService
{
    public async Task EnsureAsync(string permissionCode, CancellationToken ct = default)
    {
        if (!await HasAsync(permissionCode, ct))
            throw new UnauthorizedAccessException($"Missing permission: {permissionCode}");
    }

    public async Task<bool> HasAsync(string permissionCode, CancellationToken ct = default)
    {
        if (tenant.UserId is null) return false;

        var user = await db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role!).ThenInclude(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Id == tenant.UserId, ct);

        if (user is null) return false;
        if (user.IsPlatformAdmin) return true;
        if (user.UserRoles.Any(r => string.Equals(r.Role?.Code, "Admin", StringComparison.OrdinalIgnoreCase)))
            return true;

        return user.UserRoles
            .SelectMany(r => r.Role?.RolePermissions ?? Enumerable.Empty<Domain.Entities.RolePermission>())
            .Any(rp => string.Equals(rp.Permission?.Code, permissionCode, StringComparison.OrdinalIgnoreCase));
    }
}
