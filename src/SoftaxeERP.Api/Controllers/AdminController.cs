using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;

namespace SoftaxeERP.Api.Controllers;

[ApiController, Authorize, Route("api/admin")]
public class AdminController(IAdminService service) : ControllerBase
{
    [HttpGet("company")]
    [HttpGet("companies")]
    public async Task<IActionResult> Company(CancellationToken ct) =>
        (await service.GetCompanyAsync(ct)) is { } x ? Ok(x) : NotFound();

    [HttpPut("company")]
    [HttpPut("companies")]
    public async Task<IActionResult> UpdateCompany(CompanyUpdateDto dto, CancellationToken ct) =>
        (await service.UpdateCompanyAsync(dto, ct)) is { } x ? Ok(x) : NotFound();

    [HttpGet("branches")]
    public async Task<IActionResult> Branches(CancellationToken ct) => Ok(await service.GetBranchesAsync(ct));

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch(BranchUpsertDto dto, CancellationToken ct) =>
        Ok(await service.CreateBranchAsync(dto, ct));

    [HttpPut("branches/{id:guid}")]
    public async Task<IActionResult> UpdateBranch(Guid id, BranchUpsertDto dto, CancellationToken ct) =>
        (await service.UpdateBranchAsync(id, dto, ct)) is { } x ? Ok(x) : NotFound();

    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken ct) => Ok(await service.GetRolesAsync(ct));

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole(RoleCreateDto dto, CancellationToken ct) =>
        Ok(await service.CreateRoleAsync(dto, ct));

    [HttpGet("permissions")]
    public async Task<IActionResult> Permissions(CancellationToken ct) => Ok(await service.GetPermissionsAsync(ct));

    [HttpGet("roles/{id:guid}/permissions")]
    public async Task<IActionResult> RolePermissions(Guid id, CancellationToken ct) =>
        (await service.GetRolePermissionsAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPut("roles/{id:guid}/permissions")]
    public async Task<IActionResult> SetRolePermissions(Guid id, RolePermissionsDto dto, CancellationToken ct) =>
        (await service.SetRolePermissionsAsync(id, dto.PermissionCodes, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPut("users/{id:guid}/roles")]
    public async Task<IActionResult> SetUserRoles(Guid id, UserRolesDto dto, CancellationToken ct) =>
        await service.SetUserRolesAsync(id, dto.RoleIds, ct) ? NoContent() : NotFound();

    [HttpGet("financial-years")]
    public async Task<IActionResult> FinancialYears(CancellationToken ct) => Ok(await service.GetFinancialYearsAsync(ct));

    [HttpPost("financial-years")]
    public async Task<IActionResult> CreateFinancialYear(FinancialYearCreateDto dto, CancellationToken ct) =>
        Ok(await service.CreateFinancialYearAsync(dto, ct));

    [HttpPost("financial-years/{id:guid}/set-current")]
    [HttpPut("financial-years/{id:guid}/set-current")]
    public async Task<IActionResult> SetCurrent(Guid id, CancellationToken ct) =>
        (await service.SetCurrentFinancialYearAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpGet("financial-years/{id:guid}/fiscal-periods")]
    public async Task<IActionResult> FiscalPeriods(Guid id, CancellationToken ct) =>
        Ok(await service.GetFiscalPeriodsAsync(id, ct));

    [HttpPost("financial-years/{id:guid}/fiscal-periods")]
    public async Task<IActionResult> CreateFiscalPeriod(Guid id, FiscalPeriodCreateDto dto, CancellationToken ct) =>
        Ok(await service.CreateFiscalPeriodAsync(id, dto, ct));

    [HttpGet("audit-logs")]
    public async Task<IActionResult> Audit(
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] int take = 100,
        CancellationToken ct = default) =>
        Ok(await service.GetAuditLogsAsync(entityName, entityId, take, ct));

    [HttpGet("document-types")]
    public async Task<IActionResult> DocumentTypes(CancellationToken ct) => Ok(await service.GetDocumentTypesAsync(ct));

    [HttpPut("document-types/{id:guid}")]
    public async Task<IActionResult> UpdateDocumentType(Guid id, DocumentTypeUpsertDto dto, CancellationToken ct) =>
        (await service.UpdateDocumentTypeAsync(id, dto, ct)) is { } x ? Ok(x) : NotFound();
}
