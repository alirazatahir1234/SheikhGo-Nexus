using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Infrastructure.Persistence;

namespace SoftaxeERP.Infrastructure.Services;

public class AuditService(AppDbContext db, ITenantContext tenant) : IAuditService
{
    public async Task WriteAsync(string entityName, Guid entityId, string action, string? oldValues = null, string? newValues = null, CancellationToken ct = default)
    {
        Guid? companyId = await db.Companies.Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
        db.AuditLogs.Add(new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId.ToString(),
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = tenant.UserId,
            CompanyId = companyId,
            IpAddress = tenant.ClientIp,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}

public class AdminService(AppDbContext db) : IAdminService
{
    public async Task<CompanyDto?> GetCompanyAsync(CancellationToken ct = default) => (await db.Companies.FirstOrDefaultAsync(ct)) is { } c ? Map(c) : null;
    public async Task<CompanyDto?> UpdateCompanyAsync(CompanyUpdateDto dto, CancellationToken ct = default)
    {
        var c = await db.Companies.FirstOrDefaultAsync(ct); if (c is null) return null;
        c.Name = dto.Name; c.ShortName = dto.ShortName; c.Address = dto.Address; c.City = dto.City; c.Country = dto.Country; c.Phone = dto.Phone; c.Email = dto.Email; c.Currency = dto.Currency; c.Ntn = dto.Ntn; c.Industry = dto.Industry; c.DefaultSaleTaxPercent = dto.DefaultSaleTaxPercent; c.DefaultPurchaseTaxPercent = dto.DefaultPurchaseTaxPercent;
        await db.SaveChangesAsync(ct); return Map(c);
    }
    public Task<IReadOnlyList<BranchDto>> GetBranchesAsync(CancellationToken ct = default) => db.Branches.OrderBy(x => x.Code).Select(x => new BranchDto(x.Id, x.CompanyId, x.Code, x.Name, x.IsDefault)).ToListAsync(ct).ContinueWith(x => (IReadOnlyList<BranchDto>)x.Result, ct);
    public async Task<BranchDto> CreateBranchAsync(BranchUpsertDto dto, CancellationToken ct = default)
    {
        var company = await db.Companies.FirstAsync(ct); var entity = new Branch { CompanyId = company.Id, Code = dto.Code, Name = dto.Name, IsDefault = dto.IsDefault }; db.Branches.Add(entity); await db.SaveChangesAsync(ct); return new(entity.Id, entity.CompanyId, entity.Code, entity.Name, entity.IsDefault);
    }
    public async Task<BranchDto?> UpdateBranchAsync(Guid id, BranchUpsertDto dto, CancellationToken ct = default)
    {
        var entity = await db.Branches.FirstOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return null; entity.Code = dto.Code; entity.Name = dto.Name; entity.IsDefault = dto.IsDefault; await db.SaveChangesAsync(ct); return new(entity.Id, entity.CompanyId, entity.Code, entity.Name, entity.IsDefault);
    }
    public Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default) => db.Roles.OrderBy(x => x.Code).Select(x => new RoleDto(x.Id, x.Code, x.Name)).ToListAsync(ct).ContinueWith(x => (IReadOnlyList<RoleDto>)x.Result, ct);
    public async Task<RoleDto> CreateRoleAsync(RoleCreateDto dto, CancellationToken ct = default) { var r = new Role { Code = dto.Code, Name = dto.Name }; db.Roles.Add(r); await db.SaveChangesAsync(ct); return new(r.Id, r.Code, r.Name); }
    public Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default) => db.Permissions.OrderBy(x => x.Code).Select(x => new PermissionDto(x.Id, x.Code, x.Name, x.Module)).ToListAsync(ct).ContinueWith(x => (IReadOnlyList<PermissionDto>)x.Result, ct);
    public async Task<RolePermissionsDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default) => await db.Roles.Where(x => x.Id == roleId).Select(x => new RolePermissionsDto(x.Id, x.RolePermissions.Select(p => p.Permission!.Code).ToList())).FirstOrDefaultAsync(ct);
    public async Task<RolePermissionsDto?> SetRolePermissionsAsync(Guid roleId, IReadOnlyList<string> codes, CancellationToken ct = default)
    {
        var role = await db.Roles.Include(x => x.RolePermissions).FirstOrDefaultAsync(x => x.Id == roleId, ct); if (role is null) return null;
        var permissions = await db.Permissions.Where(x => codes.Contains(x.Code)).ToListAsync(ct); db.RolePermissions.RemoveRange(role.RolePermissions); db.RolePermissions.AddRange(permissions.Select(x => new RolePermission { RoleId = roleId, PermissionId = x.Id })); await db.SaveChangesAsync(ct); return new(roleId, permissions.Select(x => x.Code).ToList());
    }
    public async Task<bool> SetUserRolesAsync(Guid userId, IReadOnlyList<Guid> roleIds, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(x => x.Id == userId, ct)) return false; var roles = await db.Roles.Where(x => roleIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct); if (roles.Count != roleIds.Distinct().Count()) throw new InvalidOperationException("One or more roles do not exist.");
        db.UserRoles.RemoveRange(db.UserRoles.Where(x => x.UserId == userId)); db.UserRoles.AddRange(roles.Select(x => new UserRole { UserId = userId, RoleId = x })); await db.SaveChangesAsync(ct); return true;
    }
    public Task<IReadOnlyList<FinancialYearDto>> GetFinancialYearsAsync(CancellationToken ct = default) => db.FinancialYears.OrderByDescending(x => x.StartDate).Select(x => new FinancialYearDto(x.Id, x.CompanyId, x.Code, x.StartDate, x.EndDate, x.IsCurrent, x.IsClosed)).ToListAsync(ct).ContinueWith(x => (IReadOnlyList<FinancialYearDto>)x.Result, ct);
    public async Task<FinancialYearDto> CreateFinancialYearAsync(FinancialYearCreateDto dto, CancellationToken ct = default) { var c = await db.Companies.FirstAsync(ct); var fy = new FinancialYear { CompanyId = c.Id, Code = dto.Code, StartDate = dto.StartDate.Date, EndDate = dto.EndDate.Date }; db.FinancialYears.Add(fy); await db.SaveChangesAsync(ct); return new(fy.Id, fy.CompanyId, fy.Code, fy.StartDate, fy.EndDate, fy.IsCurrent, fy.IsClosed); }
    public async Task<FinancialYearDto?> SetCurrentFinancialYearAsync(Guid id, CancellationToken ct = default) { var fy = await db.FinancialYears.FirstOrDefaultAsync(x => x.Id == id, ct); if (fy is null) return null; await db.FinancialYears.ExecuteUpdateAsync(x => x.SetProperty(y => y.IsCurrent, false), ct); fy.IsCurrent = true; await db.SaveChangesAsync(ct); return new(fy.Id, fy.CompanyId, fy.Code, fy.StartDate, fy.EndDate, true, fy.IsClosed); }
    public Task<IReadOnlyList<FiscalPeriodDto>> GetFiscalPeriodsAsync(Guid yearId, CancellationToken ct = default) => db.FiscalPeriods.Where(x => x.FinancialYearId == yearId).OrderBy(x => x.PeriodNumber).Select(x => new FiscalPeriodDto(x.Id, x.FinancialYearId, x.Name, x.PeriodNumber, x.StartDate, x.EndDate, x.IsClosed)).ToListAsync(ct).ContinueWith(x => (IReadOnlyList<FiscalPeriodDto>)x.Result, ct);
    public async Task<FiscalPeriodDto> CreateFiscalPeriodAsync(Guid yearId, FiscalPeriodCreateDto dto, CancellationToken ct = default) { if (!await db.FinancialYears.AnyAsync(x => x.Id == yearId, ct)) throw new InvalidOperationException("Financial year not found."); var p = new FiscalPeriod { FinancialYearId = yearId, Name = dto.Name, PeriodNumber = dto.PeriodNumber, StartDate = dto.StartDate.Date, EndDate = dto.EndDate.Date }; db.FiscalPeriods.Add(p); await db.SaveChangesAsync(ct); return new(p.Id, p.FinancialYearId, p.Name, p.PeriodNumber, p.StartDate, p.EndDate, p.IsClosed); }
    public Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(string? name, string? id, int take, CancellationToken ct = default) { var q = db.AuditLogs.AsQueryable(); if (!string.IsNullOrWhiteSpace(name)) q = q.Where(x => x.EntityName == name); if (!string.IsNullOrWhiteSpace(id)) q = q.Where(x => x.EntityId == id); return q.OrderByDescending(x => x.Timestamp).Take(Math.Clamp(take, 1, 500)).Select(x => new AuditLogDto(x.Id, x.EntityName, x.EntityId, x.Action, x.OldValues, x.NewValues, x.UserId, x.Timestamp, x.IpAddress)).ToListAsync(ct).ContinueWith(x => (IReadOnlyList<AuditLogDto>)x.Result, ct); }
    public Task<IReadOnlyList<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken ct = default) => db.DocumentTypes.OrderBy(x => x.Code).Select(x => Map(x)).ToListAsync(ct).ContinueWith(x => (IReadOnlyList<DocumentTypeDto>)x.Result, ct);
    public async Task<DocumentTypeDto?> UpdateDocumentTypeAsync(Guid id, DocumentTypeUpsertDto dto, CancellationToken ct = default) { var d = await db.DocumentTypes.FirstOrDefaultAsync(x => x.Id == id, ct); if (d is null) return null; d.Code = dto.Code; d.Name = dto.Name; d.RequiresVerification = dto.RequiresVerification; d.RequiresApproval = dto.RequiresApproval; d.Prefix = dto.Prefix; d.AffectsInventory = dto.AffectsInventory; d.AffectsGl = dto.AffectsGl; await db.SaveChangesAsync(ct); return Map(d); }
    private static CompanyDto Map(Company x) => new(x.Id, x.Name, x.ShortName, x.Address, x.City, x.Country, x.Phone, x.Email, x.Currency, x.Ntn, x.Industry, x.DefaultSaleTaxPercent, x.DefaultPurchaseTaxPercent);
    private static DocumentTypeDto Map(DocumentType x) => new(x.Id, x.Code, x.Name, x.RequiresVerification, x.RequiresApproval, x.Prefix, x.AffectsInventory, x.AffectsGl);
}
