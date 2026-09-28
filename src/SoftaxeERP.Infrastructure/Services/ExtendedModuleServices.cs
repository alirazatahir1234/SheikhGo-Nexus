using Microsoft.EntityFrameworkCore;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Domain.Entities;
using SoftaxeERP.Domain.Enums;
using SoftaxeERP.Infrastructure.Persistence;

namespace SoftaxeERP.Infrastructure.Services;

public class PurchaseContractService : IPurchaseContractService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public PurchaseContractService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<PurchaseContractDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.PurchaseContracts.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.Date)
            .Select(x => new PurchaseContractDto(x.Id, x.ContractNo, x.Date, x.DeliveryDate, x.PartyId, x.Party!.Name, x.Status, x.Remarks,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new PurchaseContractLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.ReceivedQty, l.Rate, l.Unit, l.Remarks)).ToList()))
            .ToListAsync(ct);

    public async Task<PurchaseContractDto> CreateAsync(PurchaseContractCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Add at least one contract line.");
        var party = await _db.Parties.FirstAsync(p => p.Id == dto.PartyId, ct);
        var no = await _numbers.NextAsync("PC", "PC", ct);
        var lines = new List<PurchaseContractLine>();
        var i = 1;
        foreach (var l in dto.Lines)
        {
            var product = await _db.Products.FirstAsync(p => p.Id == l.ProductId, ct);
            lines.Add(new PurchaseContractLine
            {
                ProductId = product.Id, Qty = l.Qty, Rate = l.Rate, Unit = l.Unit ?? product.Unit,
                Remarks = l.Remarks, LineNo = i++
            });
        }
        var entity = new PurchaseContract
        {
            ContractNo = no, Date = dto.Date.Date, DeliveryDate = dto.DeliveryDate?.Date,
            PartyId = party.Id, Status = "Open", Remarks = dto.Remarks, Lines = lines
        };
        _db.PurchaseContracts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (await GetAllAsync(ct)).First(x => x.Id == entity.Id);
    }

    public async Task<bool> CloseAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.PurchaseContracts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return false;
        e.Status = "Closed";
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class LabService : ILabService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public LabService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<LabTestTypeDto>> GetTestTypesAsync(CancellationToken ct = default) =>
        await _db.LabTestTypes.OrderBy(x => x.Code)
            .Select(x => new LabTestTypeDto(x.Id, x.Code, x.Name, x.Unit, x.IsActive)).ToListAsync(ct);

    public async Task<LabTestTypeDto> UpsertTestTypeAsync(LabTestTypeUpsertDto dto, CancellationToken ct = default)
    {
        var existing = await _db.LabTestTypes.FirstOrDefaultAsync(x => x.Code == dto.Code, ct);
        if (existing is null)
        {
            existing = new LabTestType { Code = dto.Code.Trim(), Name = dto.Name.Trim(), Unit = dto.Unit, IsActive = dto.IsActive };
            _db.LabTestTypes.Add(existing);
        }
        else
        {
            existing.Name = dto.Name.Trim();
            existing.Unit = dto.Unit;
            existing.IsActive = dto.IsActive;
        }
        await _db.SaveChangesAsync(ct);
        return new LabTestTypeDto(existing.Id, existing.Code, existing.Name, existing.Unit, existing.IsActive);
    }

    public async Task<IReadOnlyList<LabResultDto>> GetResultsAsync(CancellationToken ct = default) =>
        await _db.LabResults.Include(x => x.Party).Include(x => x.Product).Include(x => x.LabTestType)
            .OrderByDescending(x => x.Date)
            .Select(x => new LabResultDto(x.Id, x.ResultNo, x.Date, x.PartyId, x.Party != null ? x.Party.Name : null, x.ProductId, x.Product != null ? x.Product.Name : null,
                x.LabTestTypeId, x.LabTestType != null ? x.LabTestType.Name : null, x.VehicleNo, x.Bags, x.Percentage, x.PartyDeductionKg, x.StockDeductionKg, x.Remarks))
            .ToListAsync(ct);

    public async Task<LabResultDto> CreateResultAsync(LabResultCreateDto dto, CancellationToken ct = default)
    {
        var no = await _numbers.NextAsync("LR", "LR", ct);
        var entity = new LabResult
        {
            ResultNo = no, Date = dto.Date.Date, PartyId = dto.PartyId, ProductId = dto.ProductId,
            LabTestTypeId = dto.LabTestTypeId, VehicleNo = dto.VehicleNo, Bags = dto.Bags,
            Percentage = dto.Percentage, PartyDeductionKg = dto.PartyDeductionKg,
            StockDeductionKg = dto.StockDeductionKg, Remarks = dto.Remarks
        };
        _db.LabResults.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (await GetResultsAsync(ct)).First(x => x.Id == entity.Id);
    }
}

