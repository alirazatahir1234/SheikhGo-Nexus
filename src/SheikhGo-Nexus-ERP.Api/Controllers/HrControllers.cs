using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/hr")]
public class HrOrgController(IHrOrgService org) : ControllerBase
{
    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> Departments(CancellationToken ct) =>
        Ok(await org.GetDepartmentsAsync(ct));

    [HttpPost("departments")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment([FromBody] DepartmentUpsertDto dto, CancellationToken ct) =>
        Ok(await org.CreateDepartmentAsync(dto, ct));

    [HttpGet("designations")]
    public async Task<ActionResult<IReadOnlyList<DesignationDto>>> Designations(CancellationToken ct) =>
        Ok(await org.GetDesignationsAsync(ct));

    [HttpPost("designations")]
    public async Task<ActionResult<DesignationDto>> CreateDesignation([FromBody] DesignationUpsertDto dto, CancellationToken ct) =>
        Ok(await org.CreateDesignationAsync(dto, ct));

    [HttpGet("components")]
    public async Task<ActionResult<IReadOnlyList<PayrollComponentDto>>> Components(CancellationToken ct) =>
        Ok(await org.GetComponentsAsync(ct));
}

[ApiController]
[Authorize]
[Route("api/payroll")]
public class PayrollController(IPayrollService payroll) : ControllerBase
{
    [HttpGet("periods")]
    public async Task<ActionResult<IReadOnlyList<PayrollPeriodDto>>> Periods(CancellationToken ct) =>
        Ok(await payroll.GetPeriodsAsync(ct));

    [HttpPost("periods")]
    public async Task<ActionResult<PayrollPeriodDto>> CreatePeriod([FromBody] PayrollPeriodCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await payroll.CreatePeriodAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("periods/{id:guid}/close")]
    public async Task<ActionResult<PayrollPeriodDto>> ClosePeriod(Guid id, CancellationToken ct)
    {
        try
        {
            var p = await payroll.ClosePeriodAsync(id, ct);
            return p is null ? NotFound() : Ok(p);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyList<PayrollRunDto>>> Runs(CancellationToken ct) =>
        Ok(await payroll.GetRunsAsync(ct));

    [HttpGet("runs/{id:guid}")]
    public async Task<ActionResult<PayrollRunDto>> GetRun(Guid id, CancellationToken ct)
    {
        var r = await payroll.GetRunByIdAsync(id, ct);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost("runs")]
    public async Task<ActionResult<PayrollRunDto>> CreateRun([FromBody] PayrollRunCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await payroll.CreateRunAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("runs/{id:guid}/calculate")]
    public async Task<ActionResult<PayrollRunDto>> Calculate(Guid id, CancellationToken ct)
    {
        try
        {
            var r = await payroll.CalculateAsync(id, ct);
            return r is null ? NotFound() : Ok(r);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("runs/{id:guid}/{actionName}")]
    public async Task<ActionResult<PayrollRunDto>> Transition(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var r = await payroll.TransitionAsync(id, actionName, ct);
            return r is null ? NotFound() : Ok(r);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("payments")]
    public async Task<ActionResult<IReadOnlyList<SalaryPaymentDto>>> Payments([FromQuery] Guid? runId, CancellationToken ct) =>
        Ok(await payroll.GetPaymentsAsync(runId, ct));

    [HttpPost("payments")]
    public async Task<ActionResult<SalaryPaymentDto>> CreatePayment([FromBody] SalaryPaymentCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await payroll.CreatePaymentAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/finance/gl-mappings")]
public class GlMappingsController(IGlMappingService mappings) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GlAccountMappingDto>>> GetAll(CancellationToken ct) =>
        Ok(await mappings.GetAllAsync(ct));

    [HttpPut]
    public async Task<ActionResult<GlAccountMappingDto>> Upsert([FromBody] GlAccountMappingUpsertDto dto, CancellationToken ct)
    {
        try { return Ok(await mappings.UpsertAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
