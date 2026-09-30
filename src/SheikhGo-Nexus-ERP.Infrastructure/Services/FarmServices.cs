using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class FarmService(
    AppDbContext db,
    IAuditService audit,
    IInventoryPostingService inventory,
    IAccountingPostingService accounting,
    IPermissionService permissions) : IFarmService
{
    public async Task<IReadOnlyList<FarmDto>> GetFarmsAsync(CancellationToken ct = default)
    {
        var rows = await db.Farms.Include(x => x.Units).OrderBy(x => x.Code).ToListAsync(ct);
        return rows.Select(MapFarm).ToList();
    }

    public async Task<FarmDto> CreateFarmAsync(FarmUpsertDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Farm.Create", ct);
        if (!Enum.TryParse<FarmType>(dto.FarmType, true, out var farmType))
            farmType = FarmType.Poultry;
        var farm = new Farm
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            FarmType = farmType,
            BranchId = dto.BranchId,
            IsActive = dto.IsActive
        };
        db.Farms.Add(farm);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(Farm), farm.Id, "Created", null, farm.Code, ct);
        return MapFarm(await db.Farms.Include(x => x.Units).FirstAsync(x => x.Id == farm.Id, ct));
    }

    public async Task<FarmUnitDto> CreateUnitAsync(FarmUnitUpsertDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Farm.Edit", ct);
        _ = await db.Farms.FirstOrDefaultAsync(x => x.Id == dto.FarmId, ct)
            ?? throw new InvalidOperationException("Farm not found.");
        var unit = new FarmUnit
        {
            FarmId = dto.FarmId,
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Capacity = dto.Capacity,
            IsActive = dto.IsActive
        };
        db.FarmUnits.Add(unit);
        await db.SaveChangesAsync(ct);
        return new FarmUnitDto(unit.Id, unit.FarmId, unit.Code, unit.Name, unit.Capacity, unit.IsActive);
    }

    public async Task<IReadOnlyList<FlockDto>> GetFlocksAsync(CancellationToken ct = default)
    {
        var rows = await db.Flocks.Include(x => x.Farm).Include(x => x.FarmUnit)
            .OrderByDescending(x => x.PlacementDate).ToListAsync(ct);
        return rows.Select(MapFlock).ToList();
    }

    public async Task<FlockDto?> GetFlockByIdAsync(Guid id, CancellationToken ct = default)
    {
        var f = await db.Flocks.Include(x => x.Farm).Include(x => x.FarmUnit).FirstOrDefaultAsync(x => x.Id == id, ct);
        return f is null ? null : MapFlock(f);
    }

    public async Task<FlockDto> CreateFlockAsync(FlockCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Flock.Create", ct);
        if (dto.QtyPlaced <= 0) throw new InvalidOperationException("QtyPlaced must be positive.");
        _ = await db.Farms.FirstOrDefaultAsync(x => x.Id == dto.FarmId, ct)
            ?? throw new InvalidOperationException("Farm not found.");
        if (dto.FarmUnitId is Guid uid)
            _ = await db.FarmUnits.FirstOrDefaultAsync(x => x.Id == uid && x.FarmId == dto.FarmId, ct)
                ?? throw new InvalidOperationException("Farm unit not found for this farm.");

        var flock = new Flock
        {
            FarmId = dto.FarmId,
            FarmUnitId = dto.FarmUnitId,
            BatchNo = dto.BatchNo.Trim().ToUpperInvariant(),
            BirdType = dto.BirdType.Trim(),
            PlacementDate = dto.PlacementDate.Date,
            QtyPlaced = dto.QtyPlaced,
            QtyCurrent = dto.QtyPlaced,
            QtyMortality = 0,
            Status = FlockStatus.Active,
            Remarks = dto.Remarks
        };
        db.Flocks.Add(flock);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(Flock), flock.Id, "Created", null, flock.BatchNo, ct);
        return (await GetFlockByIdAsync(flock.Id, ct))!;
    }

    public async Task<bool> CloseFlockAsync(Guid flockId, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Flock.Edit", ct);
        var flock = await db.Flocks.FirstOrDefaultAsync(x => x.Id == flockId, ct);
        if (flock is null) return false;
        flock.Status = FlockStatus.Closed;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(Flock), flock.Id, "Closed", null, flock.BatchNo, ct);
        return true;
    }

    public async Task<MortalityDto> AddMortalityAsync(Guid flockId, MortalityCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Farm.Mortality.Create", ct);
        var flock = await db.Flocks.FirstOrDefaultAsync(x => x.Id == flockId, ct)
            ?? throw new InvalidOperationException("Flock not found.");
        if (flock.Status != FlockStatus.Active)
            throw new InvalidOperationException("Flock is not active.");
        if (dto.Qty <= 0) throw new InvalidOperationException("Qty must be positive.");
        if (dto.Qty > flock.QtyCurrent)
            throw new InvalidOperationException("Mortality exceeds current flock quantity.");

        var rec = new MortalityRecord
        {
            FlockId = flockId,
            Date = dto.Date.Date,
            Qty = dto.Qty,
            Reason = dto.Reason
        };
        flock.QtyMortality += dto.Qty;
        flock.QtyCurrent -= dto.Qty;
        db.MortalityRecords.Add(rec);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(MortalityRecord), rec.Id, "Created", null, $"{dto.Qty} on {flock.BatchNo}", ct);
        return new MortalityDto(rec.Id, flockId, rec.Date, rec.Qty, rec.Reason);
    }

    public async Task<IReadOnlyList<MortalityDto>> GetMortalitiesAsync(Guid flockId, CancellationToken ct = default)
    {
        var rows = await db.MortalityRecords.Where(x => x.FlockId == flockId).OrderByDescending(x => x.Date).ToListAsync(ct);
        return rows.Select(r => new MortalityDto(r.Id, r.FlockId, r.Date, r.Qty, r.Reason)).ToList();
    }

    public async Task<IReadOnlyList<FeedIssueDto>> GetFeedIssuesAsync(CancellationToken ct = default)
    {
        var rows = await db.FeedIssues
            .Include(x => x.Farm).Include(x => x.Flock).Include(x => x.Warehouse)
            .Include(x => x.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(x => x.IssueDate).ToListAsync(ct);
        return rows.Select(MapFeed).ToList();
    }

    public async Task<FeedIssueDto> CreateFeedIssueAsync(FeedIssueCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Farm.Feed.Issue", ct);
        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new InvalidOperationException("Feed issue requires lines.");
        _ = await db.Farms.FirstOrDefaultAsync(x => x.Id == dto.FarmId, ct)
            ?? throw new InvalidOperationException("Farm not found.");
        _ = await db.Flocks.FirstOrDefaultAsync(x => x.Id == dto.FlockId && x.FarmId == dto.FarmId, ct)
            ?? throw new InvalidOperationException("Flock not found for farm.");
        _ = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == dto.WarehouseId, ct)
            ?? throw new InvalidOperationException("Warehouse not found.");

        var (company, branch, fy, dt, no) = await NextDocAsync("FI", "FI", ct);
        var doc = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = (dto.IssueDate ?? DateTime.UtcNow).Date, Narration = dto.Remarks
        };
        var issue = new FeedIssue
        {
            Document = doc,
            IssueNo = no,
            IssueDate = (dto.IssueDate ?? DateTime.UtcNow).Date,
            FarmId = dto.FarmId,
            FlockId = dto.FlockId,
            WarehouseId = dto.WarehouseId,
            Status = DocumentStatus.Draft,
            Remarks = dto.Remarks
        };
        var lineNo = 1;
        foreach (var l in dto.Lines.Where(x => x.Qty > 0))
        {
            issue.Lines.Add(new FeedIssueLine
            {
                LineNo = lineNo++,
                ProductId = l.ProductId,
                Qty = l.Qty
            });
        }
        db.FeedIssues.Add(issue);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(FeedIssue), issue.Id, "Created", null, no, ct);
        return (await GetFeedIssuesAsync(ct)).First(x => x.Id == issue.Id);
    }

    public async Task<FeedIssueDto?> TransitionFeedIssueAsync(Guid id, string action, CancellationToken ct = default)
    {
        var issue = await db.FeedIssues.Include(x => x.Lines).Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (issue is null) return null;

        var key = action.Trim().ToLowerInvariant();
        switch (key)
        {
            case "submit":
                if (issue.Status != DocumentStatus.Draft) throw new InvalidOperationException("Only draft can be submitted.");
                issue.Status = DocumentStatus.Submitted;
                break;
            case "approve":
                if (issue.Status is not (DocumentStatus.Draft or DocumentStatus.Submitted))
                    throw new InvalidOperationException($"Cannot approve from {issue.Status}.");
                issue.Status = DocumentStatus.Approved;
                break;
            case "post":
                if (issue.Status is not (DocumentStatus.Approved or DocumentStatus.Submitted or DocumentStatus.Draft))
                    throw new InvalidOperationException($"Cannot post from {issue.Status}.");
                await inventory.PostFarmFeedIssueAsync(issue, ct);
                issue.Status = DocumentStatus.Posted;
                break;
            case "cancel":
                if (issue.Status == DocumentStatus.Posted)
                    throw new InvalidOperationException("Cannot cancel posted feed issue.");
                issue.Status = DocumentStatus.Cancelled;
                break;
            default:
                throw new InvalidOperationException($"Unknown action '{action}'.");
        }
        if (issue.Document is not null) issue.Document.Status = issue.Status;
        else if (issue.DocumentId is Guid did)
        {
            var doc = await db.Documents.FirstOrDefaultAsync(x => x.Id == did, ct);
            if (doc is not null) doc.Status = issue.Status;
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(FeedIssue), issue.Id, key, null, issue.Status.ToString(), ct);
        return (await GetFeedIssuesAsync(ct)).FirstOrDefault(x => x.Id == id);
    }

    public async Task<IReadOnlyList<FarmTreatmentDto>> GetTreatmentsAsync(Guid? flockId = null, CancellationToken ct = default)
    {
        var q = db.FarmTreatments.Include(x => x.Flock).Include(x => x.Product).AsQueryable();
        if (flockId is Guid fid) q = q.Where(x => x.FlockId == fid);
        var rows = await q.OrderByDescending(x => x.Date).ToListAsync(ct);
        return rows.Select(MapTreatment).ToList();
    }

    public async Task<FarmTreatmentDto> CreateTreatmentAsync(FarmTreatmentCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Farm.Treatment.Create", ct);
        _ = await db.Flocks.FirstOrDefaultAsync(x => x.Id == dto.FlockId, ct)
            ?? throw new InvalidOperationException("Flock not found.");
        if (!Enum.TryParse<FarmTreatmentKind>(dto.Kind, true, out var kind))
            throw new InvalidOperationException("Kind must be Vaccination or Medication.");

        Document? doc = null;
        string no = $"TRT-{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (dto.ProductId is not null && dto.Qty > 0 && dto.WarehouseId is not null)
        {
            var numbered = await NextDocAsync("FMT", "FMT", ct);
            no = numbered.No;
            doc = new Document
            {
                DocumentTypeId = numbered.Dt.Id, CompanyId = numbered.Company.Id, BranchId = numbered.Branch.Id,
                FinancialYearId = numbered.Fy.Id, DocumentNo = no, DocumentDate = dto.Date.Date, Narration = dto.Narration
            };
        }

        var trt = new FarmTreatment
        {
            Document = doc,
            TreatmentNo = no,
            FlockId = dto.FlockId,
            Date = dto.Date.Date,
            Kind = kind,
            ProductId = dto.ProductId,
            WarehouseId = dto.WarehouseId,
            Qty = dto.Qty,
            BatchNo = dto.BatchNo,
            Narration = dto.Narration,
            Status = DocumentStatus.Draft
        };
        db.FarmTreatments.Add(trt);
        await db.SaveChangesAsync(ct);

        if (dto.Post && trt.ProductId is not null && trt.WarehouseId is not null && trt.Qty > 0)
        {
            await inventory.PostFarmMedicationAsync(trt, ct);
            trt.Status = DocumentStatus.Posted;
            if (trt.Document is not null) trt.Document.Status = DocumentStatus.Posted;
            await db.SaveChangesAsync(ct);
        }

        await audit.WriteAsync(nameof(FarmTreatment), trt.Id, "Created", null, no, ct);
        return (await GetTreatmentsAsync(dto.FlockId, ct)).First(x => x.Id == trt.Id);
    }

    public async Task<IReadOnlyList<FarmExpenseDto>> GetExpensesAsync(CancellationToken ct = default)
    {
        var rows = await db.FarmExpenses.Include(x => x.Farm).OrderByDescending(x => x.ExpenseDate).ToListAsync(ct);
        return rows.Select(MapExpense).ToList();
    }

    public async Task<FarmExpenseDto> CreateExpenseAsync(FarmExpenseCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Farm.Expense.Create", ct);
        if (dto.Amount <= 0) throw new InvalidOperationException("Amount must be positive.");
        if (!Enum.TryParse<FarmExpenseCategory>(dto.Category, true, out var cat))
            throw new InvalidOperationException("Invalid expense category.");
        _ = await db.Farms.FirstOrDefaultAsync(x => x.Id == dto.FarmId, ct)
            ?? throw new InvalidOperationException("Farm not found.");

        var (company, branch, fy, dt, no) = await NextDocAsync("FEX", "FEX", ct);
        var doc = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = dto.ExpenseDate.Date, Narration = dto.Narration
        };
        var exp = new FarmExpense
        {
            Document = doc,
            ExpenseNo = no,
            ExpenseDate = dto.ExpenseDate.Date,
            FarmId = dto.FarmId,
            FlockId = dto.FlockId,
            Category = cat,
            Amount = dto.Amount,
            CashBankAccountId = dto.CashBankAccountId,
            Status = DocumentStatus.Draft,
            Narration = dto.Narration
        };
        db.FarmExpenses.Add(exp);
        await db.SaveChangesAsync(ct);

        if (dto.Post)
        {
            await accounting.PostFarmExpenseAsync(exp, ct);
            exp.Status = DocumentStatus.Posted;
            if (exp.Document is not null) exp.Document.Status = DocumentStatus.Posted;
            await db.SaveChangesAsync(ct);
        }

        await audit.WriteAsync(nameof(FarmExpense), exp.Id, "Created", null, no, ct);
        return MapExpense(await db.FarmExpenses.Include(x => x.Farm).FirstAsync(x => x.Id == exp.Id, ct));
    }

    public async Task<FarmExpenseDto?> TransitionExpenseAsync(Guid id, string action, CancellationToken ct = default)
    {
        var exp = await db.FarmExpenses.Include(x => x.Document).Include(x => x.Farm)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (exp is null) return null;
        var key = action.Trim().ToLowerInvariant();
        if (key is "post")
        {
            if (exp.Status == DocumentStatus.Posted) return MapExpense(exp);
            await accounting.PostFarmExpenseAsync(exp, ct);
            exp.Status = DocumentStatus.Posted;
            if (exp.Document is not null) exp.Document.Status = DocumentStatus.Posted;
        }
        else if (key is "cancel")
        {
            if (exp.Status == DocumentStatus.Posted)
                throw new InvalidOperationException("Cannot cancel posted expense.");
            exp.Status = DocumentStatus.Cancelled;
            if (exp.Document is not null) exp.Document.Status = DocumentStatus.Cancelled;
        }
        else throw new InvalidOperationException($"Unknown action '{action}'.");
        await db.SaveChangesAsync(ct);
        return MapExpense(exp);
    }

    public async Task<FarmPerformanceDto?> GetPerformanceAsync(Guid flockId, CancellationToken ct = default)
    {
        var flock = await db.Flocks.FirstOrDefaultAsync(x => x.Id == flockId, ct);
        if (flock is null) return null;

        var feed = await db.FeedIssues.Where(x => x.FlockId == flockId && x.Status == DocumentStatus.Posted)
            .SelectMany(x => x.Lines).SumAsync(l => (decimal?)l.Qty, ct) ?? 0m;
        var expense = await db.FarmExpenses.Where(x => x.FlockId == flockId && x.Status == DocumentStatus.Posted)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var mortPct = flock.QtyPlaced > 0 ? Math.Round(100m * flock.QtyMortality / flock.QtyPlaced, 2) : 0m;
        decimal? costPer = flock.QtyCurrent > 0 ? Math.Round(expense / flock.QtyCurrent, 4) : null;

        return new FarmPerformanceDto(
            flock.Id, flock.BatchNo, flock.QtyPlaced, flock.QtyPlaced, flock.QtyMortality, flock.QtyCurrent,
            mortPct, feed, expense, costPer);
    }

    public async Task<FarmEventDto> AddEventAsync(Guid flockId, FarmEventCreateDto dto, CancellationToken ct = default)
    {
        if (dto.EventType.Equals("Mortality", StringComparison.OrdinalIgnoreCase))
        {
            var m = await AddMortalityAsync(flockId, new MortalityCreateDto(dto.Date, (int)dto.Qty, dto.Remarks), ct);
            return new FarmEventDto(m.Id, flockId, m.Date, "Mortality", m.Qty, 0, null, m.Reason);
        }
        throw new InvalidOperationException("Use dedicated mortality/feed/treatment endpoints.");
    }

    public async Task<IReadOnlyList<FarmEventDto>> GetEventsAsync(Guid flockId, CancellationToken ct = default)
    {
        var morts = await GetMortalitiesAsync(flockId, ct);
        return morts.Select(m => new FarmEventDto(m.Id, flockId, m.Date, "Mortality", m.Qty, 0, null, m.Reason)).ToList();
    }

    private async Task<(Company Company, Branch Branch, FinancialYear Fy, DocumentType Dt, string No)> NextDocAsync(
        string code, string prefix, CancellationToken ct)
    {
        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == code, ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? prefix, fy.StartDate.Year, ct);
        return (company, branch, fy, dt, no);
    }

    private static FarmDto MapFarm(Farm f) => new(
        f.Id, f.Code, f.Name, f.FarmType.ToString(), f.BranchId, f.IsActive,
        f.Units.Select(u => new FarmUnitDto(u.Id, u.FarmId, u.Code, u.Name, u.Capacity, u.IsActive)).ToList());

    private static FlockDto MapFlock(Flock f) => new(
        f.Id, f.BatchNo, f.FarmId, f.Farm?.Name, f.FarmUnitId, f.FarmUnit?.Name, f.BirdType,
        f.PlacementDate, f.QtyPlaced, f.QtyCurrent, f.QtyMortality, f.Status.ToString(), f.Remarks);

    private static FeedIssueDto MapFeed(FeedIssue i) => new(
        i.Id, i.IssueNo, i.IssueDate, i.FarmId, i.Farm?.Name, i.FlockId, i.Flock?.BatchNo,
        i.WarehouseId, i.Warehouse?.Name, i.Status, i.Remarks,
        i.Lines.Select(l => new FeedIssueLineDto(l.Id, l.ProductId, l.Product?.Code, l.Product?.Name, l.Qty, l.UnitCost, l.TotalCost)).ToList());

    private static FarmTreatmentDto MapTreatment(FarmTreatment t) => new(
        t.Id, t.TreatmentNo, t.FlockId, t.Flock?.BatchNo, t.Date, t.Kind.ToString(),
        t.ProductId, t.Product?.Name, t.WarehouseId, t.Qty, t.BatchNo, t.Narration, t.Status);

    private static FarmExpenseDto MapExpense(FarmExpense e) => new(
        e.Id, e.ExpenseNo, e.ExpenseDate, e.FarmId, e.Farm?.Name, e.FlockId,
        e.Category.ToString(), e.Amount, e.CashBankAccountId, e.Status, e.Narration);
}