public class CostCentreService : ICostCentreService
{
    private readonly AppDbContext _db;
    public CostCentreService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CostCentreDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.CostCentres.OrderBy(x => x.Code)
            .Select(x => new CostCentreDto(x.Id, x.Code, x.Name, x.Location, x.IsActive, x.Remarks)).ToListAsync(ct);

    public async Task<CostCentreDto> CreateAsync(CostCentreUpsertDto dto, CancellationToken ct = default)
    {
        var e = new CostCentre { Code = dto.Code.Trim(), Name = dto.Name.Trim(), Location = dto.Location, IsActive = dto.IsActive, Remarks = dto.Remarks };
        _db.CostCentres.Add(e);
        await _db.SaveChangesAsync(ct);
        return new CostCentreDto(e.Id, e.Code, e.Name, e.Location, e.IsActive, e.Remarks);
    }
}

public class ChequeService : IChequeService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public ChequeService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<ChequeBookDto>> GetBooksAsync(CancellationToken ct = default) =>
        await _db.ChequeBooks.OrderByDescending(x => x.Date)
            .Select(x => new ChequeBookDto(x.Id, x.BookNo, x.Date, x.BankName, x.ChequeFrom, x.ChequeTo, x.NextChequeNo, x.IsClosed, x.Remarks))
            .ToListAsync(ct);

    public async Task<ChequeBookDto> CreateBookAsync(ChequeBookCreateDto dto, CancellationToken ct = default)
    {
        if (dto.ChequeTo < dto.ChequeFrom) throw new InvalidOperationException("Cheque To must be >= From.");
        var e = new ChequeBook
        {
            BookNo = dto.BookNo.Trim(), Date = dto.Date.Date, BankName = dto.BankName.Trim(),
            ChequeFrom = dto.ChequeFrom, ChequeTo = dto.ChequeTo, NextChequeNo = dto.ChequeFrom, Remarks = dto.Remarks
        };
        _db.ChequeBooks.Add(e);
        await _db.SaveChangesAsync(ct);
        return new ChequeBookDto(e.Id, e.BookNo, e.Date, e.BankName, e.ChequeFrom, e.ChequeTo, e.NextChequeNo, e.IsClosed, e.Remarks);
    }

    public async Task<IReadOnlyList<PostDatedChequeDto>> GetChequesAsync(CancellationToken ct = default) =>
        await _db.PostDatedCheques.Include(x => x.Party).OrderByDescending(x => x.ChequeDate)
            .Select(x => new PostDatedChequeDto(x.Id, x.DocNo, x.VoucherDate, x.ChequeDate, x.ChequeNo, x.PartyId, x.Party != null ? x.Party.Name : null,
                x.ChequeBookId, x.Amount, x.Direction, x.Status, x.ClearedDate, x.BouncedDate, x.Remarks))
            .ToListAsync(ct);

    public async Task<PostDatedChequeDto> CreateChequeAsync(PostDatedChequeCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");
        var no = await _numbers.NextAsync("PDC", "PDC", ct);
        if (dto.ChequeBookId is Guid bookId)
        {
            var book = await _db.ChequeBooks.FirstAsync(x => x.Id == bookId, ct);
            if (book.IsClosed) throw new InvalidOperationException("Cheque book is closed.");
            if (book.NextChequeNo > book.ChequeTo) throw new InvalidOperationException("Cheque book exhausted.");
            book.NextChequeNo++;
            if (book.NextChequeNo > book.ChequeTo) book.IsClosed = true;
        }
        var e = new PostDatedCheque
        {
            DocNo = no, VoucherDate = dto.VoucherDate.Date, ChequeDate = dto.ChequeDate.Date,
            ChequeNo = dto.ChequeNo.Trim(), PartyId = dto.PartyId, ChequeBookId = dto.ChequeBookId,
            Amount = dto.Amount, Direction = string.Equals(dto.Direction, "Received", StringComparison.OrdinalIgnoreCase) ? "Received" : "Issued",
            Status = "Pending", Remarks = dto.Remarks
        };
        _db.PostDatedCheques.Add(e);
        await _db.SaveChangesAsync(ct);
        return (await GetChequesAsync(ct)).First(x => x.Id == e.Id);
    }

    public async Task<PostDatedChequeDto?> UpdateStatusAsync(Guid id, PostDatedChequeStatusDto dto, CancellationToken ct = default)
    {
        var e = await _db.PostDatedCheques.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return null;
        e.Status = dto.Status;
        if (dto.Status.Equals("Cleared", StringComparison.OrdinalIgnoreCase)) e.ClearedDate = DateTime.UtcNow.Date;
        if (dto.Status.Equals("Bounced", StringComparison.OrdinalIgnoreCase)) e.BouncedDate = DateTime.UtcNow.Date;
        await _db.SaveChangesAsync(ct);
        return (await GetChequesAsync(ct)).First(x => x.Id == e.Id);
    }
}

