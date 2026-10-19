using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/operations")]
public class OperationsController(IOperationsService svc) : ControllerBase
{
    [HttpGet("vehicle-types")]
    public async Task<ActionResult<IReadOnlyList<VehicleTypeDto>>> VehicleTypes(CancellationToken ct) =>
        Ok(await svc.GetVehicleTypesAsync(ct));

    [HttpPost("vehicle-types")]
    public async Task<ActionResult<VehicleTypeDto>> CreateVehicleType([FromBody] VehicleTypeDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateVehicleTypeAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("vehicles")]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> Vehicles(CancellationToken ct) =>
        Ok(await svc.GetVehiclesAsync(ct));

    [HttpPost("vehicles")]
    public async Task<ActionResult<VehicleDto>> CreateVehicle([FromBody] VehicleDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateVehicleAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("drivers")]
    public async Task<ActionResult<IReadOnlyList<DriverDto>>> Drivers(CancellationToken ct) =>
        Ok(await svc.GetDriversAsync(ct));

    [HttpPost("drivers")]
    public async Task<ActionResult<DriverDto>> CreateDriver([FromBody] DriverDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateDriverAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("gate-passes")]
    public async Task<ActionResult<IReadOnlyList<GatePassDto>>> GatePasses(CancellationToken ct) =>
        Ok(await svc.GetGatePassesAsync(ct));

    [HttpPost("gate-passes")]
    public async Task<ActionResult<GatePassDto>> CreateGatePass([FromBody] GatePassDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateGatePassAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("gate-passes/{id:guid}/{actionName:regex(^(submit|approve|post|cancel)$)}")]
    public async Task<IActionResult> TransitionGatePass(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var result = await svc.TransitionGatePassAsync(id, actionName, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/weighbridge")]
public class WeighbridgeController(IWeighbridgeService svc) : ControllerBase
{
    [HttpGet("scales")]
    public async Task<ActionResult<IReadOnlyList<WeighbridgeScaleDto>>> Scales(CancellationToken ct) =>
        Ok(await svc.GetScalesAsync(ct));

    [HttpGet("weighments")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WeighbridgeTicketDto>>> GetAll(CancellationToken ct) =>
        Ok(await svc.GetAllAsync(ct));

    [HttpGet("weighments/{id:guid}")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WeighbridgeTicketDto>> GetById(Guid id, CancellationToken ct)
    {
        var row = await svc.GetByIdAsync(id, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost("weighments")]
    [HttpPost]
    public async Task<ActionResult<WeighbridgeTicketDto>> Create([FromBody] WeighbridgeTicketCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("weighments/{id:guid}/first")]
    [HttpPost("{id:guid}/first")]
    public async Task<IActionResult> First(Guid id, [FromBody] WeighbridgeFirstWeightDto dto, CancellationToken ct)
    {
        try
        {
            var row = await svc.RecordFirstWeightAsync(id, dto, ct);
            return row is null ? NotFound() : Ok(row);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("weighments/{id:guid}/complete")]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] WeighbridgeCompleteDto dto, CancellationToken ct)
    {
        try
        {
            var row = await svc.CompleteAsync(id, dto, ct);
            return row is null ? NotFound() : Ok(row);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/pos")]
public class PosController(IPosService svc) : ControllerBase
{
    [HttpGet("terminals")]
    public async Task<ActionResult<IReadOnlyList<PosTerminalDto>>> Terminals(CancellationToken ct) =>
        Ok(await svc.GetTerminalsAsync(ct));

    [HttpPost("terminals")]
    public async Task<ActionResult<PosTerminalDto>> CreateTerminal([FromBody] PosTerminalDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateTerminalAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("shifts")]
    public async Task<ActionResult<IReadOnlyList<PosShiftDto>>> Shifts(CancellationToken ct) =>
        Ok(await svc.GetShiftsAsync(ct));

    [HttpPost("shifts/open")]
    public async Task<ActionResult<PosShiftDto>> OpenShift([FromBody] PosShiftOpenDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.OpenShiftAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("shifts/{id:guid}/reconciliation")]
    public async Task<ActionResult<PosShiftReconciliationDto>> ShiftReconciliation(Guid id, CancellationToken ct)
    {
        var row = await svc.GetShiftReconciliationAsync(id, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost("shifts/{id:guid}/close")]
    public async Task<IActionResult> CloseShift(Guid id, [FromBody] PosShiftCloseDto dto, CancellationToken ct)
    {
        try
        {
            var row = await svc.CloseShiftAsync(id, dto, ct);
            return row is null ? NotFound() : Ok(row);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("sales")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PosSaleDto>>> Sales(CancellationToken ct) =>
        Ok(await svc.GetAllAsync(ct));

    [HttpGet("sales/{id:guid}")]
    public async Task<ActionResult<PosSaleDto>> SaleById(Guid id, CancellationToken ct)
    {
        var row = await svc.GetSaleByIdAsync(id, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost("sales")]
    [HttpPost]
    public async Task<ActionResult<PosSaleDto>> CreateSale([FromBody] PosSaleCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("sales/{id:guid}/returns")]
    public async Task<ActionResult<PosSaleDto>> ReturnSale(Guid id, [FromBody] PosSaleReturnCreateDto? dto, CancellationToken ct)
    {
        try
        {
            var body = dto ?? new PosSaleReturnCreateDto(id, null, null);
            if (body.PosSaleId == Guid.Empty) body = body with { PosSaleId = id };
            return Ok(await svc.CreateReturnAsync(body, ct));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("reports/daily-sales")]
    public async Task<ActionResult<PosDailySalesReportDto>> DailySales([FromQuery] DateTime? date, CancellationToken ct)
    {
        try { return Ok(await svc.GetDailySalesReportAsync(date, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("reports/owner-profit")]
    public async Task<ActionResult<PosOwnerProfitReportDto>> OwnerProfit([FromQuery] DateTime? date, CancellationToken ct)
    {
        try { return Ok(await svc.GetOwnerProfitReportAsync(date, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
