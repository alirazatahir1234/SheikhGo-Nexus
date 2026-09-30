using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Api.Controllers;

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
    private readonly IPermissionService _permissions;
    public DashboardController(IDashboardService svc, IPermissionService permissions)
    {
        _svc = svc;
        _permissions = permissions;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken ct) =>
        Ok(await _svc.GetSummaryAsync(ct));

    [HttpGet("executive")]
    public async Task<ActionResult<ExecutiveDashboardDto>> Executive(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        try
        {
            await _permissions.EnsureAsync("Reports.Executive.View", ct);
            return Ok(await _svc.GetExecutiveAsync(from, to, ct));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }
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
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _svc;
    private readonly IDocumentWorkflowService _workflow;

    public SalesController(ISalesService svc, IDocumentWorkflowService workflow)
    {
        _svc = svc;
        _workflow = workflow;
    }

    // ── Sales orders ───────────────────────────────────────────────────────
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<SalesOrderDto>>> GetOrders(CancellationToken ct) =>
        Ok(await _svc.GetOrdersAsync(ct));

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct) =>
        (await _svc.GetOrderByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("orders")]
    public async Task<ActionResult<SalesOrderDto>> CreateOrder([FromBody] SalesOrderCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateOrderAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("orders/{id:guid}")]
    public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] SalesOrderCreateDto dto, CancellationToken ct)
    {
        try { return (await _svc.UpdateOrderAsync(id, dto, ct)) is { } x ? Ok(x) : NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("orders/{id:guid}/{actionName:regex(^(submit|verify|approve|post|cancel)$)}")]
    public Task<IActionResult> OrderTransition(Guid id, string actionName, [FromBody] DocumentActionDto? body, CancellationToken ct) =>
        Run(() => _workflow.TransitionSalesOrderAsync(id, actionName, body?.Remarks, ct));

    // ── Delivery orders ────────────────────────────────────────────────────
    [HttpGet("deliveries")]
    public async Task<ActionResult<IReadOnlyList<DeliveryOrderDto>>> GetDeliveries(CancellationToken ct) =>
        Ok(await _svc.GetDeliveriesAsync(ct));

    [HttpGet("deliveries/{id:guid}")]
    public async Task<IActionResult> GetDelivery(Guid id, CancellationToken ct) =>
        (await _svc.GetDeliveryByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("deliveries")]
    public async Task<ActionResult<DeliveryOrderDto>> CreateDelivery([FromBody] DeliveryOrderCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateDeliveryAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("deliveries/{id:guid}/{actionName:regex(^(submit|verify|approve|post|cancel)$)}")]
    public Task<IActionResult> DeliveryTransition(Guid id, string actionName, [FromBody] DocumentActionDto? body, CancellationToken ct) =>
        Run(() => _workflow.TransitionDeliveryOrderAsync(id, actionName, body?.Remarks, ct));

    // ── Sales invoices ─────────────────────────────────────────────────────
    [HttpGet("invoices")]
    public async Task<ActionResult<IReadOnlyList<SalesInvoiceDocDto>>> GetInvoices(CancellationToken ct) =>
        Ok(await _svc.GetInvoicesAsync(ct));

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct) =>
        (await _svc.GetInvoiceByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("invoices")]
    public async Task<ActionResult<SalesInvoiceDocDto>> CreateInvoice([FromBody] SalesInvoiceDocCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateInvoiceAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("invoices/{id:guid}/{actionName:regex(^(submit|verify|approve|post|cancel)$)}")]
    public Task<IActionResult> InvoiceTransition(Guid id, string actionName, [FromBody] DocumentActionDto? body, CancellationToken ct) =>
        Run(() => _workflow.TransitionSalesInvoiceAsync(id, actionName, body?.Remarks, ct));

    // ── Customer payments ──────────────────────────────────────────────────
    [HttpGet("payments")]
    public async Task<ActionResult<IReadOnlyList<CustomerPaymentDto>>> GetPayments(CancellationToken ct) =>
        Ok(await _svc.GetPaymentsAsync(ct));

    [HttpGet("payments/{id:guid}")]
    public async Task<IActionResult> GetPayment(Guid id, CancellationToken ct) =>
        (await _svc.GetPaymentByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("payments")]
    public async Task<ActionResult<CustomerPaymentDto>> CreatePayment([FromBody] CustomerPaymentCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreatePaymentAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("payments/{id:guid}/{actionName:regex(^(submit|verify|approve|post|cancel)$)}")]
    public Task<IActionResult> PaymentTransition(Guid id, string actionName, [FromBody] DocumentActionDto? body, CancellationToken ct) =>
        Run(() => _workflow.TransitionCustomerPaymentAsync(id, actionName, body?.Remarks, ct));

    // ── Receivables / audit ────────────────────────────────────────────────
    [HttpGet("receivables/{partyId:guid}")]
    public async Task<ActionResult<ReceivableBalanceDto>> GetReceivable(Guid partyId, CancellationToken ct)
    {
        var bal = await _svc.GetReceivableAsync(partyId, ct);
        return bal is null ? NotFound() : Ok(bal);
    }

    [HttpGet("audit/{entityName}/{id:guid}")]
    public async Task<IActionResult> Audit(string entityName, Guid id, CancellationToken ct) =>
        Ok(await _svc.GetAuditAsync(entityName, id, ct));

    private async Task<IActionResult> Run<T>(Func<Task<T?>> action) where T : class
    {
        try
        {
            var result = await action();
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
    public async Task<ActionResult<IReadOnlyList<StockDto>>> Stock([FromQuery] Guid? warehouseId, CancellationToken ct) =>
        Ok(await _svc.GetStockAsync(warehouseId, ct));

    [HttpGet("ledger")]
    public async Task<ActionResult<IReadOnlyList<InventoryLedgerLineDto>>> Ledger(
        [FromQuery] Guid? productId, [FromQuery] Guid? warehouseId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var f = from ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var t = to ?? DateTime.UtcNow.Date;
        return Ok(await _svc.GetLedgerAsync(productId, warehouseId, f, t, ct));
    }

    [HttpGet("valuation")]
    public async Task<ActionResult<IReadOnlyList<StockValuationLineDto>>> Valuation([FromQuery] Guid? warehouseId, CancellationToken ct) =>
        Ok(await _svc.GetValuationAsync(warehouseId, ct));

    [HttpGet("movement")]
    public async Task<ActionResult<IReadOnlyList<StockMovementLineDto>>> Movement(
        [FromQuery] Guid? productId, [FromQuery] Guid? warehouseId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var f = from ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var t = to ?? DateTime.UtcNow.Date;
        return Ok(await _svc.GetMovementAsync(productId, warehouseId, f, t, ct));
    }
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(e);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] EmployeeUpsertDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> Update(Guid id, [FromBody] EmployeeUpsertDto dto, CancellationToken ct)
    {
        try
        {
            var e = await _svc.UpdateAsync(id, dto, ct);
            return e is null ? NotFound() : Ok(e);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
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

[ApiController]
[Authorize]
[Route("api/finance")]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _svc;
    private readonly IAccountService _accounts;
    private readonly IAdminService _admin;

    public FinanceController(IFinanceService svc, IAccountService accounts, IAdminService admin)
    {
        _svc = svc;
        _accounts = accounts;
        _admin = admin;
    }

    [HttpGet("accounts")]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> Accounts(CancellationToken ct) =>
        Ok(await _accounts.GetAllAsync(ct));

    [HttpPost("accounts")]
    public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] AccountUpsertDto dto, CancellationToken ct)
    {
        try { return Ok(await _accounts.CreateAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("journals")]
    public async Task<ActionResult<IReadOnlyList<JournalEntryDto>>> Journals(CancellationToken ct) =>
        Ok(await _svc.GetJournalsAsync(ct));

    [HttpGet("journals/{id:guid}")]
    public async Task<IActionResult> Journal(Guid id, CancellationToken ct) =>
        (await _svc.GetJournalByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("journals")]
    public async Task<ActionResult<JournalEntryDto>> CreateJournal([FromBody] JournalEntryCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateJournalAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("journals/{id:guid}/post")]
    public async Task<IActionResult> PostJournal(Guid id, CancellationToken ct)
    {
        try { return (await _svc.PostJournalAsync(id, ct)) is { } x ? Ok(x) : NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("cash-bank")]
    public async Task<ActionResult<IReadOnlyList<CashBankAccountDto>>> CashBank(CancellationToken ct) =>
        Ok(await _svc.GetCashBankAccountsAsync(ct));

    [HttpPost("cash-bank")]
    public async Task<ActionResult<CashBankAccountDto>> CreateCashBank([FromBody] CashBankAccountUpsertDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateCashBankAccountAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("cash-bank/transactions")]
    public async Task<ActionResult<IReadOnlyList<CashBankTxnDto>>> CashBankTxns(CancellationToken ct) =>
        Ok(await _svc.GetCashBankTransactionsAsync(ct));

    [HttpPost("cash-bank/transactions")]
    public async Task<ActionResult<CashBankTxnDto>> CreateCashBankTxn([FromBody] CashBankTxnCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateCashBankTransactionAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("supplier-payments")]
    public async Task<ActionResult<IReadOnlyList<SupplierPaymentDto>>> SupplierPayments(CancellationToken ct) =>
        Ok(await _svc.GetSupplierPaymentsAsync(ct));

    [HttpGet("supplier-payments/{id:guid}")]
    public async Task<IActionResult> SupplierPayment(Guid id, CancellationToken ct) =>
        (await _svc.GetSupplierPaymentByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("supplier-payments")]
    public async Task<ActionResult<SupplierPaymentDto>> CreateSupplierPayment([FromBody] SupplierPaymentCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await _svc.CreateSupplierPaymentAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("supplier-payments/{id:guid}/{actionName:regex(^(submit|post|cancel)$)}")]
    public async Task<IActionResult> SupplierPaymentTransition(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var result = await _svc.TransitionSupplierPaymentAsync(id, actionName, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("periods/ensure")]
    public async Task<IActionResult> EnsurePeriods(CancellationToken ct)
    {
        await _svc.EnsureMonthlyPeriodsAsync(ct);
        return Ok(new { message = "Periods ensured." });
    }

    [HttpPost("periods/{id:guid}/close")]
    public async Task<IActionResult> ClosePeriod(Guid id, CancellationToken ct) =>
        (await _svc.ClosePeriodAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpPost("periods/{id:guid}/open")]
    public async Task<IActionResult> OpenPeriod(Guid id, CancellationToken ct) =>
        (await _svc.OpenPeriodAsync(id, ct)) is { } x ? Ok(x) : NotFound();

    [HttpGet("periods")]
    public async Task<ActionResult<IReadOnlyList<FiscalPeriodDto>>> Periods([FromQuery] Guid? yearId, CancellationToken ct)
    {
        await _svc.EnsureMonthlyPeriodsAsync(ct);
        var years = await _admin.GetFinancialYearsAsync(ct);
        var yid = yearId ?? years.FirstOrDefault(y => y.IsCurrent)?.Id ?? years.FirstOrDefault()?.Id;
        if (yid is null) return Ok(Array.Empty<FiscalPeriodDto>());
        return Ok(await _admin.GetFiscalPeriodsAsync(yid.Value, ct));
    }
}
