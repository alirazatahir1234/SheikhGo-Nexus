using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Api.Controllers;

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
    private readonly IPermissionService _permissions;

    public ReportsController(IReportService svc, IPermissionService permissions)
    {
        _svc = svc;
        _permissions = permissions;
    }

    private async Task EnsureFinanceAsync(CancellationToken ct)
    {
        if (await _permissions.HasAsync("Reports.Finance.View", ct)) return;
        if (await _permissions.HasAsync("Finance.Reports.View", ct)) return;
        await _permissions.EnsureAsync("Reports.Finance.View", ct);
    }

    [HttpGet("ledger")]
    public async Task<ActionResult<LedgerReportDto>> Ledger(
        [FromQuery] Guid accountId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        try
        {
            await EnsureFinanceAsync(ct);
            return Ok(await _svc.GetLedgerAsync(accountId, from, to, ct));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceReportDto>> TrialBalance([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        try
        {
            await EnsureFinanceAsync(ct);
            return Ok(await _svc.GetTrialBalanceAsync(asOf ?? DateTime.UtcNow.Date, ct));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("aging/receivables")]
    public async Task<ActionResult<AgingReportDto>> ReceivablesAging([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        try
        {
            await EnsureFinanceAsync(ct);
            return Ok(await _svc.GetReceivablesAgingAsync(asOf, ct));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("aging/payables")]
    public async Task<ActionResult<AgingReportDto>> PayablesAging([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        try
        {
            await EnsureFinanceAsync(ct);
            return Ok(await _svc.GetPayablesAgingAsync(asOf, ct));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("profit-loss")]
    public async Task<ActionResult<ProfitLossReportDto>> ProfitLoss(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        try
        {
            await EnsureFinanceAsync(ct);
            var f = from ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
            var t = to ?? DateTime.UtcNow.Date;
            return Ok(await _svc.GetProfitLossAsync(f, t, ct));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<BalanceSheetReportDto>> BalanceSheet([FromQuery] DateTime? asOf, CancellationToken ct)
    {
        try
        {
            await EnsureFinanceAsync(ct);
            return Ok(await _svc.GetBalanceSheetAsync(asOf ?? DateTime.UtcNow.Date, ct));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }
}
