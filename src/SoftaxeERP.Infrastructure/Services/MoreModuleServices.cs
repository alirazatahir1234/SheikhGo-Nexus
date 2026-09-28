using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Infrastructure.Persistence;

namespace SoftaxeERP.Infrastructure.Services;

public class BrandService : IBrandService
{
    private readonly AppDbContext _db;
    public BrandService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Brands.OrderBy(x => x.Code).Select(x => new BrandDto(x.Id, x.Code, x.Name, x.IsActive)).ToListAsync(ct);

    public async Task<BrandDto> CreateAsync(BrandUpsertDto dto, CancellationToken ct = default)
    {
        var e = new Brand { Code = dto.Code.Trim(), Name = dto.Name.Trim(), IsActive = dto.IsActive };
        _db.Brands.Add(e);
        await _db.SaveChangesAsync(ct);
        return new BrandDto(e.Id, e.Code, e.Name, e.IsActive);
    }
}

public class ProductConversionService : IProductConversionService
{
    private readonly AppDbContext _db;
    public ProductConversionService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductConversionDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.ProductConversions.Include(x => x.Product).OrderBy(x => x.Product!.Code)
            .Select(x => new ProductConversionDto(x.Id, x.ProductId, x.Product!.Name, x.Unit, x.Packing, x.PackingSize, x.PurchaseRate, x.MinRate, x.MaxRate))
            .ToListAsync(ct);

    public async Task<ProductConversionDto> CreateAsync(ProductConversionUpsertDto dto, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstAsync(p => p.Id == dto.ProductId, ct);
        var e = new ProductConversion
        {
            ProductId = product.Id, Unit = dto.Unit, Packing = dto.Packing, PackingSize = dto.PackingSize,
            PurchaseRate = dto.PurchaseRate, MinRate = dto.MinRate, MaxRate = dto.MaxRate
        };
        _db.ProductConversions.Add(e);
        await _db.SaveChangesAsync(ct);
        return new ProductConversionDto(e.Id, e.ProductId, product.Name, e.Unit, e.Packing, e.PackingSize, e.PurchaseRate, e.MinRate, e.MaxRate);
    }
}

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public BudgetService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<BudgetDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Budgets.Include(x => x.Lines).ThenInclude(l => l.Account).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .Select(x => new BudgetDto(x.Id, x.BudgetNo, x.Date, x.Year, x.Month, x.Status, x.Remarks,
                x.Lines.Sum(l => l.Amount),
                x.Lines.OrderBy(l => l.LineNo).Select(l => new BudgetLineDto(l.Id, l.AccountId, l.Account != null ? l.Account.Name : null, l.Description, l.Qty, l.Amount)).ToList()))
            .ToListAsync(ct);

    public async Task<BudgetDto> CreateAsync(BudgetCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Add at least one budget line.");
        var no = await _numbers.NextAsync("BDG", "BDG", ct);
        var lines = new List<BudgetLine>();
        var i = 1;
        foreach (var l in dto.Lines)
        {
            lines.Add(new BudgetLine
            {
                AccountId = l.AccountId, Description = l.Description, Qty = l.Qty, Amount = l.Amount, LineNo = i++
            });
        }
        var e = new Budget
        {
            BudgetNo = no, Date = dto.Date.Date, Year = dto.Year, Month = dto.Month,
            Status = "Draft", Remarks = dto.Remarks, Lines = lines
        };
        _db.Budgets.Add(e);
        await _db.SaveChangesAsync(ct);
        return (await GetAllAsync(ct)).First(x => x.Id == e.Id);
    }

    public async Task<bool> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.Budgets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return false;
        e.Status = "Approved";
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public AttendanceService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<AttendanceDto>> GetAttendanceAsync(CancellationToken ct = default) =>
        await _db.AttendanceEntries.Include(x => x.Employee).OrderByDescending(x => x.Date)
            .Select(x => new AttendanceDto(x.Id, x.Date, x.EmployeeId, x.Employee!.Name, x.Status, x.Shift, x.Remarks))
            .ToListAsync(ct);

    public async Task<AttendanceDto> CreateAttendanceAsync(AttendanceCreateDto dto, CancellationToken ct = default)
    {
        var emp = await _db.Employees.FirstAsync(x => x.Id == dto.EmployeeId, ct);
        var existing = await _db.AttendanceEntries.FirstOrDefaultAsync(x => x.EmployeeId == dto.EmployeeId && x.Date == dto.Date.Date, ct);
        if (existing != null)
        {
            existing.Status = dto.Status;
            existing.Shift = dto.Shift;
            existing.Remarks = dto.Remarks;
            await _db.SaveChangesAsync(ct);
            return new AttendanceDto(existing.Id, existing.Date, existing.EmployeeId, emp.Name, existing.Status, existing.Shift, existing.Remarks);
        }
        var e = new AttendanceEntry
        {
            Date = dto.Date.Date, EmployeeId = emp.Id, Status = dto.Status, Shift = dto.Shift, Remarks = dto.Remarks
        };
        _db.AttendanceEntries.Add(e);
        await _db.SaveChangesAsync(ct);
        return new AttendanceDto(e.Id, e.Date, e.EmployeeId, emp.Name, e.Status, e.Shift, e.Remarks);
    }

    public async Task<IReadOnlyList<OvertimeDto>> GetOvertimeAsync(CancellationToken ct = default) =>
        await _db.OvertimeEntries.Include(x => x.Employee).OrderByDescending(x => x.Date)
            .Select(x => new OvertimeDto(x.Id, x.DocNo, x.Date, x.EmployeeId, x.Employee!.Name, x.Hours, x.PerHourRate, x.Amount, x.Remarks))
            .ToListAsync(ct);

    public async Task<OvertimeDto> CreateOvertimeAsync(OvertimeCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Hours <= 0) throw new InvalidOperationException("Hours must be greater than zero.");
        var emp = await _db.Employees.FirstAsync(x => x.Id == dto.EmployeeId, ct);
        var no = await _numbers.NextAsync("OT", "OT", ct);
        var amount = Math.Round(dto.Hours * dto.PerHourRate, 2);
        var e = new OvertimeEntry
        {
            DocNo = no, Date = dto.Date.Date, EmployeeId = emp.Id, Hours = dto.Hours,
            PerHourRate = dto.PerHourRate, Amount = amount, Remarks = dto.Remarks
        };
        _db.OvertimeEntries.Add(e);
        await _db.SaveChangesAsync(ct);
        return new OvertimeDto(e.Id, e.DocNo, e.Date, e.EmployeeId, emp.Name, e.Hours, e.PerHourRate, e.Amount, e.Remarks);
    }
}

