using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class OperationsService(
    AppDbContext db,
    IAuditService audit,
    IPermissionService permissions) : IOperationsService
{
    public async Task<IReadOnlyList<VehicleTypeDto>> GetVehicleTypesAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Operations.Vehicle.View", ct);
        return await db.VehicleTypes.OrderBy(x => x.Code)
            .Select(x => new VehicleTypeDto(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<VehicleTypeDto> CreateVehicleTypeAsync(VehicleTypeDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Operations.Vehicle.Create", ct);
        var row = new VehicleType
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            IsActive = dto.IsActive
        };
        db.VehicleTypes.Add(row);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(VehicleType), row.Id, "Created", null, row.Code, ct);
        return new VehicleTypeDto(row.Id, row.Code, row.Name, row.IsActive);
    }

    public async Task<IReadOnlyList<VehicleDto>> GetVehiclesAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Operations.Vehicle.View", ct);
        return await db.Vehicles.Include(x => x.VehicleType).OrderBy(x => x.Code)
            .Select(x => new VehicleDto(x.Id, x.Code, x.VehicleTypeId, x.VehicleType!.Name, x.Make, x.Model, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<VehicleDto> CreateVehicleAsync(VehicleDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Operations.Vehicle.Create", ct);
        _ = await db.VehicleTypes.FirstOrDefaultAsync(x => x.Id == dto.VehicleTypeId, ct)
            ?? throw new InvalidOperationException("Vehicle type not found.");
        var row = new Vehicle
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            VehicleTypeId = dto.VehicleTypeId,
            Make = dto.Make,
            Model = dto.Model,
            IsActive = dto.IsActive
        };
        db.Vehicles.Add(row);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(Vehicle), row.Id, "Created", null, row.Code, ct);
        var typeName = await db.VehicleTypes.Where(x => x.Id == row.VehicleTypeId).Select(x => x.Name).FirstAsync(ct);
        return new VehicleDto(row.Id, row.Code, row.VehicleTypeId, typeName, row.Make, row.Model, row.IsActive);
    }

    public async Task<IReadOnlyList<DriverDto>> GetDriversAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Operations.Driver.View", ct);
        return await db.Drivers.OrderBy(x => x.Code)
            .Select(x => new DriverDto(x.Id, x.Code, x.Name, x.EmployeeId, x.LicenseNo, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<DriverDto> CreateDriverAsync(DriverDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Operations.Driver.Create", ct);
        var row = new Driver
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            EmployeeId = dto.EmployeeId,
            LicenseNo = dto.LicenseNo,
            IsActive = dto.IsActive
        };
        db.Drivers.Add(row);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(Driver), row.Id, "Created", null, row.Code, ct);
        return new DriverDto(row.Id, row.Code, row.Name, row.EmployeeId, row.LicenseNo, row.IsActive);
    }

    public async Task<IReadOnlyList<GatePassDto>> GetGatePassesAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Operations.GatePass.View", ct);
        return await db.GatePasses.Include(x => x.Vehicle).OrderByDescending(x => x.PassDate)
            .Select(x => new GatePassDto(
                x.Id, x.GatePassNo, x.Direction.ToString(), x.PassDate, x.VehicleId, x.Vehicle != null ? x.Vehicle.Code : null,
                x.WeighmentId, x.SourceDocType, x.SourceEntityId, x.Status, x.Remarks))
            .ToListAsync(ct);
    }

    public async Task<GatePassDto> CreateGatePassAsync(GatePassDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Operations.GatePass.Create", ct);
        if (!Enum.TryParse<GatePassDirection>(dto.Direction, true, out var direction))
            direction = GatePassDirection.Outward;
        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "GP", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "GP", fy.StartDate.Year, ct);
        var document = new Document
        {
            DocumentTypeId = dt.Id,
            CompanyId = company.Id,
            BranchId = branch.Id,
            FinancialYearId = fy.Id,
            DocumentNo = no,
            DocumentDate = dto.PassDate.Date,
            Narration = dto.Remarks
        };
        var gp = new GatePass
        {
            GatePassNo = no,
            Direction = direction,
            PassDate = dto.PassDate.Date,
            VehicleId = dto.VehicleId,
            DriverId = null,
            SourceDocType = dto.SourceDocType,
            SourceEntityId = dto.SourceEntityId,
            WeighmentId = dto.WeighmentId,
            Status = DocumentStatus.Draft,
            Remarks = dto.Remarks,
            Document = document
        };
        db.GatePasses.Add(gp);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(GatePass), gp.Id, "Created", null, gp.GatePassNo, ct);
        return (await GetGatePassesAsync(ct)).First(x => x.Id == gp.Id);
    }

    public async Task<GatePassDto?> TransitionGatePassAsync(Guid id, string action, CancellationToken ct = default)
    {
        var gp = await db.GatePasses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (gp is null) return null;
        var act = action.Trim().ToLowerInvariant();
        switch (act)
        {
            case "submit":
                if (gp.Status != DocumentStatus.Draft) throw new InvalidOperationException("Only draft gate passes can be submitted.");
                gp.Status = DocumentStatus.Submitted;
                break;
            case "approve":
                await permissions.EnsureAsync("Operations.GatePass.Approve", ct);
                if (gp.Status is not DocumentStatus.Submitted and not DocumentStatus.Draft)
                    throw new InvalidOperationException("Gate pass cannot be approved in current status.");
                gp.Status = DocumentStatus.Approved;
                break;
            case "post":
                await permissions.EnsureAsync("Operations.GatePass.Post", ct);
                if (gp.Status is not DocumentStatus.Approved and not DocumentStatus.Submitted)
                    throw new InvalidOperationException("Gate pass must be approved before post.");
                gp.Status = DocumentStatus.Posted;
                break;
            case "cancel":
                if (gp.Status == DocumentStatus.Posted) throw new InvalidOperationException("Posted gate passes cannot be cancelled.");
                gp.Status = DocumentStatus.Cancelled;
                break;
            default:
                throw new InvalidOperationException($"Unknown action '{action}'.");
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(GatePass), gp.Id, act, null, gp.Status.ToString(), ct);
        return (await GetGatePassesAsync(ct)).First(x => x.Id == id);
    }

    private async Task<(Company company, Branch branch, FinancialYear fy)> ScopeAsync(CancellationToken ct)
    {
        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        return (company, branch, fy);
    }
}