public class FarmShareHolderService : IFarmShareHolderService
{
    private readonly AppDbContext _db;
    public FarmShareHolderService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<FarmShareHolderDto>> GetAllAsync(Guid? flockId = null, CancellationToken ct = default)
    {
        var q = _db.FarmShareHolders.Include(x => x.FarmFlock).AsQueryable();
        if (flockId.HasValue) q = q.Where(x => x.FarmFlockId == flockId);
        return await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new FarmShareHolderDto(x.Id, x.FarmFlockId, x.FarmFlock!.Name, x.HolderName, x.PartyCode, x.SharePercent, x.ShareType, x.Remarks))
            .ToListAsync(ct);
    }

    public async Task<FarmShareHolderDto> CreateAsync(FarmShareHolderCreateDto dto, CancellationToken ct = default)
    {
        var flock = await _db.FarmFlocks.FirstAsync(x => x.Id == dto.FarmFlockId, ct);
        var e = new FarmShareHolder
        {
            FarmFlockId = flock.Id, HolderName = dto.HolderName.Trim(), PartyCode = dto.PartyCode,
            SharePercent = dto.SharePercent, ShareType = dto.ShareType, Remarks = dto.Remarks
        };
        _db.FarmShareHolders.Add(e);
        await _db.SaveChangesAsync(ct);
        return new FarmShareHolderDto(e.Id, e.FarmFlockId, flock.Name, e.HolderName, e.PartyCode, e.SharePercent, e.ShareType, e.Remarks);
    }
}

public class SalesForceService : ISalesForceService
{
    private readonly AppDbContext _db;
    public SalesForceService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalesAreaDto>> GetAreasAsync(CancellationToken ct = default) =>
        await _db.SalesAreas.OrderBy(x => x.Code).Select(x => new SalesAreaDto(x.Id, x.Code, x.Name, x.IsActive)).ToListAsync(ct);

    public async Task<SalesAreaDto> CreateAreaAsync(SalesAreaUpsertDto dto, CancellationToken ct = default)
    {
        var e = new SalesArea { Code = dto.Code.Trim(), Name = dto.Name.Trim(), IsActive = dto.IsActive };
        _db.SalesAreas.Add(e);
        await _db.SaveChangesAsync(ct);
        return new SalesAreaDto(e.Id, e.Code, e.Name, e.IsActive);
    }

    public async Task<IReadOnlyList<SalesmanDto>> GetSalesmenAsync(CancellationToken ct = default) =>
        await _db.Salesmen.Include(x => x.SalesArea).OrderBy(x => x.Code)
            .Select(x => new SalesmanDto(x.Id, x.Code, x.Name, x.SalesAreaId, x.SalesArea != null ? x.SalesArea.Name : null, x.Phone, x.IsActive)).ToListAsync(ct);

    public async Task<SalesmanDto> CreateSalesmanAsync(SalesmanUpsertDto dto, CancellationToken ct = default)
    {
        var e = new Salesman { Code = dto.Code.Trim(), Name = dto.Name.Trim(), SalesAreaId = dto.SalesAreaId, Phone = dto.Phone, IsActive = dto.IsActive };
        _db.Salesmen.Add(e);
        await _db.SaveChangesAsync(ct);
        var area = dto.SalesAreaId is null ? null : await _db.SalesAreas.FirstOrDefaultAsync(a => a.Id == dto.SalesAreaId, ct);
        return new SalesmanDto(e.Id, e.Code, e.Name, e.SalesAreaId, area?.Name, e.Phone, e.IsActive);
    }

    public async Task<IReadOnlyList<SalesTargetDto>> GetTargetsAsync(CancellationToken ct = default) =>
        await _db.SalesTargets.Include(x => x.Salesman).Include(x => x.SalesArea).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .Select(x => new SalesTargetDto(x.Id, x.SalesmanId, x.Salesman != null ? x.Salesman.Name : null, x.SalesAreaId, x.SalesArea != null ? x.SalesArea.Name : null, x.Year, x.Month, x.TargetAmount, x.AchievedAmount, x.Remarks))
            .ToListAsync(ct);

