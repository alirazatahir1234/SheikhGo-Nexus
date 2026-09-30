using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/production")]
public class ProductionController : ControllerBase
{
    private readonly IProductionService _svc;
    public ProductionController(IProductionService svc) => _svc = svc;

    [HttpGet("boms")]
    public async Task<ActionResult<IReadOnlyList<BomDto>>> Boms(CancellationToken ct) =>
        Ok(await _svc.GetBomsAsync(ct));

    [HttpGet("boms/{id:guid}")]
    public async Task<ActionResult<BomDto>> BomById(Guid id, CancellationToken ct)
    {
        var bom = await _svc.GetBomByIdAsync(id, ct);
        return bom is null ? NotFound() : Ok(bom);
    }

    [HttpPost("boms")]
    public async Task<ActionResult<BomDto>> CreateBom([FromBody] BomUpsertDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateBomAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<ProductionOrderDto>>> Orders(CancellationToken ct) =>
        Ok(await _svc.GetOrdersAsync(ct));

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<ProductionOrderDto>> OrderById(Guid id, CancellationToken ct)
    {
        var order = await _svc.GetOrderByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("orders")]
    public async Task<ActionResult<ProductionOrderDto>> CreateOrder([FromBody] ProductionOrderCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateOrderAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("orders/{id:guid}/{actionName}")]
    public async Task<ActionResult<ProductionOrderDto>> Transition(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var result = await _svc.TransitionAsync(id, actionName, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("orders/{id:guid}/variances")]
    public async Task<ActionResult<ProductionOrderDto>> AddVariance(Guid id, [FromBody] ProductionVarianceCreateDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _svc.AddVarianceAsync(id, dto, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("costing/{orderId:guid}")]
    public async Task<ActionResult<ProductionCostingDto>> Costing(Guid orderId, CancellationToken ct)
    {
        var result = await _svc.GetCostingAsync(orderId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _svc;
    public UsersController(IUserAdminService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetUsersAsync(ct));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] UserCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UserUpdateDto dto, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }
}

[ApiController]
[Authorize]
[Route("api/inventory")]
public class InventoryAdjustmentsController : ControllerBase
{
    private readonly IStockAdjustmentService _svc;
    public InventoryAdjustmentsController(IStockAdjustmentService svc) => _svc = svc;

    [HttpGet("adjustments")]
    public async Task<ActionResult<IReadOnlyList<StockAdjustmentDocDto>>> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    [HttpGet("adjustments/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        (await _svc.GetByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("adjustments")]
    public async Task<ActionResult<StockAdjustmentDocDto>> Create([FromBody] StockAdjustmentDocCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("adjustments/{id:guid}/{actionName:regex(^(submit|approve|post|cancel)$)}")]
    public async Task<IActionResult> Transition(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var result = await _svc.TransitionAsync(id, actionName, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}

[ApiController]
[Authorize]
[Route("api/inventory/stock-counts")]
public class StockCountsController : ControllerBase
{
    private readonly IStockCountService _svc;
    public StockCountsController(IStockCountService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockCountDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        (await _svc.GetByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost]
    public async Task<ActionResult<StockCountDto>> Create([FromBody] StockCountCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/snapshot")]
    public async Task<IActionResult> Snapshot(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _svc.SnapshotAsync(id, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/{actionName:regex(^(submit|approve|post|cancel)$)}")]
    public async Task<IActionResult> Transition(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var result = await _svc.TransitionAsync(id, actionName, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _svc;
    private readonly IWarehouseLocationService _locations;
    public WarehousesController(IWarehouseService svc, IWarehouseLocationService locations)
    {
        _svc = svc;
        _locations = locations;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<WarehouseDto>> Create([FromBody] WarehouseUpsertDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateAsync(dto, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> Update(Guid id, [FromBody] WarehouseUpsertDto dto, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/locations")]
    public async Task<ActionResult<IReadOnlyList<WarehouseLocationDto>>> Locations(Guid id, CancellationToken ct) =>
        Ok(await _locations.GetByWarehouseAsync(id, ct));

    [HttpPost("locations")]
    public async Task<ActionResult<WarehouseLocationDto>> CreateLocation([FromBody] WarehouseLocationUpsertDto dto, CancellationToken ct) =>
        Ok(await _locations.CreateAsync(dto, ct));
}

[ApiController]
[Authorize]
[Route("api/settlements")]
public class SettlementsController : ControllerBase
{
    private readonly IPartySettlementService _svc;
    public SettlementsController(IPartySettlementService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartySettlementDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<PartySettlementDto>> Create([FromBody] PartySettlementCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/contracts")]
public class PurchaseContractsController : ControllerBase
{
    private readonly IPurchaseContractService _svc;
    public PurchaseContractsController(IPurchaseContractService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseContractDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<PurchaseContractDto>> Create([FromBody] PurchaseContractCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct) =>
        await _svc.CloseAsync(id, ct) ? NoContent() : NotFound();
}

[ApiController]
[Authorize]
[Route("api/cost-centres")]
public class CostCentresController : ControllerBase
{
    private readonly ICostCentreService _svc;
    public CostCentresController(ICostCentreService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CostCentreDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<CostCentreDto>> Create([FromBody] CostCentreUpsertDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateAsync(dto, ct));
}

[ApiController]
[Authorize]
[Route("api/cheques")]
public class ChequesController : ControllerBase
{
    private readonly IChequeService _svc;
    public ChequesController(IChequeService svc) => _svc = svc;

    [HttpGet("books")]
    public async Task<ActionResult<IReadOnlyList<ChequeBookDto>>> Books(CancellationToken ct) => Ok(await _svc.GetBooksAsync(ct));

    [HttpPost("books")]
    public async Task<ActionResult<ChequeBookDto>> CreateBook([FromBody] ChequeBookCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateBookAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("pdc")]
    public async Task<ActionResult<IReadOnlyList<PostDatedChequeDto>>> Cheques(CancellationToken ct) => Ok(await _svc.GetChequesAsync(ct));

    [HttpPost("pdc")]
    public async Task<ActionResult<PostDatedChequeDto>> CreateCheque([FromBody] PostDatedChequeCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateChequeAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("pdc/{id:guid}/status")]
    public async Task<ActionResult<PostDatedChequeDto>> Status(Guid id, [FromBody] PostDatedChequeStatusDto dto, CancellationToken ct)
    {
        var result = await _svc.UpdateStatusAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }
}

[ApiController]
[Authorize]
[Route("api/farm/shareholders")]
public class FarmShareHoldersController : ControllerBase
{
    private readonly IFarmShareHolderService _svc;
    public FarmShareHoldersController(IFarmShareHolderService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FarmShareHolderDto>>> GetAll([FromQuery] Guid? flockId, CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(flockId, ct));

    [HttpPost]
    public async Task<ActionResult<FarmShareHolderDto>> Create([FromBody] FarmShareHolderCreateDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateAsync(dto, ct));
}

[ApiController]
[Authorize]
[Route("api/salesforce")]
public class SalesForceController : ControllerBase
{
    private readonly ISalesForceService _svc;
    public SalesForceController(ISalesForceService svc) => _svc = svc;

    [HttpGet("areas")]
    public async Task<ActionResult<IReadOnlyList<SalesAreaDto>>> Areas(CancellationToken ct) => Ok(await _svc.GetAreasAsync(ct));

    [HttpPost("areas")]
    public async Task<ActionResult<SalesAreaDto>> CreateArea([FromBody] SalesAreaUpsertDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateAreaAsync(dto, ct));

    [HttpGet("salesmen")]
    public async Task<ActionResult<IReadOnlyList<SalesmanDto>>> Salesmen(CancellationToken ct) => Ok(await _svc.GetSalesmenAsync(ct));

    [HttpPost("salesmen")]
    public async Task<ActionResult<SalesmanDto>> CreateSalesman([FromBody] SalesmanUpsertDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateSalesmanAsync(dto, ct));

    [HttpGet("targets")]
    public async Task<ActionResult<IReadOnlyList<SalesTargetDto>>> Targets(CancellationToken ct) => Ok(await _svc.GetTargetsAsync(ct));

    [HttpPost("targets")]
    public async Task<ActionResult<SalesTargetDto>> CreateTarget([FromBody] SalesTargetUpsertDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateTargetAsync(dto, ct));
}

[ApiController]
[Authorize]
[Route("api/demands")]
public class DemandsController : ControllerBase
{
    private readonly IMaterialDemandService _svc;
    public DemandsController(IMaterialDemandService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaterialDemandDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<MaterialDemandDto>> Create([FromBody] MaterialDemandCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/hr-finance")]
public class HrFinanceController : ControllerBase
{
    private readonly IHrFinanceService _svc;
    public HrFinanceController(IHrFinanceService svc) => _svc = svc;

    [HttpGet("advances")]
    public async Task<ActionResult<IReadOnlyList<SalaryAdvanceDto>>> Advances(CancellationToken ct) => Ok(await _svc.GetAdvancesAsync(ct));

    [HttpPost("advances")]
    public async Task<ActionResult<SalaryAdvanceDto>> CreateAdvance([FromBody] SalaryAdvanceCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAdvanceAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("loans")]
    public async Task<ActionResult<IReadOnlyList<StaffLoanDto>>> Loans(CancellationToken ct) => Ok(await _svc.GetLoansAsync(ct));

    [HttpPost("loans")]
    public async Task<ActionResult<StaffLoanDto>> CreateLoan([FromBody] StaffLoanCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateLoanAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("eobi")]
    public async Task<ActionResult<IReadOnlyList<EobiDto>>> Eobi(CancellationToken ct) => Ok(await _svc.GetEobiAsync(ct));

    [HttpPost("eobi")]
    public async Task<ActionResult<EobiDto>> CreateEobi([FromBody] EobiCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateEobiAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _svc;
    public BrandsController(IBrandService svc) => _svc = svc;
    [HttpGet] public async Task<ActionResult<IReadOnlyList<BrandDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));
    [HttpPost] public async Task<ActionResult<BrandDto>> Create([FromBody] BrandUpsertDto dto, CancellationToken ct) => Ok(await _svc.CreateAsync(dto, ct));
}

[ApiController]
[Authorize]
[Route("api/conversions")]
public class ConversionsController : ControllerBase
{
    private readonly IUnitConversionService _svc;
    public ConversionsController(IUnitConversionService svc) => _svc = svc;
    [HttpGet] public async Task<ActionResult<IReadOnlyList<UnitConversionDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));
    [HttpPost]
    public async Task<ActionResult<UnitConversionDto>> Create([FromBody] UnitConversionUpsertDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/budgets")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _svc;
    public BudgetsController(IBudgetService svc) => _svc = svc;
    [HttpGet] public async Task<ActionResult<IReadOnlyList<BudgetDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));
    [HttpPost]
    public async Task<ActionResult<BudgetDto>> Create([FromBody] BudgetCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct) =>
        await _svc.ApproveAsync(id, ct) ? NoContent() : NotFound();
}

[ApiController]
[Authorize]
[Route("api/attendance")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _svc;
    public AttendanceController(IAttendanceService svc) => _svc = svc;
    [HttpGet] public async Task<ActionResult<IReadOnlyList<AttendanceDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAttendanceAsync(ct));
    [HttpGet("summary")]
    public async Task<ActionResult<IReadOnlyList<AttendanceSummaryDto>>> Summary([FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        Ok(await _svc.GetSummaryAsync(year, month, ct));
    [HttpPost]
    public async Task<ActionResult<AttendanceDto>> Create([FromBody] AttendanceCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAttendanceAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
    [HttpGet("overtime")]
    public async Task<ActionResult<IReadOnlyList<OvertimeDto>>> Overtime(CancellationToken ct) => Ok(await _svc.GetOvertimeAsync(ct));
    [HttpPost("overtime")]
    public async Task<ActionResult<OvertimeDto>> CreateOt([FromBody] OvertimeCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateOvertimeAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/day-close")]
public class DayCloseController : ControllerBase
{
    private readonly IDayCloseService _svc;
    public DayCloseController(IDayCloseService svc) => _svc = svc;
    [HttpGet] public async Task<ActionResult<IReadOnlyList<DayCloseDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));
    [HttpPost]
    public async Task<ActionResult<DayCloseDto>> Close([FromBody] DayCloseCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CloseAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/stock-transfers")]
public class StockTransfersController : ControllerBase
{
    private readonly IStockTransferService _svc;
    public StockTransfersController(IStockTransferService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockTransferDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        (await _svc.GetByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost]
    public async Task<ActionResult<StockTransferDto>> Create([FromBody] StockTransferCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/{actionName:regex(^(submit|approve|post|cancel)$)}")]
    public async Task<IActionResult> Transition(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var result = await _svc.TransitionAsync(id, actionName, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}

[ApiController]
[Authorize]
[Route("api/returns")]
public class ReturnsController : ControllerBase
{
    private readonly ITradeReturnService _svc;
    public ReturnsController(ITradeReturnService svc) => _svc = svc;

    [HttpGet("sale")]
    public async Task<ActionResult<IReadOnlyList<TradeReturnDto>>> SaleReturns(CancellationToken ct) => Ok(await _svc.GetSaleReturnsAsync(ct));

    [HttpPost("sale")]
    public async Task<ActionResult<TradeReturnDto>> CreateSale([FromBody] TradeReturnCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateSaleReturnAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("purchase")]
    public async Task<ActionResult<IReadOnlyList<TradeReturnDto>>> PurchaseReturns(CancellationToken ct) => Ok(await _svc.GetPurchaseReturnsAsync(ct));

    [HttpPost("purchase")]
    public async Task<ActionResult<TradeReturnDto>> CreatePurchase([FromBody] TradeReturnCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreatePurchaseReturnAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/leaves")]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _svc;
    public LeavesController(ILeaveService svc) => _svc = svc;

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<LeaveTypeDto>>> Types(CancellationToken ct) => Ok(await _svc.GetTypesAsync(ct));

    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceDto>>> Balances([FromQuery] Guid? employeeId, CancellationToken ct) =>
        Ok(await _svc.GetBalancesAsync(employeeId, ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaveDto>>> GetAll(CancellationToken ct) => Ok(await _svc.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveDto>> GetById(Guid id, CancellationToken ct)
    {
        var r = await _svc.GetByIdAsync(id, ct);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost]
    public async Task<ActionResult<LeaveDto>> Create([FromBody] LeaveCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/{actionName}")]
    public async Task<ActionResult<LeaveDto>> Transition(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var r = await _svc.TransitionAsync(id, actionName, ct);
            return r is null ? NotFound() : Ok(r);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/salary")]
public class SalaryController : ControllerBase
{
    private readonly ISalarySheetService _svc;
    public SalaryController(ISalarySheetService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<SalarySheetDto>> Get([FromQuery] int year, [FromQuery] int month, CancellationToken ct) =>
        Ok(await _svc.GetAsync(year, month, ct));

    [HttpPost("generate")]
    public async Task<ActionResult<SalarySheetDto>> Generate([FromBody] SalaryGenerateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.GenerateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<SalarySheetLineDto>> Pay(Guid id, CancellationToken ct)
    {
        var result = await _svc.MarkPaidAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