public class WeighbridgeService(
    AppDbContext db,
    IAuditService audit,
    IPermissionService permissions) : IWeighbridgeService
{
    public async Task<IReadOnlyList<WeighbridgeScaleDto>> GetScalesAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Weighbridge.Scale.View", ct);
        return await db.WeighbridgeScales.OrderBy(x => x.Code)
            .Select(x => new WeighbridgeScaleDto(x.Id, x.Code, x.Name, x.Location, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WeighbridgeTicketDto>> GetAllAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Weighbridge.Ticket.View", ct);
        return await MapListAsync(ct);
    }

    public async Task<WeighbridgeTicketDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Weighbridge.Ticket.View", ct);
        return (await MapListAsync(ct)).FirstOrDefault(x => x.Id == id);
    }

    public async Task<WeighbridgeTicketDto> CreateAsync(WeighbridgeTicketCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Weighbridge.Ticket.Create", ct);
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == dto.VehicleId, ct)
            ?? throw new InvalidOperationException("Vehicle not found.");
        if (!Enum.TryParse<WeighmentDirection>(dto.Direction, true, out var direction))
            direction = WeighmentDirection.Inward;
        var (company, branch, fy) = await ScopeAsync(ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "WBT", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "WBT", fy.StartDate.Year, ct);
        var document = new Document
        {
            DocumentTypeId = dt.Id,
            CompanyId = company.Id,
            BranchId = branch.Id,
            FinancialYearId = fy.Id,
            DocumentNo = no,
            DocumentDate = DateTime.UtcNow.Date,
            PartyId = dto.PartyId,
            Narration = dto.Remarks
        };
        var w = new Weighment
        {
            TicketNo = no,
            ScaleId = dto.ScaleId,
            VehicleId = vehicle.Id,
            VehicleNo = vehicle.Code,
            PartyId = dto.PartyId,
            ProductId = dto.ProductId,
            Direction = direction,
            Status = WeighmentStatus.Draft,
            Remarks = dto.Remarks,
            Document = document
        };
        db.Weighments.Add(w);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(Weighment), w.Id, "Created", null, w.TicketNo, ct);
        return (await GetByIdAsync(w.Id, ct))!;
    }

    public async Task<WeighbridgeTicketDto?> RecordFirstWeightAsync(Guid id, WeighbridgeFirstWeightDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Weighbridge.Ticket.Weigh", ct);
        var w = await db.Weighments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null) return null;
        if (w.Status is WeighmentStatus.Completed or WeighmentStatus.Cancelled)
            throw new InvalidOperationException("Completed or cancelled tickets cannot be edited.");
        if (w.Status != WeighmentStatus.Draft)
            throw new InvalidOperationException("First weight already recorded.");
        if (dto.FirstWeight <= 0)
            throw new InvalidOperationException("First weight must be greater than zero.");
        w.FirstWeight = dto.FirstWeight;
        w.FirstWeightAt = DateTime.UtcNow;
        w.Status = WeighmentStatus.FirstTaken;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(Weighment), w.Id, "FirstWeight", null, dto.FirstWeight.ToString("0.####"), ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<WeighbridgeTicketDto?> CompleteAsync(Guid id, WeighbridgeCompleteDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Weighbridge.Ticket.Complete", ct);
        var w = await db.Weighments.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (w is null) return null;
        if (w.Status == WeighmentStatus.Cancelled)
            throw new InvalidOperationException("Cancelled tickets cannot be completed.");
        if (w.Status == WeighmentStatus.Draft)
            throw new InvalidOperationException("Record first weight before second weight.");
        if (w.Status == WeighmentStatus.Completed && w.GatePassId is not null)
            throw new InvalidOperationException("Ticket already completed.");

        if (w.Status == WeighmentStatus.FirstTaken)
        {
            if (dto.SecondWeight <= 0)
                throw new InvalidOperationException("Second weight must be greater than zero.");
            var gross = Math.Max(w.FirstWeight, dto.SecondWeight);
            var tare = Math.Min(w.FirstWeight, dto.SecondWeight);
            var net = gross - tare;
            if (net <= 0)
                throw new InvalidOperationException("Net weight must be greater than zero.");
            w.SecondWeight = dto.SecondWeight;
            w.SecondWeightAt = DateTime.UtcNow;
            w.NetWeight = net;
            w.Status = WeighmentStatus.Completed;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(nameof(Weighment), w.Id, "SecondWeight",
                $"First={w.FirstWeight}", $"Second={dto.SecondWeight};Net={net}", ct);
        }

        if (w.GatePassId is null)
        {
            var gpDirection = w.Direction == WeighmentDirection.Inward
                ? GatePassDirection.Inward
                : GatePassDirection.Outward;
            var (company, branch, fy) = await ScopeAsync(ct);
            var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "GP", ct);
            var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "GP", fy.StartDate.Year, ct);
            var document = new Document
            {
                DocumentTypeId = dt.Id,
                CompanyId = company.Id,
                BranchId = branch.Id,
                FinancialYearId = fy.Id,
                DocumentNo = no,
                DocumentDate = DateTime.UtcNow.Date,
                PartyId = w.PartyId,
                Narration = $"From weighment {w.TicketNo}"
            };
            var gp = new GatePass
            {
                GatePassNo = no,
                Direction = gpDirection,
                PassDate = DateTime.UtcNow.Date,
                VehicleId = w.VehicleId,
                WeighmentId = w.Id,
                SourceDocType = "WBT",
                SourceEntityId = w.Id,
                Status = DocumentStatus.Draft,
                Remarks = w.Remarks,
                Document = document
            };
            db.GatePasses.Add(gp);
            await db.SaveChangesAsync(ct);
            w.GatePassId = gp.Id;
            w.LinkedDocumentType = "GP";
            w.LinkedDocumentId = gp.Id;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(nameof(Weighment), w.Id, "Completed", null, $"GP={gp.GatePassNo};Net={w.NetWeight}", ct);
            await audit.WriteAsync(nameof(GatePass), gp.Id, "Created", null, $"From {w.TicketNo}", ct);
        }

        return await GetByIdAsync(id, ct);
    }

    private async Task<IReadOnlyList<WeighbridgeTicketDto>> MapListAsync(CancellationToken ct) =>
        await db.Weighments.Include(x => x.Party).Include(x => x.Product)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new WeighbridgeTicketDto(
                x.Id, x.TicketNo, x.CreatedAt, x.Direction.ToString(), x.VehicleNo, x.VehicleId,
                x.PartyId, x.Party != null ? x.Party.Name : null,
                x.ProductId, x.Product != null ? x.Product.Name : null,
                x.FirstWeight, x.SecondWeight, x.NetWeight, x.Status.ToString(), x.GatePassId, x.Remarks))
            .ToListAsync(ct);

    private async Task<(Company company, Branch branch, FinancialYear fy)> ScopeAsync(CancellationToken ct)
    {
        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        return (company, branch, fy);
    }
}