    public async Task<SalesTargetDto> CreateTargetAsync(SalesTargetUpsertDto dto, CancellationToken ct = default)
    {
        var e = new SalesTarget
        {
            SalesmanId = dto.SalesmanId, SalesAreaId = dto.SalesAreaId, Year = dto.Year, Month = dto.Month,
            TargetAmount = dto.TargetAmount, AchievedAmount = dto.AchievedAmount, Remarks = dto.Remarks
        };
        _db.SalesTargets.Add(e);
        await _db.SaveChangesAsync(ct);
        return (await GetTargetsAsync(ct)).First(x => x.Id == e.Id);
    }
}

public class MaterialDemandService : IMaterialDemandService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public MaterialDemandService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<MaterialDemandDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.MaterialDemands.Include(x => x.Lines).ThenInclude(l => l.Product).OrderByDescending(x => x.Date)
            .Select(x => new MaterialDemandDto(x.Id, x.DemandNo, x.Date, x.DemandType, x.Status, x.RequestedBy, x.Remarks,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new MaterialDemandLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Remarks)).ToList()))
            .ToListAsync(ct);

    public async Task<MaterialDemandDto> CreateAsync(MaterialDemandCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Add at least one line.");
        var prefix = string.Equals(dto.DemandType, "Requisition", StringComparison.OrdinalIgnoreCase) ? "RQ" : "DM";
        var no = await _numbers.NextAsync(prefix, prefix, ct);
        var lines = new List<MaterialDemandLine>();
        var i = 1;
        foreach (var l in dto.Lines)
        {
            await _db.Products.FirstAsync(p => p.Id == l.ProductId, ct);
            lines.Add(new MaterialDemandLine { ProductId = l.ProductId, Qty = l.Qty, Remarks = l.Remarks, LineNo = i++ });
        }
        var e = new MaterialDemand
        {
            DemandNo = no, Date = dto.Date.Date,
            DemandType = string.Equals(dto.DemandType, "Requisition", StringComparison.OrdinalIgnoreCase) ? "Requisition" : "Demand",
            Status = "Open", RequestedBy = dto.RequestedBy, Remarks = dto.Remarks, Lines = lines
        };
        _db.MaterialDemands.Add(e);
        await _db.SaveChangesAsync(ct);
        return (await GetAllAsync(ct)).First(x => x.Id == e.Id);
    }
}

public class HrFinanceService : IHrFinanceService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public HrFinanceService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<SalaryAdvanceDto>> GetAdvancesAsync(CancellationToken ct = default) =>
        await _db.SalaryAdvances.Include(x => x.Employee).OrderByDescending(x => x.Date)
            .Select(x => new SalaryAdvanceDto(x.Id, x.DocNo, x.Date, x.EmployeeId, x.Employee!.Name, x.Amount, x.Remarks)).ToListAsync(ct);

    public async Task<SalaryAdvanceDto> CreateAdvanceAsync(SalaryAdvanceCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");
        var emp = await _db.Employees.FirstAsync(x => x.Id == dto.EmployeeId, ct);
        var no = await _numbers.NextAsync("ADV", "ADV", ct);
        var e = new SalaryAdvance { DocNo = no, Date = dto.Date.Date, EmployeeId = emp.Id, Amount = dto.Amount, Remarks = dto.Remarks };
        _db.SalaryAdvances.Add(e);
        await _db.SaveChangesAsync(ct);
        return new SalaryAdvanceDto(e.Id, e.DocNo, e.Date, e.EmployeeId, emp.Name, e.Amount, e.Remarks);
    }

    public async Task<IReadOnlyList<StaffLoanDto>> GetLoansAsync(CancellationToken ct = default) =>
        await _db.StaffLoans.Include(x => x.Employee).OrderByDescending(x => x.Date)
            .Select(x => new StaffLoanDto(x.Id, x.DocNo, x.Date, x.EmployeeId, x.Employee!.Name, x.LoanAmount, x.Months, x.InstallmentAmount, x.IsActive, x.Remarks))
            .ToListAsync(ct);

    public async Task<StaffLoanDto> CreateLoanAsync(StaffLoanCreateDto dto, CancellationToken ct = default)
    {
        if (dto.LoanAmount <= 0 || dto.Months <= 0) throw new InvalidOperationException("Invalid loan terms.");
        var emp = await _db.Employees.FirstAsync(x => x.Id == dto.EmployeeId, ct);
        var no = await _numbers.NextAsync("LN", "LN", ct);
        var installment = dto.InstallmentAmount > 0 ? dto.InstallmentAmount : Math.Round(dto.LoanAmount / dto.Months, 2);
        var e = new StaffLoan
        {
            DocNo = no, Date = dto.Date.Date, EmployeeId = emp.Id, LoanAmount = dto.LoanAmount,
            Months = dto.Months, InstallmentAmount = installment, IsActive = true, Remarks = dto.Remarks
        };
        _db.StaffLoans.Add(e);
        await _db.SaveChangesAsync(ct);
        return new StaffLoanDto(e.Id, e.DocNo, e.Date, e.EmployeeId, emp.Name, e.LoanAmount, e.Months, e.InstallmentAmount, e.IsActive, e.Remarks);
    }

    public async Task<IReadOnlyList<EobiDto>> GetEobiAsync(CancellationToken ct = default) =>
        await _db.EobiEntries.Include(x => x.Employee).OrderByDescending(x => x.Date)
            .Select(x => new EobiDto(x.Id, x.DocNo, x.Date, x.EmployeeId, x.Employee!.Name, x.Amount, x.IsActive, x.Remarks))
            .ToListAsync(ct);

    public async Task<EobiDto> CreateEobiAsync(EobiCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0) throw new InvalidOperationException("EOBI amount must be greater than zero.");
        var emp = await _db.Employees.FirstAsync(x => x.Id == dto.EmployeeId, ct);
        var no = await _numbers.NextAsync("EOBI", "EOBI", ct);
        var e = new EobiEntry
        {
            DocNo = no, Date = dto.Date.Date, EmployeeId = emp.Id, Amount = dto.Amount,
            IsActive = true, Remarks = dto.Remarks
        };
        _db.EobiEntries.Add(e);
        await _db.SaveChangesAsync(ct);
        return new EobiDto(e.Id, e.DocNo, e.Date, e.EmployeeId, emp.Name, e.Amount, e.IsActive, e.Remarks);
    }
}

