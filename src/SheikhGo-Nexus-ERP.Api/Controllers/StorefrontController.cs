using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Api.Controllers;

public record StorefrontProductDto(
    Guid Id, string Code, string Name, string? Category, string Unit,
    decimal SalePrice, decimal StockQty, string? Barcode, bool SellByWeight);

public record StorefrontOrderLineDto(Guid ProductId, decimal Qty);
public record StorefrontOrderCreateDto(string CustomerName, string? Phone, string? Address, string? Notes, IReadOnlyList<StorefrontOrderLineDto> Lines);
public record StorefrontOrderDto(Guid SalesOrderId, string OrderNo, decimal GrandTotal, string Status);

[ApiController]
[Route("api/storefront")]
public class StorefrontController(
    AppDbContext db,
    ISalesService sales) : ControllerBase
{
    [HttpGet("products")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<StorefrontProductDto>>> Products(CancellationToken ct)
    {
        // Anonymous browse: resolve demo/first tenant when JWT tenant context is empty.
        var tenantId = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).FirstOrDefaultAsync(ct);
        var q = db.Products.AsNoTracking().IgnoreQueryFilters()
            .Include(x => x.Category)
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.Stocks)
            .Where(x => !x.IsDeleted && x.IsActive && x.IsFinishedGood);
        if (tenantId != Guid.Empty)
            q = q.Where(x => x.TenantId == tenantId);

        var rows = await q.OrderBy(x => x.Name)
            .Select(x => new StorefrontProductDto(
                x.Id, x.Code, x.Name, x.Category != null ? x.Category.Name : null,
                x.UnitOfMeasure != null ? x.UnitOfMeasure.Code : "PCS",
                x.SalePrice, x.Stocks.Sum(s => s.Quantity), x.Barcode, x.SellByWeight))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("orders")]
    [Authorize]
    public async Task<ActionResult<StorefrontOrderDto>> PlaceOrder([FromBody] StorefrontOrderCreateDto dto, CancellationToken ct)
    {
        try
        {
            if (dto.Lines is null || dto.Lines.Count == 0)
                return BadRequest(new { message = "Cart is empty." });

            var party = await db.Parties.FirstOrDefaultAsync(x => x.Code == "CUS-WEB", ct);
            if (party is null)
            {
                party = new Domain.Entities.Party
                {
                    Code = "CUS-WEB",
                    Name = "Online Customer",
                    PartyType = Domain.Enums.PartyType.Customer,
                    Phone = dto.Phone,
                    Address = dto.Address
                };
                db.Parties.Add(party);
                db.CustomerProfiles.Add(new Domain.Entities.CustomerProfile
                {
                    Party = party,
                    PaymentTerms = "Cash",
                    CreditLimit = 0
                });
                await db.SaveChangesAsync(ct);
            }

            var warehouse = await db.Warehouses.FirstAsync(x => x.IsDefault || true, ct);
            var lines = new List<InvoiceLineDto>();
            foreach (var l in dto.Lines)
            {
                var p = await db.Products.FirstOrDefaultAsync(x => x.Id == l.ProductId && x.IsActive, ct)
                    ?? throw new InvalidOperationException("Product not found.");
                var qty = Math.Round(l.Qty, 3);
                if (qty <= 0) throw new InvalidOperationException($"Invalid qty for {p.Name}.");
                var stock = await db.WarehouseStocks
                    .Where(s => s.ProductId == p.Id && s.WarehouseId == warehouse.Id)
                    .SumAsync(s => (decimal?)s.Quantity, ct) ?? 0;
                if (stock < qty) throw new InvalidOperationException($"Insufficient stock for {p.Name}.");
                var amount = Math.Round(qty * p.SalePrice, 2);
                lines.Add(new InvoiceLineDto(null, p.Id, p.Name, qty, p.SalePrice, amount));
            }

            var notes = $"Online: {dto.CustomerName}; {dto.Phone}; {dto.Address}; {dto.Notes}".Trim();
            var so = await sales.CreateOrderAsync(new SalesOrderCreateDto(
                DateTime.UtcNow.Date, party.Id, warehouse.Id, 0, notes, false, lines), ct);

            return Ok(new StorefrontOrderDto(so.Id, so.OrderNo, so.GrandTotal, so.Status.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
