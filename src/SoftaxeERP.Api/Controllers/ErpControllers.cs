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
    public PurchasesController(IPurchaseService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseInvoiceDto>>> GetAll(CancellationToken ct) =>
        Ok(await _svc.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<PurchaseInvoiceDto>> Create([FromBody] PurchaseInvoiceCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> GetOrders(CancellationToken ct) =>
        Ok(await _svc.GetOrdersAsync(ct));

    [HttpPost("orders")]
    public async Task<ActionResult<PurchaseOrderDto>> CreateOrder([FromBody] PurchaseOrderCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateOrderAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("receipts")]
    public async Task<ActionResult<IReadOnlyList<GoodsReceiptDto>>> GetReceipts(CancellationToken ct) =>
        Ok(await _svc.GetReceiptsAsync(ct));

    [HttpPost("receipts")]
    public async Task<ActionResult<GoodsReceiptDto>> CreateReceipt([FromBody] GoodsReceiptCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateReceiptAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("payables/{partyId:guid}")]
    public async Task<ActionResult<PayableBalanceDto>> GetPayable(Guid partyId, CancellationToken ct)
    {
        var bal = await _svc.GetPayableAsync(partyId, ct);
        return bal is null ? NotFound() : Ok(bal);
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