public class WeighbridgeService : IWeighbridgeService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public WeighbridgeService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<WeighbridgeTicketDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.WeighbridgeTickets.Include(x => x.Party).Include(x => x.Product).OrderByDescending(x => x.Date)
            .Select(x => new WeighbridgeTicketDto(x.Id, x.TicketNo, x.Date, x.Direction, x.VehicleNo, x.PartyId,
                x.Party != null ? x.Party.Name : null, x.ProductId, x.Product != null ? x.Product.Name : null,
                x.FirstWeight, x.SecondWeight, x.NetWeight, x.Status, x.Remarks))
            .ToListAsync(ct);

    public async Task<WeighbridgeTicketDto> CreateAsync(WeighbridgeTicketCreateDto dto, CancellationToken ct = default)
    {
        var no = await _numbers.NextAsync("WB", "WB", ct);
        var second = dto.SecondWeight;
        var net = Math.Abs(dto.FirstWeight - second);
        var status = second > 0 ? "Completed" : "Open";
        var e = new WeighbridgeTicket
        {
            TicketNo = no, Date = dto.Date.Date,
            Direction = string.Equals(dto.Direction, "Out", StringComparison.OrdinalIgnoreCase) ? "Out" : "In",
            VehicleNo = dto.VehicleNo, PartyId = dto.PartyId, ProductId = dto.ProductId,
            FirstWeight = dto.FirstWeight, SecondWeight = second, NetWeight = net,
            Status = status, Remarks = dto.Remarks
        };
        _db.WeighbridgeTickets.Add(e);
        await _db.SaveChangesAsync(ct);
        return (await GetAllAsync(ct)).First(x => x.Id == e.Id);
    }

    public async Task<WeighbridgeTicketDto?> CompleteAsync(Guid id, WeighbridgeCompleteDto dto, CancellationToken ct = default)
    {
        var e = await _db.WeighbridgeTickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return null;
        e.SecondWeight = dto.SecondWeight;
        e.NetWeight = Math.Abs(e.FirstWeight - e.SecondWeight);
        e.Status = "Completed";
        await _db.SaveChangesAsync(ct);
        return (await GetAllAsync(ct)).First(x => x.Id == e.Id);
    }
}

public class PosService : IPosService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public PosService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<PosSaleDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.PosSales.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product).OrderByDescending(x => x.Date)
            .Select(x => new PosSaleDto(x.Id, x.BillNo, x.Date, x.PartyId, x.Party != null ? x.Party.Name : null, x.PaymentMode,
                x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.Status, x.Remarks,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new PosSaleLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .ToListAsync(ct);

    public async Task<PosSaleDto> CreateAsync(PosSaleCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Add at least one POS line.");
        var no = await _numbers.NextAsync("POS", "POS", ct);
        var lines = new List<PosSaleLine>();
        decimal sub = 0;
        var i = 1;
        foreach (var l in dto.Lines)
        {
            var product = await _db.Products.FirstAsync(p => p.Id == l.ProductId, ct);
            if (product.StockQty < l.Qty) throw new InvalidOperationException($"Insufficient stock for {product.Name}");
            var amount = Math.Round(l.Qty * l.Rate, 2);
            sub += amount;
            product.StockQty -= l.Qty;
            _db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id, Date = dto.Date.Date, QtyOut = l.Qty,
                ReferenceType = "POS", Remarks = no
            });
            lines.Add(new PosSaleLine { ProductId = product.Id, Qty = l.Qty, Rate = l.Rate, Amount = amount, LineNo = i++ });
        }
        var e = new PosSale
        {
            BillNo = no, Date = dto.Date.Date, PartyId = dto.PartyId,
            PaymentMode = string.IsNullOrWhiteSpace(dto.PaymentMode) ? "Cash" : dto.PaymentMode,
            SubTotal = sub, Discount = dto.Discount, Tax = dto.Tax,
            GrandTotal = sub - dto.Discount + dto.Tax, Status = "Posted", Remarks = dto.Remarks, Lines = lines
        };
        _db.PosSales.Add(e);
        await _db.SaveChangesAsync(ct);
        return (await GetAllAsync(ct)).First(x => x.Id == e.Id);
    }
}

