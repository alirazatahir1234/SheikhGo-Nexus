using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;

namespace SoftaxeERP.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        if (result is null) return Unauthorized(new { message = "Invalid email or password" });
        return Ok(result);
    }
}

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc) => _svc = svc;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken ct) =>
        Ok(await _svc.GetSummaryAsync(ct));
}

[ApiController]
[Authorize]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _svc;
    public AccountsController(IAccountService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create([FromBody] AccountUpsertDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateAsync(dto, ct));
}

[ApiController]
[Authorize]
[Route("api/parties")]
public class PartiesController : ControllerBase
{
    private readonly IPartyService _svc;
    public PartiesController(IPartyService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartyDto>>> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(search, ct));

    [HttpPost]
    public async Task<ActionResult<PartyDto>> Create([FromBody] PartyUpsertDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateAsync(dto, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PartyDto>> Update(Guid id, [FromBody] PartyUpsertDto dto, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await _svc.DeleteAsync(id, ct) ? NoContent() : NotFound();
}

[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _svc;
    public ProductsController(IProductService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(search, ct));

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductUpsertDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateAsync(dto, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] ProductUpsertDto dto, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await _svc.DeleteAsync(id, ct) ? NoContent() : NotFound();
}

[ApiController]
[Authorize]
[Route("api/vouchers")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _svc;
    public VouchersController(IVoucherService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VoucherListDto>>> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VoucherDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<VoucherDto>> Create([FromBody] VoucherCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _svc;
    public SalesController(ISaleService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SaleInvoiceDto>>> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<SaleInvoiceDto>> Create([FromBody] SaleInvoiceCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Authorize]
[Route("api/purchases")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _svc;
    private readonly IDocumentWorkflowService _workflow;

    public PurchasesController(IPurchaseService svc, IDocumentWorkflowService workflow)
    {
        _svc = svc;
        _workflow = workflow;
    }

    // ── Purchase invoices ──────────────────────────────────────────────────
    [HttpGet]
    [HttpGet("invoices")]
    public async Task<ActionResult<IReadOnlyList<PurchaseInvoiceDto>>> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct) =>
        (await _svc.GetInvoiceByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost]
    [HttpPost("invoices")]
    public async Task<ActionResult<PurchaseInvoiceDto>> Create([FromBody] PurchaseInvoiceCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:guid}/{actionName:regex(^(submit|verify|approve|post|cancel)$)}")]
    [HttpPost("invoices/{id:guid}/{actionName:regex(^(submit|verify|approve|post|cancel)$)}")]
    public Task<IActionResult> InvoiceTransition(Guid id, string actionName, [FromBody] DocumentActionDto? body, CancellationToken ct) =>
        RunInvoice(id, actionName, body?.Remarks, ct);

    // ── Purchase orders ────────────────────────────────────────────────────
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> GetOrders(CancellationToken ct) =>
        Ok(await _svc.GetOrdersAsync(ct));

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct) =>
        (await _svc.GetOrderByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("orders")]
    public async Task<ActionResult<PurchaseOrderDto>> CreateOrder([FromBody] PurchaseOrderCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateOrderAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("orders/{id:guid}")]
    public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] PurchaseOrderCreateDto dto, CancellationToken ct)
    {
        try { return (await _svc.UpdateOrderAsync(id, dto, ct)) is { } x ? Ok(x) : NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("orders/{id:guid}/{actionName:regex(^(submit|verify|approve|post|cancel)$)}")]
    public Task<IActionResult> OrderTransition(Guid id, string actionName, [FromBody] DocumentActionDto? body, CancellationToken ct) =>
        RunOrder(id, actionName, body?.Remarks, ct);

    // ── Goods receipts ─────────────────────────────────────────────────────
    [HttpGet("receipts")]
    public async Task<ActionResult<IReadOnlyList<GoodsReceiptDto>>> GetReceipts(CancellationToken ct) =>
        Ok(await _svc.GetReceiptsAsync(ct));

    [HttpGet("receipts/{id:guid}")]
    public async Task<IActionResult> GetReceipt(Guid id, CancellationToken ct) =>
        (await _svc.GetReceiptByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("receipts")]
    public async Task<ActionResult<GoodsReceiptDto>> CreateReceipt([FromBody] GoodsReceiptCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateReceiptAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("receipts/{id:guid}/{actionName:regex(^(submit|verify|approve|post|cancel)$)}")]
    public Task<IActionResult> ReceiptTransition(Guid id, string actionName, [FromBody] DocumentActionDto? body, CancellationToken ct) =>
        RunReceipt(id, actionName, body?.Remarks, ct);

    // ── Audit / payables ───────────────────────────────────────────────────
    [HttpGet("orders/{id:guid}/audit")]
    public async Task<IActionResult> OrderAudit(Guid id, CancellationToken ct) =>
        Ok(await _svc.GetAuditAsync("PurchaseOrder", id, ct));

    [HttpGet("receipts/{id:guid}/audit")]
    public async Task<IActionResult> ReceiptAudit(Guid id, CancellationToken ct) =>
        Ok(await _svc.GetAuditAsync("GoodsReceipt", id, ct));

    [HttpGet("{id:guid}/audit")]
    [HttpGet("invoices/{id:guid}/audit")]
    public async Task<IActionResult> InvoiceAudit(Guid id, CancellationToken ct) =>
        Ok(await _svc.GetAuditAsync("PurchaseInvoice", id, ct));

    [HttpGet("payables/{partyId:guid}")]
    public async Task<ActionResult<PayableBalanceDto>> GetPayable(Guid partyId, CancellationToken ct)
    {
        var bal = await _svc.GetPayableAsync(partyId, ct);
        return bal is null ? NotFound() : Ok(bal);
    }

    [HttpGet("audit/{entityName}/{id:guid}")]
    public async Task<IActionResult> Audit(string entityName, Guid id, CancellationToken ct) =>
        Ok(await _svc.GetAuditAsync(entityName, id, ct));

    private async Task<IActionResult> RunOrder(Guid id, string action, string? remarks, CancellationToken ct)
    {
        try
        {
            var result = await _workflow.TransitionOrderAsync(id, action, remarks, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private async Task<IActionResult> RunReceipt(Guid id, string action, string? remarks, CancellationToken ct)
    {
        try
        {
            var result = await _workflow.TransitionReceiptAsync(id, action, remarks, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private async Task<IActionResult> RunInvoice(Guid id, string action, string? remarks, CancellationToken ct)
    {
        try
        {
            var result = await _workflow.TransitionInvoiceAsync(id, action, remarks, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}

[ApiController]
[Authorize]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _svc;
    public InventoryController(IInventoryService svc) => _svc = svc;

    [HttpGet("stock")]
    public async Task<ActionResult<IReadOnlyList<StockDto>>> Stock(CancellationToken ct) =>
        Ok(await _svc.GetStockAsync(ct));
}

[ApiController]
[Authorize]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _svc;
    public EmployeesController(IEmployeeService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] EmployeeUpsertDto dto, CancellationToken ct) =>
        Ok(await _svc.CreateAsync(dto, ct));
}

[ApiController]
[Authorize]
[Route("api/company")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _svc;
    public CompanyController(ICompanyService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<CompanyDto>> Get(CancellationToken ct)
    {
        var c = await _svc.GetAsync(ct);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPut]
    public async Task<ActionResult<CompanyDto>> Update([FromBody] CompanyUpdateDto dto, CancellationToken ct)
    {
        var c = await _svc.UpdateAsync(dto, ct);
        return c is null ? NotFound() : Ok(c);
    }
}
