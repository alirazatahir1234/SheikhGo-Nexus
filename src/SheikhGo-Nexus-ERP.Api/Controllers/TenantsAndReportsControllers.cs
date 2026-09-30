using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;

namespace SoftaxeERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly ITenantPlatformService _svc;
    public TenantsController(ITenantPlatformService svc) => _svc = svc;

    private bool IsPlatformAdmin =>
        User.FindFirst("is_platform_admin")?.Value == "true"
        || string.Equals(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value, "PlatformAdmin", StringComparison.OrdinalIgnoreCase);

    [HttpGet("modules/catalog")]
    public ActionResult<IReadOnlyList<string>> ModuleCatalog() => Ok(_svc.GetModuleCatalog());

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantListDto>>> GetAll(CancellationToken ct)
    {
        if (!IsPlatformAdmin) return Forbid();
        return Ok(await _svc.GetAllAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDetailDto>> Get(Guid id, CancellationToken ct)
    {
        if (!IsPlatformAdmin) return Forbid();
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("provision")]
    public async Task<ActionResult<ProvisionTenantResult>> Provision([FromBody] ProvisionTenantRequest request, CancellationToken ct)
    {
        if (!IsPlatformAdmin) return Forbid();
        try { return Ok(await _svc.ProvisionAsync(request, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}/modules")]
    public async Task<ActionResult<TenantDetailDto>> UpdateModules(Guid id, [FromBody] string[] modules, CancellationToken ct)
    {
        if (!IsPlatformAdmin) return Forbid();
        var result = await _svc.UpdateModulesAsync(id, modules, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] bool isActive, CancellationToken ct)
    {
        if (!IsPlatformAdmin) return Forbid();
        return await _svc.SetActiveAsync(id, isActive, ct) ? NoContent() : NotFound();
    }
}

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _svc;
    public ReportsController(IReportService svc) => _svc = svc;

    [HttpGet("ledger")]
    public async Task<ActionResult<LedgerReportDto>> Ledger(
        [FromQuery] Guid accountId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        try { return Ok(await _svc.GetLedgerAsync(accountId, from, to, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceReportDto>> TrialBalance([FromQuery] DateTime? asOf, CancellationToken ct) =>
        Ok(await _svc.GetTrialBalanceAsync(asOf ?? DateTime.UtcNow.Date, ct));

    [HttpGet("aging/receivables")]
    public async Task<ActionResult<AgingReportDto>> ReceivablesAging([FromQuery] DateTime? asOf, CancellationToken ct) =>
        Ok(await _svc.GetReceivablesAgingAsync(asOf, ct));

    [HttpGet("aging/payables")]
    public async Task<ActionResult<AgingReportDto>> PayablesAging([FromQuery] DateTime? asOf, CancellationToken ct) =>
        Ok(await _svc.GetPayablesAgingAsync(asOf, ct));
}