public class StockTransferService : IStockTransferService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public StockTransferService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<StockTransferDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.StockTransfers.Include(x => x.FromWarehouse).Include(x => x.ToWarehouse).Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.Date)
            .Select(x => new StockTransferDto(x.Id, x.TransferNo, x.Date, x.FromWarehouseId, x.FromWarehouse!.Name, x.ToWarehouseId, x.ToWarehouse!.Name,
                x.Status, x.Remarks, x.Lines.OrderBy(l => l.LineNo).Select(l => new StockTransferLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty)).ToList()))
            .ToListAsync(ct);

    public async Task<StockTransferDto> CreateAsync(StockTransferCreateDto dto, CancellationToken ct = default)
    {
        if (dto.FromWarehouseId == dto.ToWarehouseId) throw new InvalidOperationException("From and To warehouses must differ.");
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Add at least one line.");
        _ = await _db.Warehouses.FirstAsync(x => x.Id == dto.FromWarehouseId, ct);
        _ = await _db.Warehouses.FirstAsync(x => x.Id == dto.ToWarehouseId, ct);
        var no = await _numbers.NextAsync("STK-TRF", "STK-TRF", ct);
        var lines = new List<StockTransferLine>();
        var i = 1;
        foreach (var l in dto.Lines)
        {
            if (l.Qty <= 0) throw new InvalidOperationException("Qty must be positive.");
            var product = await _db.Products.FirstAsync(x => x.Id == l.ProductId, ct);
            if (product.StockQty < l.Qty) throw new InvalidOperationException($"Insufficient stock for {product.Name}");
            lines.Add(new StockTransferLine { ProductId = product.Id, Qty = l.Qty, LineNo = i++ });
            _db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id, WarehouseId = dto.FromWarehouseId, Date = dto.Date.Date,
                QtyOut = l.Qty, ReferenceType = "StockTransfer", Remarks = $"{no} out"
            });
            _db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id, WarehouseId = dto.ToWarehouseId, Date = dto.Date.Date,
                QtyIn = l.Qty, ReferenceType = "StockTransfer", Remarks = $"{no} in"
            });
        }
        var e = new StockTransfer
        {
            TransferNo = no, Date = dto.Date.Date, FromWarehouseId = dto.FromWarehouseId, ToWarehouseId = dto.ToWarehouseId,
            Status = "Posted", Remarks = dto.Remarks, Lines = lines
        };
        _db.StockTransfers.Add(e);
        await _db.SaveChangesAsync(ct);
        return (await GetAllAsync(ct)).First(x => x.Id == e.Id);
    }
}