public class DayCloseService : IDayCloseService
{
    private readonly AppDbContext _db;
    public DayCloseService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DayCloseDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.DayCloses.OrderByDescending(x => x.CloseDate)
            .Select(x => new DayCloseDto(x.Id, x.CloseDate, x.IsClosed, x.Remarks, x.ClosedBy)).ToListAsync(ct);

    public async Task<DayCloseDto> CloseAsync(DayCloseCreateDto dto, CancellationToken ct = default)
    {
        var date = dto.CloseDate.Date;
        var existing = await _db.DayCloses.FirstOrDefaultAsync(x => x.CloseDate == date, ct);
        if (existing != null)
        {
            if (existing.IsClosed) throw new InvalidOperationException("Date already closed.");
            existing.IsClosed = true;
            existing.Remarks = dto.Remarks;
            await _db.SaveChangesAsync(ct);
            return new DayCloseDto(existing.Id, existing.CloseDate, existing.IsClosed, existing.Remarks, existing.ClosedBy);
        }
        var e = new DayClose { CloseDate = date, IsClosed = true, Remarks = dto.Remarks, ClosedBy = "admin" };
        _db.DayCloses.Add(e);
        await _db.SaveChangesAsync(ct);
        return new DayCloseDto(e.Id, e.CloseDate, e.IsClosed, e.Remarks, e.ClosedBy);
    }

    public async Task<bool> IsDateClosedAsync(DateTime date, CancellationToken ct = default) =>
        await _db.DayCloses.AnyAsync(x => x.CloseDate == date.Date && x.IsClosed, ct);
}