public class PosService(
    AppDbContext db,
    IAuditService audit,
    IPermissionService permissions,
    ISalesService sales,
    ITenantContext tenant) : IPosService
{
    public async Task<IReadOnlyList<PosTerminalDto>> GetTerminalsAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("POS.Terminal.View", ct);
        return await db.PosTerminals.Include(x => x.Warehouse).OrderBy(x => x.Code)
            .Select(x => new PosTerminalDto(x.Id, x.Code, x.Name, x.WarehouseId, x.Warehouse!.Name, x.BranchId, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<PosTerminalDto> CreateTerminalAsync(PosTerminalDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("POS.Terminal.Create", ct);
        _ = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == dto.WarehouseId, ct)
            ?? throw new InvalidOperationException("Warehouse not found.");
        var row = new PosTerminal
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            WarehouseId = dto.WarehouseId,
            BranchId = dto.BranchId,
            IsActive = dto.IsActive
        };
        db.PosTerminals.Add(row);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(PosTerminal), row.Id, "Created", null, row.Code, ct);
        return (await GetTerminalsAsync(ct)).First(x => x.Id == row.Id);
    }

    public async Task<IReadOnlyList<PosShiftDto>> GetShiftsAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("POS.Shift.View", ct);
        return await db.PosShifts.Include(x => x.Terminal).OrderByDescending(x => x.OpenedAt)
            .Select(x => new PosShiftDto(
                x.Id, x.ShiftNo, x.TerminalId, x.Terminal!.Code, x.OpenedAt, x.OpeningCash,
                x.ClosedAt, x.ExpectedCash, x.ActualCash, x.Variance, x.Status.ToString()))
            .ToListAsync(ct);
    }

    public async Task<PosShiftDto> OpenShiftAsync(PosShiftOpenDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("POS.Shift.Open", ct);
        var terminal = await db.PosTerminals.FirstOrDefaultAsync(x => x.Id == dto.TerminalId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Terminal not found or inactive.");
        if (await db.PosShifts.AnyAsync(x => x.TerminalId == terminal.Id && x.Status == PosShiftStatus.Open, ct))
            throw new InvalidOperationException("Terminal already has an open shift.");
        if (dto.OpeningCash < 0) throw new InvalidOperationException("Opening cash cannot be negative.");
        var userId = tenant.UserId ?? await db.Users.Select(x => x.Id).FirstAsync(ct);
        var shiftNo = $"SH-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var shift = new PosShift
        {
            TerminalId = terminal.Id,
            ShiftNo = shiftNo,
            OpenedAt = DateTime.UtcNow,
            OpenedByUserId = userId,
            OpeningCash = dto.OpeningCash,
            Status = PosShiftStatus.Open
        };
        db.PosShifts.Add(shift);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(PosShift), shift.Id, "Opened", null, $"Opening={dto.OpeningCash}", ct);
        return (await GetShiftsAsync(ct)).First(x => x.Id == shift.Id);
    }

    public async Task<PosShiftDto?> CloseShiftAsync(Guid id, PosShiftCloseDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("POS.Shift.Close", ct);
        var shift = await db.PosShifts
            .Include(x => x.Sales).ThenInclude(s => s.Payments)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (shift is null) return null;
        if (shift.Status != PosShiftStatus.Open)
            throw new InvalidOperationException("Shift is not open.");
        var cashSales = await db.PosPayments
            .Where(p => p.PosSale!.ShiftId == shift.Id
                        && p.PosSale.Status == DocumentStatus.Posted
                        && p.Tender == PosTenderType.Cash)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var expected = shift.OpeningCash + cashSales;
        var userId = tenant.UserId ?? await db.Users.Select(x => x.Id).FirstAsync(ct);
        shift.ExpectedCash = expected;
        shift.ActualCash = dto.ActualCash;
        shift.Variance = dto.ActualCash - expected;
        shift.ClosedAt = DateTime.UtcNow;
        shift.ClosedByUserId = userId;
        shift.Status = PosShiftStatus.Closed;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(PosShift), shift.Id, "Closed",
            $"Expected={expected}", $"Actual={dto.ActualCash};Variance={shift.Variance}", ct);
        return (await GetShiftsAsync(ct)).First(x => x.Id == id);
    }

    public async Task<IReadOnlyList<PosSaleDto>> GetAllAsync(CancellationToken ct = default)
    {
        await permissions.EnsureAsync("POS.Sale.View", ct);
        return await MapSalesAsync(ct);
    }

    public async Task<PosSaleDto> CreateAsync(PosSaleCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("POS.Sale.Create", ct);
        if (dto.Lines is null || dto.Lines.Count == 0)
            throw new InvalidOperationException("At least one line is required.");
        if (dto.Payments is null || dto.Payments.Count == 0)
            throw new InvalidOperationException("At least one payment is required.");

        var shift = await db.PosShifts.Include(x => x.Terminal)
            .FirstOrDefaultAsync(x => x.Id == dto.ShiftId, ct)
            ?? throw new InvalidOperationException("Shift not found.");
        if (shift.Status != PosShiftStatus.Open)
            throw new InvalidOperationException("Shift is not open.");
        var terminal = shift.Terminal ?? await db.PosTerminals.FirstAsync(x => x.Id == shift.TerminalId, ct);
        var party = await db.Parties.FirstOrDefaultAsync(x => x.Code == "CUS-POS", ct)
            ?? throw new InvalidOperationException("Walk-in party CUS-POS is not seeded.");

        var sub = dto.Lines.Sum(l => l.Qty * l.Rate);
        var payTotal = dto.Payments.Sum(p => p.Amount);
        if (Math.Abs(payTotal - sub) > 0.01m)
            throw new InvalidOperationException("Payments must equal sale total.");

        var cashBankCash = await db.CashBankAccounts.FirstOrDefaultAsync(x => x.Code == "CASH", ct);
        var cashBankCard = await db.CashBankAccounts.FirstOrDefaultAsync(x => x.Code == "CARD", ct)
            ?? await db.CashBankAccounts.FirstOrDefaultAsync(x => x.Code == "BANK", ct);

        var (company, branch, fy) = await ScopeAsync(ct);
        var posDt = await db.DocumentTypes.FirstAsync(x => x.Code == "POS", ct);
        var saleNo = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, posDt.Id, fy.Id, posDt.Prefix ?? "POS", fy.StartDate.Year, ct);

        var document = new Document
        {
            DocumentTypeId = posDt.Id,
            CompanyId = company.Id,
            BranchId = branch.Id,
            FinancialYearId = fy.Id,
            DocumentNo = saleNo,
            DocumentDate = (dto.SaleDate ?? DateTime.UtcNow).Date,
            PartyId = party.Id,
            Narration = dto.Remarks
        };

        var sale = new PosSale
        {
            SaleNo = saleNo,
            ShiftId = shift.Id,
            TerminalId = terminal.Id,
            PartyId = party.Id,
            SaleDate = (dto.SaleDate ?? DateTime.UtcNow).Date,
            SubTotal = sub,
            GrandTotal = sub,
            Status = DocumentStatus.Draft,
            Remarks = dto.Remarks,
            Document = document
        };
        var lineNo = 1;
        foreach (var l in dto.Lines)
        {
            sale.Lines.Add(new PosSaleLine
            {
                LineNo = lineNo++,
                ProductId = l.ProductId,
                Qty = l.Qty,
                Rate = l.Rate,
                Amount = l.Qty * l.Rate
            });
        }
        foreach (var p in dto.Payments)
        {
            if (!Enum.TryParse<PosTenderType>(p.Tender, true, out var tender))
                tender = PosTenderType.Cash;
            Guid? bankId = p.CashBankAccountId;
            if (bankId is null)
            {
                bankId = tender switch
                {
                    PosTenderType.Cash => cashBankCash?.Id,
                    PosTenderType.Card => cashBankCard?.Id,
                    _ => cashBankCash?.Id ?? cashBankCard?.Id
                };
            }
            sale.Payments.Add(new PosPayment
            {
                Tender = tender,
                Amount = p.Amount,
                CashBankAccountId = bankId
            });
        }
        db.PosSales.Add(sale);
        await db.SaveChangesAsync(ct);

        var invoiceLines = sale.Lines.Select(l => new InvoiceLineDto(null, l.ProductId, null, l.Qty, l.Rate, l.Amount)).ToList();
        var so = await sales.CreateOrderAsync(new SalesOrderCreateDto(
            sale.SaleDate, party.Id, terminal.WarehouseId, 0, $"POS {sale.SaleNo}", true, invoiceLines), ct);

        var doLines = so.Lines.Select(l => new InvoiceLineDto(null, l.ProductId, null, l.Qty, l.Rate, l.Amount)).ToList();
        var delivery = await sales.CreateDeliveryAsync(new DeliveryOrderCreateDto(
            sale.SaleDate, party.Id, terminal.WarehouseId, so.Id, $"POS {sale.SaleNo}", true, doLines), ct);

        var si = await sales.CreateInvoiceAsync(new SalesInvoiceDocCreateDto(
            sale.SaleDate, party.Id, 0, 0, $"POS {sale.SaleNo}", true, doLines, delivery.Id, so.Id), ct);

        foreach (var pay in sale.Payments)
        {
            var cp = await sales.CreatePaymentAsync(new CustomerPaymentCreateDto(
                sale.SaleDate, party.Id, pay.Amount, $"POS {sale.SaleNo} {pay.Tender}", true,
                [new PaymentApplicationDto(null, si.Id, si.InvoiceNo, pay.Amount)],
                pay.CashBankAccountId), ct);
            pay.CustomerPaymentId = cp.Id;
        }

        sale.SalesOrderId = so.Id;
        sale.DeliveryOrderId = delivery.Id;
        sale.SalesInvoiceId = si.Id;
        sale.Status = DocumentStatus.Posted;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(PosSale), sale.Id, "Posted", null,
            $"SO={so.OrderNo};DO={delivery.DeliveryNo};SI={si.InvoiceNo};Total={sale.GrandTotal}", ct);

        return (await MapSalesAsync(ct)).First(x => x.Id == sale.Id);
    }

    private async Task<IReadOnlyList<PosSaleDto>> MapSalesAsync(CancellationToken ct)
    {
        var rows = await db.PosSales
            .Include(x => x.Party)
            .Include(x => x.Lines).ThenInclude(l => l.Product)
            .Include(x => x.Payments)
            .OrderByDescending(x => x.SaleDate)
            .ToListAsync(ct);
        return rows.Select(x => new PosSaleDto(
            x.Id, x.SaleNo, x.ShiftId, x.TerminalId, x.PartyId, x.Party?.Name, x.SaleDate,
            x.SubTotal, x.GrandTotal, x.Status, x.SalesOrderId, x.DeliveryOrderId, x.SalesInvoiceId, x.Remarks,
            x.Lines.Select(l => new PosSaleLineDto(l.Id, l.ProductId, l.Product?.Name, l.Qty, l.Rate, l.Amount)).ToList(),
            x.Payments.Select(p => new PosPaymentDto(p.Id, p.Tender.ToString(), p.Amount, p.CashBankAccountId, p.CustomerPaymentId)).ToList()
        )).ToList();
    }

    private async Task<(Company company, Branch branch, FinancialYear fy)> ScopeAsync(CancellationToken ct)
    {
        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        return (company, branch, fy);
    }
}