public class TradeReturnService : ITradeReturnService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    private readonly IVoucherService _vouchers;
    public TradeReturnService(AppDbContext db, NumberService numbers, IVoucherService vouchers)
    { _db = db; _numbers = numbers; _vouchers = vouchers; }

    public async Task<IReadOnlyList<TradeReturnDto>> GetSaleReturnsAsync(CancellationToken ct = default) =>
        await _db.SaleReturns.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product).OrderByDescending(x => x.Date)
            .Select(x => new TradeReturnDto(x.Id, x.ReturnNo, x.Date, x.PartyId, x.Party!.Name, "Sale", x.Status, x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new TradeReturnLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TradeReturnDto>> GetPurchaseReturnsAsync(CancellationToken ct = default) =>
        await _db.PurchaseReturns.Include(x => x.Party).Include(x => x.Lines).ThenInclude(l => l.Product).OrderByDescending(x => x.Date)
            .Select(x => new TradeReturnDto(x.Id, x.ReturnNo, x.Date, x.PartyId, x.Party!.Name, "Purchase", x.Status, x.SubTotal, x.Discount, x.Tax, x.GrandTotal, x.Remarks,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new TradeReturnLineDto(l.Id, l.ProductId, l.Product!.Name, l.Qty, l.Rate, l.Amount)).ToList()))
            .ToListAsync(ct);

    public async Task<TradeReturnDto> CreateSaleReturnAsync(TradeReturnCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Add at least one line.");
        var party = await _db.Parties.FirstAsync(x => x.Id == dto.PartyId, ct);
        var no = await _numbers.NextAsync("SR", "SR", ct);
        var lines = new List<SaleReturnLine>();
        decimal sub = 0;
        var i = 1;
        foreach (var l in dto.Lines)
        {
            var product = await _db.Products.FirstAsync(x => x.Id == l.ProductId, ct);
            var amount = Math.Round(l.Qty * l.Rate, 2);
            sub += amount;
            lines.Add(new SaleReturnLine { ProductId = product.Id, Qty = l.Qty, Rate = l.Rate, Amount = amount, LineNo = i++ });
            if (dto.Post)
            {
                product.StockQty += l.Qty;
                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id, Date = dto.Date.Date, QtyIn = l.Qty,
                    ReferenceType = "SaleReturn", Remarks = no
                });
            }
        }
        var e = new SaleReturn
        {
            ReturnNo = no, Date = dto.Date.Date, PartyId = party.Id, SubTotal = sub, Discount = dto.Discount, Tax = dto.Tax,
            GrandTotal = sub - dto.Discount + dto.Tax, Remarks = dto.Remarks,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft, Lines = lines
        };
        _db.SaleReturns.Add(e);
        await _db.SaveChangesAsync(ct);
        if (dto.Post)
        {
            var ar = await SaleService.EnsureSystemAccount(_db, "1100", "Accounts Receivable", AccountType.Asset, ct);
            var sales = await SaleService.EnsureSystemAccount(_db, "4100", "Sales Revenue", AccountType.Revenue, ct);
            await _vouchers.CreateAsync(new VoucherCreateDto(VoucherType.Sr, dto.Date, $"Sale return {no} - {party.Name}", true, new[]
            {
                new VoucherLineDto(null, sales.Id, null, e.GrandTotal, 0, party.Name),
                new VoucherLineDto(null, ar.Id, null, 0, e.GrandTotal, no)
            }), ct);
        }
        return (await GetSaleReturnsAsync(ct)).First(x => x.Id == e.Id);
    }

    public async Task<TradeReturnDto> CreatePurchaseReturnAsync(TradeReturnCreateDto dto, CancellationToken ct = default)
    {
        if (dto.Lines.Count == 0) throw new InvalidOperationException("Add at least one line.");
        var party = await _db.Parties.FirstAsync(x => x.Id == dto.PartyId, ct);
        var no = await _numbers.NextAsync("PR", "PR", ct);
        var lines = new List<PurchaseReturnLine>();
        decimal sub = 0;
        var i = 1;
        foreach (var l in dto.Lines)
        {
            var product = await _db.Products.FirstAsync(x => x.Id == l.ProductId, ct);
            var amount = Math.Round(l.Qty * l.Rate, 2);
            sub += amount;
            lines.Add(new PurchaseReturnLine { ProductId = product.Id, Qty = l.Qty, Rate = l.Rate, Amount = amount, LineNo = i++ });
            if (dto.Post)
            {
                if (product.StockQty < l.Qty) throw new InvalidOperationException($"Insufficient stock for {product.Name}");
                product.StockQty -= l.Qty;
                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id, Date = dto.Date.Date, QtyOut = l.Qty,
                    ReferenceType = "PurchaseReturn", Remarks = no
                });
            }
        }
        var e = new PurchaseReturn
        {
            ReturnNo = no, Date = dto.Date.Date, PartyId = party.Id, SubTotal = sub, Discount = dto.Discount, Tax = dto.Tax,
            GrandTotal = sub - dto.Discount + dto.Tax, Remarks = dto.Remarks,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft, Lines = lines
        };
        _db.PurchaseReturns.Add(e);
        await _db.SaveChangesAsync(ct);
        if (dto.Post)
        {
            var ap = await SaleService.EnsureSystemAccount(_db, "2100", "Accounts Payable", AccountType.Liability, ct);
            var purchases = await SaleService.EnsureSystemAccount(_db, "5100", "Purchases", AccountType.Expense, ct);
            await _vouchers.CreateAsync(new VoucherCreateDto(VoucherType.Pr, dto.Date, $"Purchase return {no} - {party.Name}", true, new[]
            {
                new VoucherLineDto(null, ap.Id, null, e.GrandTotal, 0, party.Name),
                new VoucherLineDto(null, purchases.Id, null, 0, e.GrandTotal, no)
            }), ct);
        }
        return (await GetPurchaseReturnsAsync(ct)).First(x => x.Id == e.Id);
    }
}

public class LeaveService : ILeaveService
{
    private readonly AppDbContext _db;
    private readonly NumberService _numbers;
    public LeaveService(AppDbContext db, NumberService numbers) { _db = db; _numbers = numbers; }

    public async Task<IReadOnlyList<LeaveDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.LeaveEntries.Include(x => x.Employee).OrderByDescending(x => x.FromDate)
            .Select(x => new LeaveDto(x.Id, x.DocNo, x.EmployeeId, x.Employee!.Name, x.FromDate, x.ToDate, x.LeaveType, x.Days, x.Status, x.Remarks))
            .ToListAsync(ct);

    public async Task<LeaveDto> CreateAsync(LeaveCreateDto dto, CancellationToken ct = default)
    {
        var emp = await _db.Employees.FirstAsync(x => x.Id == dto.EmployeeId, ct);
        if (dto.ToDate.Date < dto.FromDate.Date) throw new InvalidOperationException("To date must be on/after from date.");
        var days = (decimal)(dto.ToDate.Date - dto.FromDate.Date).TotalDays + 1;
        var no = await _numbers.NextAsync("LEAVE", "LEAVE", ct);
        var e = new LeaveEntry
        {
            DocNo = no, EmployeeId = emp.Id, FromDate = dto.FromDate.Date, ToDate = dto.ToDate.Date,
            LeaveType = string.IsNullOrWhiteSpace(dto.LeaveType) ? "Casual" : dto.LeaveType,
            Days = days, Status = "Approved", Remarks = dto.Remarks
        };
        _db.LeaveEntries.Add(e);
        await _db.SaveChangesAsync(ct);
        return new LeaveDto(e.Id, e.DocNo, e.EmployeeId, emp.Name, e.FromDate, e.ToDate, e.LeaveType, e.Days, e.Status, e.Remarks);
    }
}

public class SalarySheetService : ISalarySheetService
{
    private readonly AppDbContext _db;
    public SalarySheetService(AppDbContext db) => _db = db;

    public async Task<SalarySheetDto> GetAsync(int year, int month, CancellationToken ct = default)
    {
        var lines = await _db.SalaryPayments.Include(x => x.Employee)
            .Where(x => x.Year == year && x.Month == month)
            .OrderBy(x => x.Employee!.Name)
            .Select(x => new SalarySheetLineDto(x.Id, x.EmployeeId, x.Employee!.Name, x.Employee.Department, x.Year, x.Month,
                x.Basic, x.Allowances, x.Deductions, x.NetSalary, x.Status.ToString(), x.PaidOn))
            .ToListAsync(ct);
        return new SalarySheetDto(year, month, lines.Count, lines.Sum(l => l.NetSalary), lines);
    }

    public async Task<SalarySheetDto> GenerateAsync(SalaryGenerateDto dto, CancellationToken ct = default)
    {
        if (dto.Month is < 1 or > 12) throw new InvalidOperationException("Month must be 1-12.");
        var existing = await _db.SalaryPayments.Where(x => x.Year == dto.Year && x.Month == dto.Month).Select(x => x.EmployeeId).ToListAsync(ct);
        var employees = await _db.Employees.Where(x => x.IsActive).ToListAsync(ct);
        foreach (var emp in employees.Where(e => !existing.Contains(e.Id)))
        {
            var allow = Math.Round(emp.BasicSalary * dto.DefaultAllowancePercent / 100m, 2);
            var ded = Math.Round(emp.BasicSalary * dto.DefaultDeductionPercent / 100m, 2);
            _db.SalaryPayments.Add(new SalaryPayment
            {
                EmployeeId = emp.Id, Year = dto.Year, Month = dto.Month,
                Basic = emp.BasicSalary, Allowances = allow, Deductions = ded,
                NetSalary = emp.BasicSalary + allow - ded, Status = DocumentStatus.Draft
            });
        }
        await _db.SaveChangesAsync(ct);
        return await GetAsync(dto.Year, dto.Month, ct);
    }

    public async Task<SalarySheetLineDto?> MarkPaidAsync(Guid id, CancellationToken ct = default)
    {
        var x = await _db.SalaryPayments.Include(p => p.Employee).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (x is null) return null;
        x.Status = DocumentStatus.Posted;
        x.PaidOn = DateTime.UtcNow.Date;
        await _db.SaveChangesAsync(ct);
        return new SalarySheetLineDto(x.Id, x.EmployeeId, x.Employee!.Name, x.Employee.Department, x.Year, x.Month,
            x.Basic, x.Allowances, x.Deductions, x.NetSalary, x.Status.ToString(), x.PaidOn);
    }
}
